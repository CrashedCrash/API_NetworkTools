// API_NetworkTools/Program.cs
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Implementations;

var builder = WebApplication.CreateBuilder(args);

var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins, policy =>
    {
        // WICHTIG: Ersetze dies mit deiner WordPress Domain!
        policy.WithOrigins("*") 
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Füge ggf. `dotnet add package Swashbuckle.AspNetCore` aus, falls nicht schon im Projekt

// Registrierung der Netzwerk-Tools
builder.Services.AddTransient<INetworkTool, PingTool>();
// builder.Services.AddTransient<INetworkTool, DnsLookupTool>(); // Für zukünftige Tools

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Wird oft vom Reverse Proxy (NPM) gehandhabt
app.UseCors(myAllowSpecificOrigins);
// app.UseAuthorization();

app.MapControllers();
app.Run();