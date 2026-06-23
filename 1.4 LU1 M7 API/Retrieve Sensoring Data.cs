using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SensoringData
{
    public class RetrieveSensoringData
    {
        private readonly HttpClient _http;

        public RetrieveSensoringData(HttpClient http)
        {
            _http = http;
        }

        // 1. Updated this class to match the server's "accessToken" JSON key
        private class LoginResponse
        {
            public string AccessToken { get; set; }
        }

        public async Task<string> GetSensoringDataAsync()
        {
            var loginData = new
            {
                email = "monitoring@groep.nl",
                password = "Monitoring1234!"
            };

            var jsonPayload = JsonSerializer.Serialize(loginData);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Authenticate
            var loginResponse = await _http.PostAsync("https://avansafvalapi-production.up.railway.app/account/login", content);

            if (!loginResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Login failed with status code: {loginResponse.StatusCode}");
            }

            // Extract the Token from the login response
            var loginJsonResult = await loginResponse.Content.ReadAsStringAsync();
            var authData = JsonSerializer.Deserialize<LoginResponse>(loginJsonResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (authData == null || string.IsNullOrEmpty(authData.AccessToken))
            {
                throw new Exception("Login succeeded, but no access token was found in the response.");
            }

            // 2. Attach the token using authData.AccessToken
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.AccessToken);

            // Fetch the actual trash data
            var dataResponse = await _http.GetAsync("https://avansafvalapi-production.up.railway.app/trash");

            if (!dataResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to fetch data: {dataResponse.StatusCode}");
            }

            return await dataResponse.Content.ReadAsStringAsync();
        }
    }
}