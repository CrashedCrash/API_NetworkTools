// API_NetworkTools/Tools/Implementations/TracerouteTool.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;

namespace API_NetworkTools.Tools.Implementations
{
    public class TracerouteTool : INetworkTool
    {
        public string Identifier => "traceroute";
        public string DisplayName => "Traceroute (Filtert lokale Hops)";
        public string Description => "Verfolgt die Route zu einem Host und zeigt Hops ab dem ersten öffentlichen Netzwerkknoten.";
        public List<ToolParameterInfo> GetParameters() => new List<ToolParameterInfo>();

        private bool IsPrivateIpAddress(string? ipString)
        {
            if (string.IsNullOrEmpty(ipString) || !IPAddress.TryParse(ipString, out IPAddress? ipAddress))
            {
                return false;
            }

            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // IPv4
            {
                byte[] ipBytes = ipAddress.GetAddressBytes();
                // 10.0.0.0    - 10.255.255.255  (10.0.0.0/8)
                if (ipBytes[0] == 10) return true;
                // 172.16.0.0  - 172.31.255.255 (172.16.0.0/12)
                if (ipBytes[0] == 172 && ipBytes[1] >= 16 && ipBytes[1] <= 31) return true;
                // 192.168.0.0 - 192.168.255.255 (192.168.0.0/16)
                if (ipBytes[0] == 192 && ipBytes[1] == 168) return true;
                // 127.0.0.0   - 127.255.255.255 (Loopback)
                if (ipBytes[0] == 127) return true;
                // 169.254.0.0 - 169.254.255.255 (APIPA / Link-Local)
                if (ipBytes[0] == 169 && ipBytes[1] == 254) return true;
            }
            else if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) // IPv6
            {
                if (ipAddress.IsIPv6Loopback) return true;       // ::1/128
                if (ipAddress.IsIPv6LinkLocal) return true;      // fe80::/10

                // Prüfung für Unique Local Addresses (ULA) fc00::/7
                // Die ersten 7 Bits sind 1111110. Das bedeutet, das erste Byte ist 0xFC oder 0xFD.
                byte[] ipBytes = ipAddress.GetAddressBytes();
                if ((ipBytes[0] & 0xFE) == 0xFC) // 0xFE ist 11111110. Maskiert das letzte Bit.
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<ToolOutput> ExecuteAsync(string target, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Ziel (Host/IP) darf nicht leer sein." };
            }

            if (Uri.CheckHostName(target) == UriHostNameType.Unknown && !IPAddress.TryParse(target, out _))
            {
                if (!target.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiges Zielformat: {target}" };
                }
            }

            var allCollectedHops = new List<TracerouteHop>();
            const int maxHops = 30;
            const int timeout = 4000;
            bool originalTargetReached = false;

            using (Ping pingSender = new Ping())
            {
                for (int ttl = 1; ttl <= maxHops; ttl++)
                {
                    var hop = new TracerouteHop { Hop = ttl };
                    PingOptions pingOptions = new PingOptions(ttl, true);
                    byte[] buffer = Encoding.ASCII.GetBytes("TracerouteSample");
                    PingReply? reply = null;

                    try
                    {
                        reply = await pingSender.SendPingAsync(target, timeout, buffer, pingOptions);

                        if (reply.Address != null)
                        {
                            hop.IpAddress = reply.Address.ToString();
                        }

                        if (reply.Status == IPStatus.Success)
                        {
                            hop.RoundtripTime = reply.RoundtripTime;
                            hop.Status = "Erfolgreich";
                            allCollectedHops.Add(hop);
                            originalTargetReached = true;
                            break; 
                        }
                        else if (reply.Status == IPStatus.TtlExpired)
                        {
                            hop.RoundtripTime = reply.RoundtripTime;
                            hop.Status = "TTL abgelaufen";
                            allCollectedHops.Add(hop);
                        }
                        else if (reply.Status == IPStatus.TimedOut)
                        {
                            hop.Status = "Zeitüberschreitung";
                            allCollectedHops.Add(hop);
                        }
                        else
                        {
                            hop.Status = reply.Status.ToString();
                            allCollectedHops.Add(hop);
                        }
                    }
                    catch (PingException pEx)
                    {
                        hop.Status = $"Ping-Fehler: {pEx.InnerException?.Message ?? pEx.Message}";
                        hop.IpAddress = "Fehler";
                        allCollectedHops.Add(hop);
                    }
                    catch (Exception ex)
                    {
                        return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Unerwarteter Fehler bei Hop {ttl}: {ex.Message}", Data = allCollectedHops };
                    }
                    await Task.Delay(50);
                }
            }

            List<TracerouteHop> filteredHopsToShow = new List<TracerouteHop>();
            int firstPublicHopIndex = -1;

            for (int i = 0; i < allCollectedHops.Count; i++)
            {
                if (!string.IsNullOrEmpty(allCollectedHops[i].IpAddress) &&
                    IPAddress.TryParse(allCollectedHops[i].IpAddress, out _) && 
                    !IsPrivateIpAddress(allCollectedHops[i].IpAddress))
                {
                    firstPublicHopIndex = i;
                    break;
                }
            }

            if (firstPublicHopIndex != -1)
            {
                filteredHopsToShow = allCollectedHops.GetRange(firstPublicHopIndex, allCollectedHops.Count - firstPublicHopIndex);
            }
            
            StringBuilder filteredRawOutputBuilder = new StringBuilder();
            filteredRawOutputBuilder.AppendLine($"Traceroute für {target} (zeigt Hops ab dem ersten öffentlichen Netzwerkknoten):");

            if (filteredHopsToShow.Any())
            {
                foreach (var hop in filteredHopsToShow)
                {
                    string rttDisplay = hop.RoundtripTime.HasValue ? $"{hop.RoundtripTime} ms" : "*";
                    string ipDisplay = hop.IpAddress ?? "*"; 
                    filteredRawOutputBuilder.AppendLine($"{hop.Hop}\t{rttDisplay}\t{ipDisplay}\t({hop.Status})");
                }
            }
            else if (allCollectedHops.Any() && firstPublicHopIndex == -1)
            {
                filteredRawOutputBuilder.AppendLine("Keine öffentlichen Netzwerkknoten auf der Route zum Ziel gefunden. Alle Hops waren im lokalen/privaten Netzwerk oder konnten nicht als öffentlich identifiziert werden.");
            }
            else if (!allCollectedHops.Any())
            {
                 filteredRawOutputBuilder.AppendLine($"Keine Hops konnten für {target} ermittelt werden.");
            }

            bool finalSuccess = false;
            if (originalTargetReached)
            {
                var targetHopInFullTrace = allCollectedHops.LastOrDefault(h => h.Status == "Erfolgreich");
                if (targetHopInFullTrace != null && filteredHopsToShow.Any(fh => fh.Hop == targetHopInFullTrace.Hop && fh.IpAddress == targetHopInFullTrace.IpAddress))
                {
                    finalSuccess = true;
                }
            }
            
            if (filteredHopsToShow.Any() && !finalSuccess && originalTargetReached) {
                 filteredRawOutputBuilder.AppendLine("Ziel wurde erreicht, befand sich aber nicht unter den angezeigten öffentlichen Hops (z.B. Ziel im lokalen Netz).");
            } else if (filteredHopsToShow.Any() && !originalTargetReached && !finalSuccess) { // Hinzugefügt: !finalSuccess um Dopplung zu vermeiden
                filteredRawOutputBuilder.AppendLine("Ziel konnte innerhalb der angezeigten Hops nicht erreicht werden.");
            } else if (finalSuccess) {
                 filteredRawOutputBuilder.AppendLine("Ziel erfolgreich innerhalb der angezeigten Hops erreicht.");
            }


            return new ToolOutput
            {
                Success = finalSuccess,
                ToolName = DisplayName,
                Data = filteredHopsToShow,
                RawOutput = filteredRawOutputBuilder.ToString()
            };
        }
    }
}