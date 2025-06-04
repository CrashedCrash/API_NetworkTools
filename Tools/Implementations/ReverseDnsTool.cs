// API_NetworkTools/Tools/Implementations/ReverseDnsTool.cs
using System;
using System.Collections.Generic;
using System.Linq; // Erforderlich für .FirstOrDefault()
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
        public string Description => "Ermittelt den Hostnamen zu einer gegebenen IP-Adresse oder Domain.";

        public List<ToolParameterInfo> GetParameters()
        {
            // Für dieses Tool benötigen wir keine zusätzlichen Parameter neben dem 'target'.
            return new List<ToolParameterInfo>();
        }

        public async Task<ToolOutput> ExecuteAsync(string targetInput, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(targetInput))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Ziel (IP-Adresse oder Domain) darf nicht leer sein." };
            }

            IPAddress? ipAddressToLookup = null;
            string originalInput = targetInput;
            bool inputWasHostname = false;

            // Prüfen, ob die Eingabe eine IP-Adresse ist
            if (IPAddress.TryParse(targetInput, out IPAddress? parsedIp))
            {
                ipAddressToLookup = parsedIp;
            }
            else
            {
                // Eingabe ist keine IP, also als Hostname behandeln und versuchen aufzulösen
                inputWasHostname = true;
                try
                {
                    IPHostEntry hostEntryForIpResolution = await Dns.GetHostEntryAsync(targetInput);
                    if (hostEntryForIpResolution.AddressList.Any())
                    {
                        // Bevorzuge IPv4, wenn verfügbar, ansonsten die erste Adresse
                        ipAddressToLookup = hostEntryForIpResolution.AddressList.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork)
                                            ?? hostEntryForIpResolution.AddressList[0];
                    }
                    else
                    {
                        return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Hostname '{targetInput}' konnte nicht zu einer IP-Adresse aufgelöst werden." };
                    }
                }
                catch (SocketException ex)
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler beim Auflösen des Hostnamens '{targetInput}': {ex.Message} (Möglicherweise existiert die Domain nicht oder es gibt ein DNS-Problem)" };
                }
                catch (Exception ex) // Andere unerwartete Fehler bei der DNS-Auflösung
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Fehler ist beim Auflösen von '{targetInput}' aufgetreten: {ex.Message}" };
                }
            }

            if (ipAddressToLookup == null) // Sollte durch die obige Logik eigentlich nicht eintreten
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Konnte keine gültige IP-Adresse für den Lookup ermitteln." };
            }

            try
            {
                IPHostEntry hostEntry = await Dns.GetHostEntryAsync(ipAddressToLookup);
                string? foundHostName = null;
                string[]? aliases = null;

                if (!string.IsNullOrWhiteSpace(hostEntry.HostName) && !hostEntry.HostName.Equals(ipAddressToLookup.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    foundHostName = hostEntry.HostName;
                }
                else if (hostEntry.Aliases.Any()) // Prüfen, ob Aliase vorhanden sind
                {
                    foundHostName = hostEntry.Aliases[0]; // Nimm den ersten Alias als primären Hostnamen
                    if (hostEntry.Aliases.Length > 1)
                    {
                        aliases = hostEntry.Aliases;
                    }
                }

                if (foundHostName != null)
                {
                    var data = new {
                        QueriedIp = ipAddressToLookup.ToString(),
                        HostName = foundHostName,
                        Aliases = aliases,
                        OriginalInput = inputWasHostname ? originalInput : null // Zeige die ursprüngliche Eingabe, wenn es ein Hostname war
                    };
                    return new ToolOutput { Success = true, ToolName = DisplayName, Data = data };
                }
                else
                {
                    string message = $"Kein Hostname (PTR-Record) für die IP-Adresse {ipAddressToLookup} gefunden.";
                    if (inputWasHostname)
                    {
                        message += $" (Aufgelöst von Domain: '{originalInput}')";
                    }
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = message, Data = new { IpAddress = ipAddressToLookup.ToString(), OriginalInput = inputWasHostname ? originalInput : null } };
                }
            }
            catch (SocketException ex)
            {
                string message = $"Fehler beim Reverse-DNS-Lookup für {ipAddressToLookup}: {ex.Message}";
                if (inputWasHostname)
                {
                    message += $" (Aufgelöst von Domain: '{originalInput}')";
                }
                message += " (Möglicherweise kein PTR-Record vorhanden oder DNS-Serverproblem)";
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = message };
            }
            catch (ArgumentException ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler mit der IP-Adresse {ipAddressToLookup}: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Serverfehler ist aufgetreten: {ex.Message}" };
            }
        }
    }
}