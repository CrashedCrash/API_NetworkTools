// API_NetworkTools/Tools/Implementations/ARecordLookupTool.cs
// Diese Datei existiert bereits in deinem Projekt.
// Hier ist der Inhalt zur Erinnerung:
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets; // Für AddressFamily
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces; // Sicherstellen, dass INetworkTool hier korrekt referenziert wird
using API_NetworkTools.Tools.Models;   // Sicherstellen, dass ToolOutput und ToolParameterInfo hier korrekt referenziert werden


namespace API_NetworkTools.Tools.Implementations // Dein Namespace für Implementierungen
{
    public class ARecordLookupTool : INetworkTool
    {
        public string Identifier => "a-lookup";
        public string DisplayName => "A Record Lookup (IPv4)";
        public string Description => "Findet die IPv4-Adressen (A-Records) für einen Hostnamen.";

        public List<ToolParameterInfo> GetParameters()
        {
            // Dieses Tool benötigt neben dem "target" keine zusätzlichen spezifischen Parameter.
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
                List<string> aRecords = addresses
                                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork) // Filtert nach IPv4
                                        .Select(ip => ip.ToString())
                                        .ToList();

                if (aRecords.Any())
                {
                    return new ToolOutput { Success = true, ToolName = DisplayName, Data = aRecords };
                }
                else
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Keine A-Records (IPv4) für {target} gefunden.", Data = new List<string>() };
                }
            }
            catch (SocketException ex) // Tritt auf, wenn der Host nicht gefunden/aufgelöst werden kann
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler beim Auflösen von {target}: {ex.Message}" };
            }
            catch (System.Exception ex)
            {
                // TODO: Logge die Exception ex für Debugging-Zwecke
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}" };
            }
        }
    }
}