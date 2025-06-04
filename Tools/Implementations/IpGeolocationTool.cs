// API_NetworkTools/Tools/Implementations/IpGeolocationTool.cs
using System;
using System.Collections.Generic;
using System.Linq; // Hinzugefügt für .FirstOrDefault()
using System.Net; // Hinzugefügt für IPAddress und Dns
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
        private static readonly HttpClient httpClient = new HttpClient();
        // WICHTIG: Auf HTTP geändert, da der kostenlose Endpunkt SSL nicht unterstützt
        private const string ApiBaseUrl = "http://ip-api.com/json/";

        public string Identifier => "ip-geolocation";
        public string DisplayName => "IP Geolocation";
        public string Description => "Ermittelt geografische Informationen für eine IP-Adresse oder Domain über ip-api.com.";

        public List<ToolParameterInfo> GetParameters()
        {
            return new List<ToolParameterInfo>();
        }

        public async Task<ToolOutput> ExecuteAsync(string targetNameOrIp, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(targetNameOrIp))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Ziel (Hostname/IP-Adresse) darf nicht leer sein." };
            }

            string ipToQuery;

            // Prüfen, ob die Eingabe ein Hostname und keine IP-Adresse ist
            if (!IPAddress.TryParse(targetNameOrIp, out IPAddress? parsedIpAddress))
            {
                // Die Eingabe ist keine gültige IP, also versuchen wir, sie als Hostname aufzulösen
                try
                {
                    IPAddress[] addresses = await Dns.GetHostAddressesAsync(targetNameOrIp);
                    if (addresses == null || addresses.Length == 0)
                    {
                        return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Hostname '{targetNameOrIp}' konnte nicht aufgelöst werden." };
                    }
                    // Bevorzuge IPv4, falls vorhanden, ansonsten nimm die erste Adresse
                    // ip-api.com kann sowohl IPv4 als auch IPv6 verarbeiten
                    parsedIpAddress = addresses.FirstOrDefault(addr => addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                      ?? addresses[0];
                    ipToQuery = parsedIpAddress.ToString();
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler beim Auflösen des Hostnamens '{targetNameOrIp}': {ex.Message}" };
                }
                catch (Exception ex) // Andere unerwartete Fehler bei der DNS-Auflösung
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Fehler ist beim Auflösen von '{targetNameOrIp}' aufgetreten: {ex.Message}" };
                }
            }
            else // Die Eingabe war bereits eine IP-Adresse
            {
                // Stelle sicher, dass wir die geparste (und potenziell kanonisierte) IP-Adresse verwenden
                ipToQuery = parsedIpAddress.ToString();
            }

            // Der Rest der Methode verwendet 'ipToQuery' für die Anfrage an ip-api.com
            try
            {
                string apiUrl = $"{ApiBaseUrl}{ipToQuery}?fields=status,message,country,countryCode,regionName,city,zip,lat,lon,timezone,isp,org,as,query";

                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                IpApiComResponse? geoResponse = await response.Content.ReadFromJsonAsync<IpApiComResponse>();

                if (geoResponse == null)
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Fehler beim Deserialisieren der API-Antwort." };
                }

                if ("success".Equals(geoResponse.Status, StringComparison.OrdinalIgnoreCase))
                {
                    // Ggf. hier das Data-Objekt anreichern, um targetNameOrIp und ipToQuery anzuzeigen, falls unterschiedlich
                    object dataToReturn = geoResponse;
                    if (!targetNameOrIp.Equals(ipToQuery, StringComparison.OrdinalIgnoreCase))
                    {
                        dataToReturn = new {
                            OriginalInput = targetNameOrIp,
                            ResolvedIp = ipToQuery,
                            Geolocation = geoResponse
                        };
                    }
                    return new ToolOutput { Success = true, ToolName = DisplayName, Data = dataToReturn };
                }
                else
                {
                    string apiErrorMessage = geoResponse.Message ?? "Unbekannter Fehler von ip-api.com";
                    if (!targetNameOrIp.Equals(ipToQuery, StringComparison.OrdinalIgnoreCase)) {
                         apiErrorMessage = $"API-Fehler für IP '{ipToQuery}' (aufgelöst von '{targetNameOrIp}'): {apiErrorMessage}";
                    } else {
                         apiErrorMessage = $"API-Fehler für '{ipToQuery}': {apiErrorMessage}";
                    }
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = apiErrorMessage, Data = geoResponse };
                }
            }
            catch (HttpRequestException httpEx)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Netzwerk- oder HTTP-Fehler beim Abrufen der Geolocation-Daten für '{ipToQuery}': {httpEx.Message}" };
            }
            catch (Exception ex) // Andere unerwartete Fehler beim Abruf der Geolocation
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Fehler ist beim Abrufen der Geolocation für '{ipToQuery}' aufgetreten: {ex.Message}" };
            }
        }
    }
}