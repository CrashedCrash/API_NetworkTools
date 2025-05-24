// API_NetworkTools/Program.cs
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Implementations;
using API_NetworkTools.Data; // Für AppDbContext
using Microsoft.EntityFrameworkCore; // Für UseSqlite

var builder = WebApplication.CreateBuilder(args);

// CORS (wie zuvor)
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options => { /* ... */ });

// === DbContext für SQLite hinzufügen ===
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=urlshortener.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
// Die SQLite-Datenbankdatei (urlshortener.db) wird im Hauptverzeichnis deiner App erstellt.
// === Ende DbContext ===

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrierung der Netzwerk-Tools
builder.Services.AddTransient<INetworkTool, PingTool>();
builder.Services.AddTransient<INetworkTool, UrlShortenerTool>(); // NEUES TOOL REGISTRIEREN

var app = builder.Build();

// === Datenbank-Migrationen beim Start anwenden (optional, gut für Entwicklung) ===
// In Produktion ist es oft besser, Migrationen manuell oder per Skript anzuwenden.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // Erstellt die DB und wendet Migrationen an, falls nötig
}
// === Ende DB-Migrationen ===


if (app.Environment.IsDevelopment()) { /* ... */ }
// app.UseHttpsRedirection();
app.UseCors(myAllowSpecificOrigins);
// app.UseAuthorization();
app.MapControllers();

app.Run();