// API_NetworkTools/Tools/Implementations/TracerouteTool.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public string DisplayName => "Traceroute";
        public string Description => "Verfolgt die Route von Paketen zu einem Netzwerkhost.";

        // Für Traceroute benötigen wir aktuell keine spezifischen Parameter außer dem Ziel.
        // Diese Methode könnte erweitert werden, um z.B. maxHops oder Timeout zu konfigurieren.
        public List<ToolParameterInfo> GetParameters() => new List<ToolParameterInfo>();

        public async Task<ToolOutput> ExecuteAsync(string target, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Ziel (Host/IP) darf nicht leer sein." };
            }

            // Überprüfen, ob das Ziel ein gültiger Hostname oder eine IP-Adresse ist.
            // Uri.CheckHostName gibt für reine IP-Adressen UriHostNameType.IPv4 oder UriHostNameType.IPv6 zurück.
            // IPAddress.TryParse prüft, ob es sich um eine valide IP-Adresse handelt.
            if (Uri.CheckHostName(target) == UriHostNameType.Unknown && !IPAddress.TryParse(target, out _))
            {
                 // Zusätzliche Prüfung für localhost, da CheckHostName dies als Unknown einstuft
                if (!target.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiges Zielformat: {target}" };
                }
            }

            var results = new List<TracerouteHop>();
            var rawOutputBuilder = new StringBuilder();
            const int maxHops = 30; // Maximale Anzahl an Hops
            const int timeout = 4000; // Timeout für jeden Ping-Versuch in Millisekunden

            rawOutputBuilder.AppendLine($"Traceroute wird für {target} mit maximal {maxHops} Hops ausgeführt:");

            using (Ping pingSender = new Ping())
            {
                for (int ttl = 1; ttl <= maxHops; ttl++)
                {
                    var hop = new TracerouteHop { Hop = ttl };
                    PingOptions pingOptions = new PingOptions(ttl, true); // don't fragment
                    byte[] buffer = Encoding.ASCII.GetBytes("TracerouteSample"); // Kleiner Puffer

                    try
                    {
                        PingReply reply = await pingSender.SendPingAsync(target, timeout, buffer, pingOptions);

                        if (reply.Address != null)
                        {
                            hop.IpAddress = reply.Address.ToString();
                        }

                        if (reply.Status == IPStatus.Success)
                        {
                            hop.RoundtripTime = reply.RoundtripTime;
                            hop.Status = "Erfolgreich";
                            results.Add(hop);
                            rawOutputBuilder.AppendLine($"{ttl}\t{hop.RoundtripTime} ms\t{hop.IpAddress ?? "-"}");
                            break; // Ziel erreicht
                        }
                        else if (reply.Status == IPStatus.TtlExpired)
                        {
                            // Dies ist der erwartete Status für intermediäre Hops
                            hop.RoundtripTime = reply.RoundtripTime;
                            hop.Status = "TTL abgelaufen";
                            results.Add(hop);
                            rawOutputBuilder.AppendLine($"{ttl}\t{hop.RoundtripTime} ms\t{hop.IpAddress ?? "-"}");
                        }
                        else if (reply.Status == IPStatus.TimedOut)
                        {
                            hop.Status = "Zeitüberschreitung";
                            results.Add(hop);
                            rawOutputBuilder.AppendLine($"{ttl}\t*\tZeitüberschreitung");
                        }
                        else
                        {
                            hop.Status = reply.Status.ToString();
                            results.Add(hop);
                            rawOutputBuilder.AppendLine($"{ttl}\t*\t{reply.Status}");
                        }
                    }
                    catch (PingException pEx)
                    {
                        // Manchmal kann SendPingAsync eine PingException werfen, z.B. wenn der Hostname nicht aufgelöst werden kann
                        // oder keine Route zum Host existiert.
                        hop.Status = $"Ping-Fehler: {pEx.InnerException?.Message ?? pEx.Message}";
                        hop.IpAddress = "Fehler";
                        results.Add(hop);
                        rawOutputBuilder.AppendLine($"{ttl}\t*\tPing-Fehler: {pEx.Message}");
                        // Bei bestimmten Fehlern (z.B. Host nicht erreichbar) könnte man hier abbrechen.
                        // Für eine einfache Traceroute versuchen wir es bis maxHops.
                    }
                    catch (Exception ex)
                    {
                        // Fange alle anderen Ausnahmen ab, um sicherzustellen, dass das Tool nicht abstürzt.
                        return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Unerwarteter Fehler bei Hop {ttl}: {ex.Message}", RawOutput = rawOutputBuilder.ToString(), Data = results };
                    }

                    // Kurze Pause, um das Netzwerk nicht zu überlasten und die Chance auf Antworten zu erhöhen
                    await Task.Delay(50);
                }
            }

            if (results.Count == 0) {
                 return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Keine Hops konnten für {target} ermittelt werden.", RawOutput = rawOutputBuilder.ToString() };
            }

            // Überprüfen, ob der letzte Hop das Ziel war
            var lastHop = results.LastOrDefault();
            bool targetReached = lastHop != null && lastHop.Status == "Erfolgreich";

            return new ToolOutput { Success = targetReached, ToolName = DisplayName, Data = results, RawOutput = rawOutputBuilder.ToString() };
        }
    }

    // Hilfsklasse zur Strukturierung der Traceroute-Ergebnisse
    public class TracerouteHop
    {
        public int Hop { get; set; }
        public string? IpAddress { get; set; }
        public long? RoundtripTime { get; set; } // In Millisekunden
        public string Status { get; set; } = string.Empty;
    }
}