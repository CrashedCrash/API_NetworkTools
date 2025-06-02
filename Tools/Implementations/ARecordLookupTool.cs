// API_NetworkTools/Tools/Implementations/ARecordLookupTool.cs
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;


namespace API_NetworkTools.Tools.Implementations
{
    public class ARecordLookupTool : INetworkTool
    {
        public string Identifier => "a-lookup";
        public string DisplayName => "A Record Lookup (IPv4)";
        public string Description => "Findet die IPv4-Adressen (A-Records) für einen Hostnamen.";

        public List<ToolParameterInfo> GetParameters()
        {
            return new List<ToolParameterInfo>();
        }

        public async Task<ToolOutput> ExecuteAsync(string target, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Hostname darf nicht leer sein." };
            }
            if (Uri.CheckHostName(target) == UriHostNameType.Unknown)
            {
                if (!target.Equals("localhost", StringComparison.OrdinalIgnoreCase)) {
                    // Geänderte Fehlermeldung:
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiges Ziel (Hostname/IP): {target}" };
                }
            }

            try
            {
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(target);
                List<string> aRecords = addresses
                                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
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
            catch (SocketException ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler beim Auflösen von {target}: {ex.Message}" };
            }
            catch (System.Exception ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}" };
            }
        }
    }
}