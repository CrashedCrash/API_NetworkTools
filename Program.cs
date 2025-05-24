// API_NetworkTools/Program.cs
using API_NetworkTools.Data; // Für AppDbContext
using API_NetworkTools.Tools.Implementations; // Für PingTool, UrlShortenerTool
using API_NetworkTools.Tools.Interfaces; // Für INetworkTool
using Microsoft.EntityFrameworkCore; // Für UseSqlite und Migrate

var builder = WebApplication.CreateBuilder(args);

// 1. CORS-Policy definieren
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
                      policy =>
                      {
                          // WICHTIG: Ersetze "https://www.DEINE-WORDPRESS-DOMAIN.de" 
                          // mit der tatsächlichen Domain deiner WordPress-Seite!
                          // Wenn deine WordPress-Seite z.B. unter https://solidstate.network läuft, trage das hier ein.
                          // Wenn sie www verwendet, z.B. https://www.solidstate.network, dann diese Domain.
                          // Du kannst auch mehrere Domains hinzufügen: .WithOrigins("https://domain1.com", "https://domain2.com")
                          policy.WithOrigins("*") 
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// 2. DbContext für SQLite hinzufügen und konfigurieren
// Die SQLite-Datenbankdatei (urlshortener.db) wird im Ausführungsverzeichnis der App erstellt.
// Auf dem Server ist das dein WorkingDirectory, z.B. /opt/api_networktools/
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=urlshortener.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// 3. Controller-Dienste hinzufügen
builder.Services.AddControllers();

// 4. Dienste für API Explorer und Swagger/OpenAPI hinzufügen
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Stellt sicher, dass Swashbuckle.AspNetCore im .csproj ist

// 5. Registrierung deiner Netzwerk-Tools für Dependency Injection
builder.Services.AddTransient<INetworkTool, PingTool>();
builder.Services.AddTransient<INetworkTool, UrlShortenerTool>();
// Hier später weitere Tools registrieren, z.B.:
// builder.Services.AddTransient<INetworkTool, DnsLookupTool>();


// === Anwendung bauen ===
var app = builder.Build();


// 6. Datenbank-Migrationen beim Start anwenden (optional, aber gut für Entwicklung/einfache Deployments)
// Stellt sicher, dass die Datenbank dem aktuellen Modell entspricht.
// In einer größeren Produktionsumgebung führt man Migrationen oft separat aus.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate(); // Erstellt die DB und wendet ausstehende Migrationen an
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
        // Optional: Anwendung beenden, wenn DB nicht initialisiert werden kann
        // throw; 
    }
}

// 7. HTTP-Request-Pipeline konfigurieren
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Stellt Swagger UI unter /swagger bereit
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API_NetworkTools v1");
        // Optional: RoutePrefix auf leer setzen, damit Swagger UI unter der Root-URL der API erreichbar ist (im Development)
        // c.RoutePrefix = string.Empty; 
    });
}

// HTTPS Redirection:
// Dein Nginx Proxy Manager terminiert SSL und leitet Anfragen als HTTP an deine App weiter.
// Wenn NPM die X-Forwarded-Proto und X-Forwarded-Host Header korrekt setzt (was es meist tut),
// kann UseHttpsRedirection hier immer noch nützlich sein, falls doch mal eine HTTP-Anfrage direkt ankommt
// oder um sicherzustellen, dass die App intern weiß, dass sie hinter HTTPS läuft.
// Wenn du Probleme damit hast (z.B. Redirect-Loops), kommentiere es aus, da NPM bereits "Force SSL" macht.
app.UseHttpsRedirection();

// CORS-Middleware verwenden (WICHTIG: Vor UseRouting/UseEndpoints/MapControllers und vor UseAuthorization)
app.UseCors(myAllowSpecificOrigins);

// app.UseRouting(); // In .NET 6+ oft nicht mehr explizit nötig, wenn Endpoints verwendet werden

// app.UseAuthorization(); // Falls du später Authentifizierung/Autorisierung hinzufügst

app.MapControllers(); // Mappt die Routen für deine Controller (ToolsController, RedirectController)

app.Run();