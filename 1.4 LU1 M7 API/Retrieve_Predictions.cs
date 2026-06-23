using System.Text.Json;

namespace Predictions
{
    public class RetrievePredictions
    {
        private readonly HttpClient _http;

        // Inject HttpClient via the constructor
        public RetrievePredictions(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> GetPredictionsAsync()
        {
            string url = $"https://www.thesportsdb.com/api/v1/json/123/eventsnext.php?id=133773";

            try
            {
                // Fetch live data from the API
                string jsonResponse = await _http.GetStringAsync(url);

                // Parse the JSON document dynamically
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                // Navigate to the 'events' array
                if (root.TryGetProperty("events", out JsonElement eventsArray) && eventsArray.GetArrayLength() > 0)
                {
                    // Extract the dateEvent string from the first event [0]
                    return eventsArray[0].GetProperty("dateEvent").GetString() ?? "No date specified";
                }

                return "No upcoming matches found";
            }
            catch (Exception ex)
            {
                // Log the error or handle it as needed for your prototype
                Console.WriteLine($"API Error: {ex.Message}");
                return "Error loading live data";
            }
        }
    }
}