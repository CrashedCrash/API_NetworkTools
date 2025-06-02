// API_NetworkTools/Program.cs
using API_NetworkTools.Data;
using API_NetworkTools.Tools.Implementations;
using API_NetworkTools.Tools.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("*")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=urlshortener.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<INetworkTool, PingTool>();
builder.Services.AddTransient<INetworkTool, UrlShortenerTool>();
builder.Services.AddTransient<INetworkTool, ARecordLookupTool>();
builder.Services.AddTransient<INetworkTool, AAAARecordLookupTool>();
builder.Services.AddTransient<INetworkTool, TracerouteTool>();
builder.Services.AddTransient<INetworkTool, ReverseDnsTool>();
builder.Services.AddTransient<INetworkTool, MxRecordLookupTool>();
builder.Services.AddTransient<INetworkTool, NsRecordLookupTool>();
builder.Services.AddTransient<INetworkTool, TxtRecordLookupTool>();
builder.Services.AddTransient<INetworkTool, PortScannerTool>();
builder.Services.AddTransient<INetworkTool, WhoisLookupTool>();
builder.Services.AddTransient<INetworkTool, IpGeolocationTool>();
builder.Services.AddTransient<INetworkTool, HttpHeaderTool>();
builder.Services.AddTransient<INetworkTool, SslCertificateTool>();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API_NetworkTools v1");
    });
}

app.UseHttpsRedirection();

app.UseCors(myAllowSpecificOrigins);


app.MapControllers();

app.Run();