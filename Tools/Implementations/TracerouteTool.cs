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
// using Microsoft.Extensions.Logging; // Einkommentieren, wenn Logging verwendet wird

namespace API_NetworkTools.Tools.Implementations
{
    public class TracerouteTool : INetworkTool
    {
        public string Identifier => "traceroute";
        public string DisplayName => "Traceroute (Filtert lokale Hops)";
        public string Description => "Verfolgt die Route zu einem Host und zeigt Hops ab dem ersten öffentlichen Netzwerkknoten.";

        // private readonly ILogger<TracerouteTool> _logger;
        // public TracerouteTool(ILogger<TracerouteTool> logger) { _logger = logger; }

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
                if (ipBytes[0] == 10) return true;
                if (ipBytes[0] == 172 && ipBytes[1] >= 16 && ipBytes[1] <= 31) return true;
                if (ipBytes[0] == 192 && ipBytes[1] == 168) return true;
                if (ipBytes[0] == 127) return true;
                if (ipBytes[0] == 169 && ipBytes[1] == 254) return true;
            }
            else if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) // IPv6
            {
                if (ipAddress.Equals(IPAddress.IPv6Loopback)) return true;

                byte[] ipBytes = ipAddress.GetAddressBytes();
                if (ipBytes[0] == 0xFE && (ipBytes[1] & 0xC0) == 0x80) return true; // Link-Local

                if ((ipBytes[0] & 0xFE) == 0xFC) return true; // ULA (fc00::/7)
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

            var allCollectedHops = new List<TracerouteHop>(); // hier TracerouteHop verwenden
            const int maxHops = 30;
            const int timeout = 4000;
            bool originalTargetReached = false;

            using (Ping pingSender = new Ping())
            {
                for (int ttl = 1; ttl <= maxHops; ttl++)
                {
                    var hop = new TracerouteHop { Hop = ttl }; // hier TracerouteHop verwenden
                    PingOptions pingOptions = new PingOptions(ttl, true);
                    byte[] buffer = Encoding.ASCII.GetBytes("TracerouteSample");
                    PingReply? reply = null;

                    try
                    {
                        reply = await pingSender.SendPingAsync(target, timeout, buffer, pingOptions);
                        if (reply.Address != null) hop.IpAddress = reply.Address.ToString();

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

            List<TracerouteHop> filteredHopsToShow = new List<TracerouteHop>(); // hier TracerouteHop verwenden
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
                foreach (var hop in filteredHopsToShow) // hier TracerouteHop verwenden
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
            } else if (filteredHopsToShow.Any() && !originalTargetReached && !finalSuccess) {
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
    } // Ende der TracerouteTool Klasse

    // Definition der TracerouteHop Klasse hier, wenn sie in derselben Datei bleiben soll:
    public class TracerouteHop
    {
        public int Hop { get; set; }
        public string? IpAddress { get; set; }
        public long? RoundtripTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}