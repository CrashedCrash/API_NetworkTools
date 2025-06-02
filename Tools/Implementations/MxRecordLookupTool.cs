// API_NetworkTools/Tools/Implementations/MxRecordLookupTool.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;
using DnsClient; // <-- Wichtig: Namespace für DnsClient.NET
using DnsClient.Protocol; // <-- Wichtig: Für MxRecord und andere Record-Typen

namespace API_NetworkTools.Tools.Implementations
{
    public class MxRecordLookupTool : INetworkTool
    {
        public string Identifier => "mx-lookup";
        public string DisplayName => "MX Record Lookup";
        public string Description => "Findet die Mail Exchange (MX) Records für eine Domain.";

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

            // Einfache Validierung für einen Domainnamen (kann bei Bedarf erweitert werden)
            if (Uri.CheckHostName(targetDomain) == UriHostNameType.Unknown && !targetDomain.Contains("."))
            {
                 // CheckHostName ist für reine Domain-Namen manchmal zu streng, daher zusätzliche Prüfung
                if (!targetDomain.Equals("localhost", StringComparison.OrdinalIgnoreCase)) // localhost ist kein gültiges Ziel für MX
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiger Domainname: {targetDomain}" };
                }
            }


            try
            {
                var lookup = new LookupClient();
                var result = await lookup.QueryAsync(targetDomain, QueryType.MX);

                if (result.HasError)
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler bei der DNS-Abfrage: {result.ErrorMessage}" };
                }

                var mxRecords = result.Answers.MxRecords().ToList();

                if (mxRecords.Any())
                {
                    var formattedRecords = mxRecords
                                            .OrderBy(r => r.Preference)
                                            .Select(r => new { Preference = r.Preference, Exchange = r.Exchange.Value })
                                            .ToList();
                    return new ToolOutput { Success = true, ToolName = DisplayName, Data = formattedRecords };
                }
                else
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Keine MX-Records für {targetDomain} gefunden.", Data = new List<object>() };
                }
            }
            catch (DnsResponseException dnsEx) // Spezifische Exception von DnsClient.NET
            {
                 return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"DNS-Abfragefehler für {targetDomain}: {dnsEx.Message} (Möglicherweise existiert die Domain nicht oder hat keine MX-Records)" };
            }
            catch (Exception ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}" };
            }
        }
    }
}