using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NACGames;
using Predictions;
using SensoringData;

namespace API_Data.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ApiKey]
    public class API_DataController : ControllerBase
    {
        private readonly RetrieveNACGames _gameService;
        private readonly RetrievePredictions _predictionService;
        private readonly RetrieveSensoringData _sensoringDataService;

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
                // 1. Fetch raw response text from the live service
                string rawJson = await _sensoringDataService.GetSensoringDataAsync();
                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    return StatusCode(500, new { message = "External sensoring API returned an empty response." });
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                List<SensoringRecord> recordsList = null;

                // 2. Open the JSON document and target the "predictions" array directly
                using (JsonDocument doc = JsonDocument.Parse(rawJson))
                {
                    JsonElement root = doc.RootElement;
                    JsonElement arrayElement = default;

                    // SCENARIO A: The root is a raw JSON array direct list (like your new payload)
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        arrayElement = root;
                    }
                    // SCENARIO B: The root is an object wrapper containing the property
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("predictions", out JsonElement lowerProp))
                        {
                            arrayElement = lowerProp;
                        }
                        else if (root.TryGetProperty("Predictions", out JsonElement upperProp))
                        {
                            arrayElement = upperProp;
                        }
                    }
                    // SCENARIO C: The root is a string primitive wrapping the actual data payload
                    else if (root.ValueKind == JsonValueKind.String)
                    {
                        using (JsonDocument innerDoc = JsonDocument.Parse(root.GetString()))
                        {
                            JsonElement innerRoot = innerDoc.RootElement;
                            if (innerRoot.ValueKind == JsonValueKind.Array)
                            {
                                arrayElement = innerRoot.Clone();
                            }
                            else if (innerRoot.ValueKind == JsonValueKind.Object && innerRoot.TryGetProperty("predictions", out JsonElement innerProp))
                            {
                                arrayElement = innerProp.Clone();
                            }
                        }
                    }

                    // Safely deserialize whichever array structure we matched
                    if (arrayElement.ValueKind == JsonValueKind.Array)
                    {
                        recordsList = JsonSerializer.Deserialize<List<SensoringRecord>>(arrayElement.GetRawText(), options);
                    }
                }

                // 3. Map the first live record into the flat TrashPostModel format
                var record = recordsList.First();
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

                    // Change this line to output ONLY the year, month, and day
                    StartDate = DateTime.Now.ToString("yyyy-MM-dd"),

                    Confidence = Math.Round(record.Confidence / 100.0, 2),
                    GarbageType = record.GarbageType,
                    GarbageAmount = 5,
                    DistanceToStadiumKm = 3.0,
                    IsNACMatchDay = 1,
                    IsHomeMatch = 1,
                    ExpectedCrowdLevel = "Middel"
                };

                // 4. Send package to the AI service endpoint
                var outboundJson = JsonSerializer.Serialize(aiPayload, options);
                var content = new StringContent(outboundJson, Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    // Authenticate outbound transmission
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

    // =========================================================================
    // DATA TRANSFER OBJECT MODELS (DTOs)
    // =========================================================================

    public class SensoringDataRoot
    {
        [JsonPropertyName("predictions")]
        public List<SensoringRecord> Records { get; set; }
    }

    public class SensoringRecord
    {
        public int Id { get; set; }
        public DateTime CaptureDate { get; set; }
        public string GarbageType { get; set; }
        public string Location { get; set; }
        public int Confidence { get; set; }

        [JsonPropertyName("externalParameters")]
        [JsonConverter(typeof(SensoringExternalParametersConverter))]
        public SensoringExternalParameters ExternalParameters { get; set; }
    }

    public class SensoringExternalParameters
    {
        public string GoogleMapsLink { get; set; }
        public SensoringWeather Weather { get; set; }
    }

    public class SensoringWeather
    {
        public string Provider { get; set; }
        public bool Available { get; set; }
        public DateTime Time { get; set; }

        [JsonPropertyName("temperature2m")]
        public double Temperature { get; set; }

        [JsonPropertyName("relativeHumidity2m")]
        public double RelativeHumidity { get; set; }
    }

    public class SensoringExternalParametersConverter : JsonConverter<SensoringExternalParameters>
    {
        public override SensoringExternalParameters Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonString = reader.GetString();
            return string.IsNullOrEmpty(jsonString) ? null : JsonSerializer.Deserialize<SensoringExternalParameters>(jsonString, options);
        }

        public override void Write(Utf8JsonWriter writer, SensoringExternalParameters value, JsonSerializerOptions options) =>
            writer.WriteStringValue(JsonSerializer.Serialize(value, options));
    }
}