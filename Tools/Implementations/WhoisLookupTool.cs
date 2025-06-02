// API_NetworkTools/Tools/Implementations/WhoisLookupTool.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;
using Whois.NET;           // Für WhoisClient
using System.Net.Sockets;  // Für SocketException
using System.Threading;    // Für CancellationToken

namespace API_NetworkTools.Tools.Implementations
{
    public class WhoisLookupTool : INetworkTool
    {
        public string Identifier => "whois-lookup";
        public string DisplayName => "Whois Lookup";
        public string Description => "Ruft öffentliche Registrierungsinformationen für einen Domainnamen ab.";

        public List<ToolParameterInfo> GetParameters()
        {
            return new List<ToolParameterInfo>();
        }

        public async Task<ToolOutput> ExecuteAsync(string targetDomain, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(targetDomain))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Domainname darf nicht leer sein." };
            }

            var hostNameType = Uri.CheckHostName(targetDomain);
            if (hostNameType == UriHostNameType.Unknown || hostNameType == UriHostNameType.IPv4 || hostNameType == UriHostNameType.IPv6)
            {
                if (!targetDomain.Contains(".") || targetDomain.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                     // Geänderte Fehlermeldung:
                     return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiges Ziel (Domain): {targetDomain}" };
                }
            }

            try
            {
                // Wir nehmen die Warnung CS0618 (obsolete) in Kauf, um Kompilierfehler zu vermeiden,
                // da die empfohlene Überladung mit 'token' Parameter Probleme gemacht hat.
                // Die parameterlose Version sollte für die meisten Fälle funktionieren.
                var result = await WhoisClient.QueryAsync(targetDomain, options: null, token: CancellationToken.None);

                if (result != null && !string.IsNullOrWhiteSpace(result.Raw))
                {
                    var data = new {
                        Domain = targetDomain,
                        // OrganizationName ist in WhoisClient.NET v6.1.0 nicht direkt auf WhoisResponse verfügbar.
                        // Wir lassen das Data-Objekt sehr minimal, um Fehler zu vermeiden.
                    };
                    
                    return new ToolOutput { Success = true, ToolName = DisplayName, RawOutput = result.ToString(), Data = data };
                }
                else
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Keine Whois-Informationen für {targetDomain} gefunden oder die Antwort war leer/ungültig." };
                }
            }
            catch (SocketException sockEx) 
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Netzwerkfehler (SocketException) für {targetDomain}: {sockEx.Message}" };
            }
            // Da wir Probleme mit dem Namespace für WhoisException haben, entfernen wir den spezifischen Catch-Block.
            // Der allgemeine Exception-Handler wird alle Fehler der Whois-Bibliothek fangen.
            // catch (Whois.NET.Exceptions.WhoisException whoisEx) 
            // {
            //      return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Whois-Fehler für {targetDomain}: {whoisEx.Message}" };
            // }
            catch (Exception ex) 
            {
                // Wir geben den Typnamen der Exception aus, um ggf. doch noch herauszufinden, ob eine spezifische WhoisException geworfen wird.
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein Fehler ({ex.GetType().FullName}) ist beim Whois-Lookup aufgetreten: {ex.Message}" };
            }
        }
    }
}