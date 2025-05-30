// API_NetworkTools/Controllers/RedirectController.cs
using Microsoft.AspNetCore.Mvc;
using API_NetworkTools.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace API_NetworkTools.Controllers
{
    public class RedirectController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public RedirectController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("/s/{shortCode}")]
        public async Task<IActionResult> HandleRedirect(string shortCode)
        {
            if (string.IsNullOrWhiteSpace(shortCode))
            {
                return NotFound("Short code cannot be empty.");
            }

            var mapping = await _dbContext.ShortUrlMappings
                                          .FirstOrDefaultAsync(m => m.ShortCode == shortCode);

            if (mapping != null)
            {
                mapping.ClickCount++;
                await _dbContext.SaveChangesAsync();

                return Redirect(mapping.LongUrl);
            }
            else
            {
                return NotFound($"Short URL for code '{shortCode}' not found.");
            }
        }
    }
}