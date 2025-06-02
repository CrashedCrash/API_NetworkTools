// API_NetworkTools/Tools/Implementations/ReverseDnsTool.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;

namespace API_NetworkTools.Tools.Implementations
{
    public class ReverseDnsTool : INetworkTool
    {
        public string Identifier => "reverse-dns";
        public string DisplayName => "Reverse DNS Lookup (PTR Record)";
        public string Description => "Ermittelt den Hostnamen zu einer gegebenen IP-Adresse.";

        public List<ToolParameterInfo> GetParameters()
        {
            // Für dieses Tool benötigen wir keine zusätzlichen Parameter neben dem 'target' (der IP-Adresse).
            return new List<ToolParameterInfo>();
        }

        public async Task<ToolOutput> ExecuteAsync(string targetIpAddress, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(targetIpAddress))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "IP-Adresse darf nicht leer sein." };
            }

            if (!IPAddress.TryParse(targetIpAddress, out IPAddress? ipAddress))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiges IP-Adressformat: {targetIpAddress}" };
            }

            try
            {
                IPHostEntry hostEntry = await Dns.GetHostEntryAsync(ipAddress);
                // hostEntry.HostName kann den primären Hostnamen zurückgeben.
                // hostEntry.Aliases kann zusätzliche Aliase enthalten, falls vorhanden.
                // Für PTR-Lookups ist meist der HostName das primäre Ergebnis.
                if (!string.IsNullOrWhiteSpace(hostEntry.HostName) && !hostEntry.HostName.Equals(targetIpAddress, StringComparison.OrdinalIgnoreCase))
                {
                    // Manchmal gibt GetHostEntryAsync die IP-Adresse zurück, wenn kein Name gefunden wurde.
                    // Wir wollen sicherstellen, dass wir einen tatsächlichen Namen haben.
                    return new ToolOutput { Success = true, ToolName = DisplayName, Data = new { IpAddress = targetIpAddress, HostName = hostEntry.HostName } };
                }
                else
                {
                    // Prüfen, ob Aliase vorhanden sind, falls HostName nicht aussagekräftig ist
                    if (hostEntry.Aliases.Length > 0)
                    {
                        return new ToolOutput { Success = true, ToolName = DisplayName, Data = new { IpAddress = targetIpAddress, HostName = hostEntry.Aliases[0], Aliases = hostEntry.Aliases } };
                    }
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Kein Hostname (PTR-Record) für die IP-Adresse {targetIpAddress} gefunden.", Data = new { IpAddress = targetIpAddress } };
                }
            }
            catch (SocketException ex)
            {
                // Dieser Fehler tritt häufig auf, wenn kein PTR-Record existiert oder der DNS-Server nicht antwortet.
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler beim Auflösen von {targetIpAddress}: {ex.Message} (Möglicherweise kein PTR-Record vorhanden oder DNS-Serverproblem)" };
            }
            catch (ArgumentException ex) // Für ungültige IP-Formate, die TryParse nicht abfängt
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler mit der IP-Adresse {targetIpAddress}: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Serverfehler ist aufgetreten: {ex.Message}" };
            }
        }
    }
}