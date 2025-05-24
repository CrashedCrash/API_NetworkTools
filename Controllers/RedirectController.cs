// API_NetworkTools/Controllers/RedirectController.cs
using Microsoft.AspNetCore.Mvc;
using API_NetworkTools.Data; // Für AppDbContext
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Für FirstOrDefaultAsync

namespace API_NetworkTools.Controllers
{
    // Dieser Controller benötigt keine Route "api/..." da er direkt im Root-Pfad mit /s/ reagieren soll
    public class RedirectController : ControllerBase 
    {
        private readonly AppDbContext _dbContext;
        // Optional: ILogger<RedirectController> _logger;

        public RedirectController(AppDbContext dbContext /*, ILogger<RedirectController> logger */)
        {
            _dbContext = dbContext;
            // _logger = logger;
        }

        // Dieser Endpunkt wird für https://api.solidstate.network/s/{shortCode} aufgerufen
        [HttpGet("/s/{shortCode}")] 
        public async Task<IActionResult> HandleRedirect(string shortCode)
        {
            if (string.IsNullOrWhiteSpace(shortCode))
            {
                return NotFound("Short code cannot be empty.");
            }

            // _logger?.LogInformation("Attempting redirect for short code: {ShortCode}", shortCode);

            var mapping = await _dbContext.ShortUrlMappings
                                          .FirstOrDefaultAsync(m => m.ShortCode == shortCode);

            if (mapping != null)
            {
                // Optional: Klickzähler erhöhen
                mapping.ClickCount++;
                await _dbContext.SaveChangesAsync(); // Nicht blockierend warten

                // _logger?.LogInformation("Redirecting short code {ShortCode} to {LongUrl}", shortCode, mapping.LongUrl);
                return Redirect(mapping.LongUrl); // Standard 302 Found Redirect
                // Für einen permanenten Redirect: return RedirectPermanent(mapping.LongUrl); (301)
            }
            else
            {
                // _logger?.LogWarning("Short code not found: {ShortCode}", shortCode);
                return NotFound($"Short URL for code '{shortCode}' not found.");
            }
        }
    }
}