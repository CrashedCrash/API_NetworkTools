// API_NetworkTools/Tools/Implementations/UrlShortenerTool.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API_NetworkTools.Data;
using API_NetworkTools.Models;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;
using Microsoft.EntityFrameworkCore;

namespace API_NetworkTools.Tools.Implementations
{
    public class UrlShortenerTool : INetworkTool
    {
        private readonly AppDbContext _dbContext;
        private const string ShortLinkBaseUrl = "https://api.solidstate.network/s/";

        public UrlShortenerTool(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string Identifier => "url-shortener";
        public string DisplayName => "URL Shortener";
        public string Description => "Erstellt eine kurze, eindeutige URL für eine lange URL.";

        public List<ToolParameterInfo> GetParameters()
        {
            return new List<ToolParameterInfo>();
        }

        public async Task<ToolOutput> ExecuteAsync(string longUrl, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(longUrl))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Die zu kürzende URL darf nicht leer sein." };
            }

            if (!Uri.TryCreate(longUrl, UriKind.Absolute, out Uri? validatedUri) || (validatedUri.Scheme != Uri.UriSchemeHttp && validatedUri.Scheme != Uri.UriSchemeHttps))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Ungültiges URL-Format. Bitte eine vollständige URL mit http:// oder https:// eingeben." };
            }

            var existingMapping = await _dbContext.ShortUrlMappings.FirstOrDefaultAsync(m => m.LongUrl == longUrl);
            if (existingMapping != null)
            {
                return new ToolOutput {
                    Success = true,
                    ToolName = DisplayName,
                    Data = new { ShortUrl = ShortLinkBaseUrl + existingMapping.ShortCode, LongUrl = longUrl, Message = "Diese URL wurde bereits gekürzt." }
                };
            }

            string shortCode;
            int attempts = 0;
            const int maxAttempts = 5;

            do
            {
                shortCode = GenerateShortCode();
                if (attempts++ > maxAttempts) {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Konnte keinen einzigartigen Kurzcode generieren. Bitte später erneut versuchen." };
                }
            } while (await _dbContext.ShortUrlMappings.AnyAsync(m => m.ShortCode == shortCode));

            var newMapping = new ShortUrlMapping
            {
                ShortCode = shortCode,
                LongUrl = longUrl,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ShortUrlMappings.Add(newMapping);
            await _dbContext.SaveChangesAsync();

            return new ToolOutput {
                Success = true,
                ToolName = DisplayName,
                Data = new { ShortUrl = ShortLinkBaseUrl + shortCode, LongUrl = longUrl }
            };
        }

        private string GenerateShortCode(int length = 6)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder sb = new StringBuilder();
            using (var crypto = RandomNumberGenerator.Create())
            {
                byte[] data = new byte[length];
                crypto.GetBytes(data);
                foreach (byte b in data)
                {
                    sb.Append(chars[b % (chars.Length)]);
                }
            }
            return sb.ToString();
        }
    }
}