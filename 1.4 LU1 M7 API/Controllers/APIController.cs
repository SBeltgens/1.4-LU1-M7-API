using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NACGames;
using Predictions;
using SensoringData; // Make sure this namespace matches where your new classes live

namespace API_Data.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ApiKey]
    public class API_DataController : ControllerBase
    {
        private readonly RetrieveNACGames _gameService;
        private readonly RetrievePredictions _predictionService;
        private readonly RetrieveSensoringData _sensoringDataService; // Injected service

        public API_DataController(RetrieveNACGames gameService, RetrievePredictions predictionService, RetrieveSensoringData sensoringDataService)
        {
            _gameService = gameService;
            _predictionService = predictionService;
            _sensoringDataService = sensoringDataService;
        }

        [HttpGet("NextHomeGame")]
        public async Task<IActionResult> GetNextHomeGame()
        {
            string matchDate = await _gameService.GetNextMatchDateAsync();
            return Ok(new { NextHomeGame = matchDate });
        }

        [HttpGet("Predictions")]
        public async Task<IActionResult> GetPredictions()
        {
            try
            {
                // 1. Fetch raw response text from the sensoring data service
                string rawJson = await _sensoringDataService.GetSensoringDataAsync();
                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    return StatusCode(500, new { message = "External sensoring API returned an empty response." });
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                SensoringDataRoot dataRoot = null;

                using (JsonDocument doc = JsonDocument.Parse(rawJson))
                {
                    JsonElement root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Object)
                        dataRoot = JsonSerializer.Deserialize<SensoringDataRoot>(rawJson, options);
                    else if (root.ValueKind == JsonValueKind.String)
                        dataRoot = JsonSerializer.Deserialize<SensoringDataRoot>(root.GetString(), options);
                }

                // TEMPORARY TESTING BLOCK: If the live API is empty, generate a fake record to test the AI endpoint
                if (dataRoot?.Records == null || dataRoot.Records.Count == 0)
                {
                    dataRoot = new SensoringDataRoot
                    {
                        Records = new List<SensoringRecord>
        {
            new SensoringRecord
            {
                Id = 999,
                CaptureDate = DateTime.UtcNow,
                GarbageType = "Blikje",
                Location = "51.5865,4.7759", // Rat Verlegh Stadion area
                Confidence = 95
            }
        }
                    };
                }

                // 2. Map the very first record into the flat TrashPostModel format
                var record = dataRoot.Records.First();
                double latitude = 0.0;
                double longitude = 0.0;

                if (!string.IsNullOrEmpty(record.Location) && record.Location.Contains(","))
                {
                    var coordinates = record.Location.Split(',');
                    double.TryParse(coordinates[0], System.Globalization.CultureInfo.InvariantCulture, out latitude);
                    double.TryParse(coordinates[1], System.Globalization.CultureInfo.InvariantCulture, out longitude);
                }

                var aiPayload = new TrashPostModel
                {
                    CameraId = record.Id,
                    Latitude = latitude,
                    Longitude = longitude,

                    // Changed property name here to match the new model definition
                    StartDate = DateTime.Now.ToString("yyyy-MM-dd"),

                    Confidence = Math.Round(record.Confidence / 100.0, 2),
                    GarbageType = record.GarbageType,

                    // Contextual metrics
                    GarbageAmount = 5,
                    DistanceToStadiumKm = 3.0,
                    IsNACMatchDay = 1,
                    IsHomeMatch = 1,
                    ExpectedCrowdLevel = "Middel"
                };

                // 3. Serialize and POST the data to the AI service link
                var outboundJson = JsonSerializer.Serialize(aiPayload, options);
                var content = new StringContent(outboundJson, Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    // ADDED: Inject the required x-api-key header here
                    client.DefaultRequestHeaders.Add("x-api-key", "ZrjW2HokPKnZOAvMdV6ckt2tN5HaQtx23yqLSe61tbQ");

                    var aiResponse = await client.PostAsync("https://airepogroup6-production.up.railway.app/predict-week", content);
                    string aiResponseBody = await aiResponse.Content.ReadAsStringAsync();

                    if (!aiResponse.IsSuccessStatusCode)
                    {
                        return StatusCode((int)aiResponse.StatusCode, new
                        {
                            message = "The AI endpoint rejected the payload.",
                            errorResponse = aiResponseBody
                        });
                    }

                    // 4. Return parsed response object layout
                    try
                    {
                        using (JsonDocument aiDoc = JsonDocument.Parse(aiResponseBody))
                        {
                            return Ok(aiDoc.RootElement.Clone());
                        }
                    }
                    catch (JsonException)
                    {
                        return Ok(new { aiResponseRaw = aiResponseBody });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error during transform and AI evaluation loop", details = ex.Message });
            }
        }
    }
}