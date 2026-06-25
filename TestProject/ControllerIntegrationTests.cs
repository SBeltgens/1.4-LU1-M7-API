using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using API_Data.Controllers;
using SensoringData;
using NACGames;
using Predictions;

namespace SensoringData.Tests
{
    public class ControllerIntegrationTests
    {
        [Fact]
        public async Task GetPredictions_WhenApiReturnsEmpty_ShouldReturnStatus500()
        {
            // Arrange
            // We maken 'Mocks' van alle benodigde services voor de constructor
            var mockSensoring = new Mock<RetrieveSensoringData>(new System.Net.Http.HttpClient());
            var mockGames = new Mock<RetrieveNACGames>(new System.Net.Http.HttpClient());
            var mockPredictions = new Mock<RetrievePredictions>();

            // We dwingen de sensoring mock om een lege string terug te geven (foutsituatie)
            mockSensoring
                .Setup(s => s.GetSensoringDataAsync())
                .ReturnsAsync("");

            // De controller instantiëren met de geïnjecteerde mocks
            var controller = new API_DataController(mockGames.Object, mockPredictions.Object, mockSensoring.Object);

            // Act
            var result = await controller.GetPredictions();

            // Assert
            // We verwachten een ObjectResult (omdat we StatusCode(500, ...) teruggeven)
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}