// API_NetworkTools/Tools/Implementations/AAAARecordLookupTool.cs
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets; // Für AddressFamily
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces; // Sicherstellen, dass INetworkTool hier korrekt referenziert wird
using API_NetworkTools.Tools.Models;   // Sicherstellen, dass ToolOutput und ToolParameterInfo hier korrekt referenziert werden

namespace API_NetworkTools.Tools.Implementations
{
    public class AAAARecordLookupTool : INetworkTool
    {
        public string Identifier => "aaaa-lookup";
        public string DisplayName => "AAAA Record Lookup (IPv6)";
        public string Description => "Findet die IPv6-Adressen (AAAA-Records) für einen Hostnamen.";

        public List<ToolParameterInfo> GetParameters()
        {
            // Dieses Tool benötigt neben dem "target" (Hostname) keine zusätzlichen spezifischen Parameter.
            return new List<ToolParameterInfo>();
        }

        public async Task<ToolOutput> ExecuteAsync(string target, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Hostname darf nicht leer sein." };
            }

            // Zusätzliche Validierung für den Hostnamen (optional, aber empfohlen)
            if (Uri.CheckHostName(target) == UriHostNameType.Unknown)
            {
                 // Ausnahme für localhost, da CheckHostName dies als Unknown klassifizieren kann, es aber gültig ist.
                if (!target.Equals("localhost", StringComparison.OrdinalIgnoreCase)) {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiger Hostname: {target}" };
                }
            }

            try
            {
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(target);
                List<string> aaaaRecords = addresses
                                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6) // Filtert nach IPv6
                                        .Select(ip => ip.ToString())
                                        .ToList();

                if (aaaaRecords.Any())
                {
                    return new ToolOutput { Success = true, ToolName = DisplayName, Data = aaaaRecords };
                }
                else
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Keine AAAA-Records (IPv6) für {target} gefunden.", Data = new List<string>() };
                }
            }
            catch (SocketException ex) // Tritt auf, wenn der Host nicht gefunden/aufgelöst werden kann
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler beim Auflösen von {target}: {ex.Message}" };
            }
            catch (System.Exception ex) // Fängt andere unerwartete Fehler ab
            {
                // Es ist eine gute Praxis, hier zu loggen, um unerwartete Fehler zu debuggen.
                // z.B. _logger.LogError(ex, "Unerwarteter Fehler im AAAARecordLookupTool für Ziel {Target}", target);
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Serverfehler ist aufgetreten: {ex.Message}" };
            }
        }
    }
}