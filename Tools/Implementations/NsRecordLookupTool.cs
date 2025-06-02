// API_NetworkTools/Tools/Implementations/NsRecordLookupTool.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;
using DnsClient; // Namespace für DnsClient.NET
using DnsClient.Protocol; // Für NsRecord und andere Record-Typen

namespace API_NetworkTools.Tools.Implementations
{
    public class NsRecordLookupTool : INetworkTool
    {
        public string Identifier => "ns-lookup";
        public string DisplayName => "NS Record Lookup";
        public string Description => "Findet die Name Server (NS) Records für eine Domain.";

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

            if (Uri.CheckHostName(targetDomain) == UriHostNameType.Unknown && !targetDomain.Contains("."))
            {
                if (!targetDomain.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    // Geänderte Fehlermeldung:
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiges Ziel (Domain): {targetDomain}" };
                }
            }

            try
            {
                var lookup = new LookupClient();
                var result = await lookup.QueryAsync(targetDomain, QueryType.NS);

                if (result.HasError)
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler bei der DNS-Abfrage: {result.ErrorMessage}" };
                }

                var nsRecords = result.Answers.NsRecords().ToList();

                if (nsRecords.Any())
                {
                    var formattedRecords = nsRecords
                                            .Select(r => r.NSDName.Value) // NSDName enthält den Hostnamen des Nameservers
                                            .ToList();
                    return new ToolOutput { Success = true, ToolName = DisplayName, Data = formattedRecords };
                }
                else
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Keine NS-Records für {targetDomain} gefunden.", Data = new List<string>() };
                }
            }
            catch (DnsResponseException dnsEx)
            {
                 return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"DNS-Abfragefehler für {targetDomain}: {dnsEx.Message} (Möglicherweise existiert die Domain nicht oder hat keine NS-Records)" };
            }
            catch (Exception ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}" };
            }
        }
    }
}