using System.Text.Json;
using SensoringData;

namespace Predictions
{
    public class RetrievePredictions
    {
        private readonly HttpClient _http;
        private readonly RetrieveSensoringData _sensoringDataService;

        // Inject HttpClient via the constructor
        public RetrievePredictions(HttpClient http, RetrieveSensoringData sensoringDataService)
        {
            _http = http;
            _sensoringDataService = sensoringDataService;
        }

        public async Task<string> GetPredictionsAsync()
        {
            return await _sensoringDataService.GetSensoringDataAsync();
        }
        }
    }
