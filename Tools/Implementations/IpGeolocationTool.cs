// API_NetworkTools/Tools/Implementations/IpGeolocationTool.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json; // Für ReadFromJsonAsync
using System.Text.Json.Serialization; // Für JsonPropertyName
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;

namespace API_NetworkTools.Tools.Implementations
{
    // DTO für die Deserialisierung der ip-api.com Antwort
    public record IpApiComResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; } // Fehlermeldung bei status="fail"

        [JsonPropertyName("country")]
        public string? Country { get; init; }

        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; init; }

        [JsonPropertyName("region")]
        public string? Region { get; init; }

        [JsonPropertyName("regionName")]
        public string? RegionName { get; init; }

        [JsonPropertyName("city")]
        public string? City { get; init; }

        [JsonPropertyName("zip")]
        public string? Zip { get; init; }

        [JsonPropertyName("lat")]
        public double? Lat { get; init; }

        [JsonPropertyName("lon")]
        public double? Lon { get; init; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; init; }

        [JsonPropertyName("isp")]
        public string? Isp { get; init; }

        [JsonPropertyName("org")]
        public string? Org { get; init; }

        [JsonPropertyName("as")]
        public string? As { get; init; } // AS = Autonomous System

        [JsonPropertyName("query")]
        public string? Query { get; init; } // Die abgefragte IP
    }

    public class IpGeolocationTool : INetworkTool
    {
        private static readonly HttpClient httpClient = new HttpClient(); // Einmalige Instanz für bessere Performance
        private const string ApiBaseUrl = "http://ip-api.com/json/"; // HTTPS verwenden

        public string Identifier => "ip-geolocation";
        public string DisplayName => "IP Geolocation";
        public string Description => "Ermittelt geografische Informationen für eine IP-Adresse über ip-api.com.";

        public List<ToolParameterInfo> GetParameters()
        {
            return new List<ToolParameterInfo>();
        }

        public async Task<ToolOutput> ExecuteAsync(string targetIpAddress, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(targetIpAddress))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "IP-Adresse darf nicht leer sein." };
            }

            if (!IPAddress.TryParse(targetIpAddress, out _))
            {
                // ip-api.com kann auch mit ungültigen IPs umgehen und gibt dann einen Fehler zurück,
                // aber eine grundlegende Vorabprüfung ist sinnvoll.
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiges IP-Adressformat: {targetIpAddress}" };
            }

            try
            {
                // Spezifische Felder für die Abfrage auswählen, um die Antwortgröße zu reduzieren (optional, aber gute Praxis)
                // https://ip-api.com/docs/api:json (siehe "Fields")
                // Beispiel: fields=status,message,country,countryCode,regionName,city,lat,lon,isp,org,query
                string apiUrl = $"{ApiBaseUrl}{targetIpAddress}?fields=status,message,country,countryCode,regionName,city,zip,lat,lon,timezone,isp,org,as,query";
                
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode(); // Wirft eine Exception bei nicht-erfolgreichen HTTP-Statuscodes (4xx, 5xx)

                IpApiComResponse? geoResponse = await response.Content.ReadFromJsonAsync<IpApiComResponse>();

                if (geoResponse == null)
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Fehler beim Deserialisieren der API-Antwort." };
                }

                if ("success".Equals(geoResponse.Status, StringComparison.OrdinalIgnoreCase))
                {
                    return new ToolOutput { Success = true, ToolName = DisplayName, Data = geoResponse };
                }
                else
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"API-Fehler: {geoResponse.Message ?? "Unbekannter Fehler von ip-api.com"}" , Data = geoResponse};
                }
            }
            catch (HttpRequestException httpEx)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Netzwerk- oder HTTP-Fehler beim Abrufen der Geolocation-Daten: {httpEx.Message}" };
            }
            catch (Exception ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}" };
            }
        }
    }
}