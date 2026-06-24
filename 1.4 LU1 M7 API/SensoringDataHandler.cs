using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SensoringData
{
    public class SensoringDataRoot
    {
        // 1. Removed the converter because "predictions" is now a regular JSON array!
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

        // 2. Kept this converter because this field is still stringified JSON text
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

    #region Custom JSON Converter for Nested External Parameters

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
    #endregion
}