// API_NetworkTools/Tools/Implementations/TracerouteTool.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models; // Diese using-Anweisung ist wichtig
// using Microsoft.Extensions.Logging; 

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
            // ... (Rest der Methode bleibt unverändert) ...
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
            // ... (Anfang der Methode bleibt unverändert) ...
            if (string.IsNullOrWhiteSpace(target))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Ziel (Host/IP) darf nicht leer sein." };
            }

            if (Uri.CheckHostName(target) == UriHostNameType.Unknown && !IPAddress.TryParse(target, out _))
            {
                 if (!target.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                 {
                    // Geänderte Fehlermeldung:
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiges Ziel (Hostname/IP): {target}" };
                 }
            }

            var allCollectedHops = new List<TracerouteHop>(); // Verwendet jetzt die ausgelagerte Klasse
            const int maxHops = 30;
            const int timeout = 4000; // in Millisekunden
            bool originalTargetReached = false;

            // ... (Rest der Methode bleibt unverändert, die Verwendung von TracerouteHop ist nun korrekt) ...
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
                        // Optional: break; oder continue; je nachdem, ob der Trace abgebrochen werden soll
                    }
                    catch (Exception ex)
                    {
                        // Fehler bei einem einzelnen Hop sollte nicht den gesamten Trace abbrechen,
                        // es sei denn, es ist ein Fehler, der weitere Pings unmöglich macht.
                        // Hier könnte man den Fehler loggen und den Hop als fehlerhaft markieren.
                         hop.Status = $"Unerwarteter Fehler bei Hop {ttl}: {ex.Message}";
                         hop.IpAddress = "Fehler";
                         allCollectedHops.Add(hop);
                        // return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Unerwarteter Fehler bei Hop {ttl}: {ex.Message}", Data = allCollectedHops };
                    }
                    await Task.Delay(50); // Kurze Pause zwischen den Pings
                }
            }

            List<TracerouteHop> filteredHopsToShow = new List<TracerouteHop>();
            int firstPublicHopIndex = -1;

            for (int i = 0; i < allCollectedHops.Count; i++)
            {
                if (!string.IsNullOrEmpty(allCollectedHops[i].IpAddress) &&
                    IPAddress.TryParse(allCollectedHops[i].IpAddress, out _) && // Stelle sicher, dass es eine gültige IP ist
                    !IsPrivateIpAddress(allCollectedHops[i].IpAddress))
                {
                    firstPublicHopIndex = i;
                    break;
                }
            }

            if (firstPublicHopIndex != -1)
            {
                // Nimm alle Hops ab dem ersten öffentlichen Hop
                filteredHopsToShow = allCollectedHops.GetRange(firstPublicHopIndex, allCollectedHops.Count - firstPublicHopIndex);
            }
            // Wenn kein öffentlicher Hop gefunden wurde, aber Hops gesammelt wurden (z.B. Ziel im LAN),
            // dann könnten wir entscheiden, alle Hops oder gar keine anzuzeigen.
            // Aktuell werden dann keine Hops in filteredHopsToShow sein, wenn alle privat sind.

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
            // Das Ziel gilt als erfolgreich erreicht, wenn der letzte Hop in allCollectedHops erfolgreich war
            // UND dieser Hop auch in den filteredHopsToShow enthalten ist (oder das Ziel im LAN lag und originalTargetReached true ist)
            if (originalTargetReached) {
                var targetHopInFullTrace = allCollectedHops.LastOrDefault(h => h.Status == "Erfolgreich" && h.IpAddress != "Fehler");
                if (targetHopInFullTrace != null) {
                    if (filteredHopsToShow.Any(fh => fh.Hop == targetHopInFullTrace.Hop && fh.IpAddress == targetHopInFullTrace.IpAddress)) {
                        finalSuccess = true; // Ziel erreicht und ist ein öffentlicher Hop
                    } else if (firstPublicHopIndex == -1) {
                        finalSuccess = true; // Ziel erreicht und war im privaten Netz (oder keine öffentlichen Hops davor)
                         filteredRawOutputBuilder.AppendLine("Ziel wurde erreicht, befand sich aber nicht unter den angezeigten öffentlichen Hops (z.B. Ziel im lokalen Netz oder Route nur über private IPs).");
                    }
                }
            }


            if (filteredHopsToShow.Any() && !finalSuccess && originalTargetReached) {
                // Dieser Fall sollte durch die Logik oben abgedeckt sein, aber als Fallback:
                filteredRawOutputBuilder.AppendLine("Ziel wurde erreicht, aber der letzte Hop war nicht in den angezeigten öffentlichen Hops.");
            } else if (filteredHopsToShow.Any() && !originalTargetReached) {
                filteredRawOutputBuilder.AppendLine("Ziel konnte innerhalb der angezeigten (öffentlichen) Hops nicht erreicht werden.");
            } else if (finalSuccess && filteredHopsToShow.Any()) {
                filteredRawOutputBuilder.AppendLine("Ziel erfolgreich innerhalb der angezeigten (öffentlichen) Hops erreicht.");
            } else if (finalSuccess && !filteredHopsToShow.Any() && firstPublicHopIndex == -1 && originalTargetReached) {
                 // Dieser Fall ist oben schon abgedeckt.
            } else if (!originalTargetReached && !allCollectedHops.Any()) {
                // Bereits oben abgedeckt: "Keine Hops konnten für {target} ermittelt werden."
            } else if (!originalTargetReached && allCollectedHops.Any() && !filteredHopsToShow.Any() && firstPublicHopIndex == -1) {
                // Alle Hops waren privat, Ziel nicht erreicht
                 filteredRawOutputBuilder.AppendLine("Ziel konnte nicht erreicht werden. Alle erfassten Hops waren im privaten Netzwerk.");
            } else if (!originalTargetReached && allCollectedHops.Any()) {
                // Ziel nicht erreicht, auch wenn es öffentliche Hops gab
                filteredRawOutputBuilder.AppendLine("Ziel konnte nicht erreicht werden.");
            }


            return new ToolOutput
            {
                Success = finalSuccess, // Erfolgreich, wenn das Ziel (originalTargetReached) erreicht wurde, unabhängig von der Filterung.
                                        // Oder spezifischer: erfolgreich, wenn Ziel erreicht UND in filteredHops (falls welche da sind)
                ToolName = DisplayName,
                Data = filteredHopsToShow.Any() ? filteredHopsToShow : allCollectedHops, // Zeige gefilterte Hops, oder alle falls keine öffentlichen da waren aber welche gesammelt wurden
                RawOutput = filteredRawOutputBuilder.ToString()
            };
        }
    }
    // Die TracerouteHop Klasse wurde hier entfernt
}