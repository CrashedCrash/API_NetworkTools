// API_NetworkTools/Tools/Implementations/PortScannerTool.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;

namespace API_NetworkTools.Tools.Implementations
{
    public class PortScannerTool : INetworkTool
    {
        public string Identifier => "port-scan";
        public string DisplayName => "Port Scanner";
        public string Description => "Überprüft den Status von TCP-Ports auf einem Zielhost. (Verantwortungsbewusst einsetzen!)";

        public List<ToolParameterInfo> GetParameters()
        {
            return new List<ToolParameterInfo>
            {
                new ToolParameterInfo(
                    name: "ports",
                    label: "Ports (kommagetrennt, z.B. 80,443,22)",
                    type: "text",
                    isRequired: true,
                    defaultValue: "80,443"
                )
            };
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
                    // Geänderte Fehlermeldung:
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiges Ziel (Hostname/IP): {target}" };
                 }
            }

            if (options == null || !options.TryGetValue("ports", out var portsString) || string.IsNullOrWhiteSpace(portsString))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Der Parameter 'ports' muss angegeben werden und darf nicht leer sein." };
            }

            List<int> portsToScan = new List<int>();
            foreach (var portStr in portsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(portStr.Trim(), out int port) && port > 0 && port <= 65535)
                {
                    portsToScan.Add(port);
                }
                else
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiger Portwert gefunden: '{portStr}'. Ports müssen Zahlen zwischen 1 und 65535 sein." };
                }
            }

            if (!portsToScan.Any())
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Keine gültigen Ports zum Scannen angegeben." };
            }

            var results = new List<PortScanResult>();
            // Standard-Timeout pro Port in Millisekunden
            int timeoutMilliseconds = options.TryGetValue("timeout", out var timeoutStr) && int.TryParse(timeoutStr, out int customTimeout) ? customTimeout : 2000;


            // IP-Adresse im Voraus auflösen, um DNS-Lookups nicht für jeden Port wiederholen zu müssen
            IPAddress[] ipAddresses;
            try
            {
                ipAddresses = await Dns.GetHostAddressesAsync(target);
                if (ipAddresses == null || ipAddresses.Length == 0)
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Hostname {target} konnte nicht aufgelöst werden." };
                }
            }
            catch (SocketException ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler beim Auflösen von {target}: {ex.Message}" };
            }
            catch (Exception ex)
            {
                 return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Unerwarteter Fehler beim Auflösen von {target}: {ex.Message}" };
            }

            // Nutze die erste aufgelöste IP-Adresse (typischerweise die primäre)
            IPAddress targetIp = ipAddresses[0];

            foreach (int port in portsToScan)
            {
                using (var cts = new CancellationTokenSource(timeoutMilliseconds))
                using (var client = new TcpClient())
                {
                    string status;
                    try
                    {
                        // ConnectAsync mit CancellationToken für Timeout-Steuerung
                        await client.ConnectAsync(targetIp, port, cts.Token);
                        status = "Offen";
                    }
                    catch (OperationCanceledException) when (cts.IsCancellationRequested)
                    {
                        status = "Timeout/Gefiltert";
                    }
                    catch (SocketException ex)
                    {
                        // ConnectionRefused deutet typischerweise auf einen geschlossenen Port hin
                        if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                        {
                            status = "Geschlossen";
                        }
                        // HostUnreachable oder NetworkUnreachable können ebenfalls auftreten
                        else if (ex.SocketErrorCode == SocketError.HostUnreachable || ex.SocketErrorCode == SocketError.NetworkUnreachable)
                        {
                            status = $"Unerreichbar ({ex.SocketErrorCode})";
                        }
                        else
                        {
                            status = $"Fehler ({ex.SocketErrorCode})";
                        }
                    }
                    catch (Exception) // Andere unerwartete Fehler
                    {
                        status = "Fehler (Unbekannt)";
                    }
                    results.Add(new PortScanResult { Port = port, Status = status, Target = target, ResolvedIp = targetIp.ToString() });
                }
            }

            return new ToolOutput { Success = true, ToolName = DisplayName, Data = results };
        }
    }

    // Hilfsklasse für die Ergebnisse
    public class PortScanResult
    {
        public required string Target { get; set; }
        public required string ResolvedIp {get; set; }
        public int Port { get; set; }
        public required string Status { get; set; }
    }
}