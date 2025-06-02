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
                     return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiges Ziel (Domain): {targetDomain}" };
                }
            }

            try
            {
                // 'result' wird hier deklariert und ist im gesamten try-Block gültig.
                var result = await WhoisClient.QueryAsync(targetDomain, options: null, token: CancellationToken.None);

                if (result != null && !string.IsNullOrWhiteSpace(result.Raw))
                {
                    // 'data' wird hier deklariert und ist innerhalb dieses if-Blocks gültig.
                    var data = new {
                        Domain = targetDomain
                    };
                    
                    // Diese Zeile verwendet 'result.Raw' und 'data'. Beide sind hier im korrekten Scope.
                    // Dies ist wahrscheinlich die Zeile, die Fehler verursacht hat (Zeile 28 laut deiner Meldung).
                    return new ToolOutput { Success = true, ToolName = DisplayName, RawOutput = result.Raw, Data = data };
                }
                else
                {
                    // 'result' ist hier immer noch im Scope (kann null sein oder Raw ist leer).
                    // 'data' wurde in diesem Zweig nicht deklariert.
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Keine Whois-Informationen für {targetDomain} gefunden oder die Antwort war leer/ungültig." };
                }
            }
            catch (SocketException sockEx) 
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Netzwerkfehler (SocketException) für {targetDomain}: {sockEx.Message}" };
            }
            catch (Exception ex) 
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein Fehler ({ex.GetType().FullName}) ist beim Whois-Lookup aufgetreten: {ex.Message}" };
            }
        }
    }
}