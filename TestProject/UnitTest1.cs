using Xunit;
using API_Data.Controllers;
using SensoringData;

namespace SensoringData.Tests
{
    public class TransformationTests
    {
        [Fact]
        public void LocationString_ShouldSplit_IntoCorrectLatitudeAndLongitude()
        {
            // Arrange (De startsituatie klaarzetten)
            var record = new SensoringRecord
            {
                Id = 14,
                Location = "51.802387,4.683999", // Test coördinaten
                Confidence = 72,
                GarbageType = "cardboard"
            };

            // Act (De actie uitvoeren die we willen testen)
            double latitude = 0.0;
            double longitude = 0.0;

            if (!string.IsNullOrEmpty(record.Location) && record.Location.Contains(","))
            {
                var coordinates = record.Location.Split(',');
                double.TryParse(coordinates[0], System.Globalization.CultureInfo.InvariantCulture, out latitude);
                double.TryParse(coordinates[1], System.Globalization.CultureInfo.InvariantCulture, out longitude);
            }

            // Assert (Controleren of de uitkomst klopt met de verwachting)
            Assert.Equal(51.802387, latitude);
            Assert.Equal(4.683999, longitude);
        }
    }
}