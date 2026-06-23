using Microsoft.AspNetCore.Mvc;
using NACGames;
using Predictions;

namespace API_Data.Controllers
{
    [ApiController]
    [Route("[controller]")] // This makes the base route /NACGames
    [ApiKey]
    public class API_DataController : ControllerBase
    {
        private readonly RetrieveNACGames _gameService;
        private readonly RetrievePredictions _predictionService;

        public API_DataController(RetrieveNACGames gameService, RetrievePredictions predictionService)
        {
            _gameService = gameService;
            _predictionService = predictionService;
        }

        // This maps to: GET /NACGames/NextHomeGame
        [HttpGet("NextHomeGame")]
        public async Task<IActionResult> GetNextHomeGame()
        {
            string matchDate = await _gameService.GetNextMatchDateAsync();

            // Returns an HTTP 200 OK status with your JSON object
            return Ok(new { NextHomeGame = matchDate });
        }

        [HttpGet("Predictions")]
        public async Task<IActionResult> GetPredictions()
        {
            string predictions = await _predictionService.GetPredictionsAsync();

            // Returns an HTTP 200 OK status with your JSON object
            return Ok(new { Predictions = predictions });
        }
    }
}