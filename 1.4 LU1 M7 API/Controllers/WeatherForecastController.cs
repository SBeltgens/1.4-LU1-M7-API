using Microsoft.AspNetCore.Mvc;

namespace NACGames.Controllers
{
    [ApiController]
    [Route("[controller]")] // This makes the base route /NACGames
    public class NACGamesController : ControllerBase
    {
        private readonly RetrieveNACGames _gameService;

        public NACGamesController(RetrieveNACGames gameService)
        {
            _gameService = gameService;
        }

        // This maps to: GET /NACGames/NextHomeGame
        [HttpGet("NextHomeGame")]
        public async Task<IActionResult> GetNextHomeGame()
        {
            string matchDate = await _gameService.GetNextMatchDateAsync();

            // Returns an HTTP 200 OK status with your JSON object
            return Ok(new { NextHomeGame = matchDate });
        }
    }
}