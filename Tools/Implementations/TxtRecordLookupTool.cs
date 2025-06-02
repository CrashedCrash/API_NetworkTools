// API_NetworkTools/Tools/Implementations/TxtRecordLookupTool.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;
using DnsClient; // Namespace für DnsClient.NET
using DnsClient.Protocol; // Für TxtRecord und andere Record-Typen

namespace API_NetworkTools.Tools.Implementations
{
    public class TxtRecordLookupTool : INetworkTool
    {
        public string Identifier => "txt-lookup";
        public string DisplayName => "TXT Record Lookup";
        public string Description => "Findet die Text (TXT) Records für eine Domain.";

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
                var result = await lookup.QueryAsync(targetDomain, QueryType.TXT);

                if (result.HasError)
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler bei der DNS-Abfrage: {result.ErrorMessage}" };
                }

                var txtRecords = result.Answers.TxtRecords().ToList();

                if (txtRecords.Any())
                {
                    // Ein einzelner TXT-Record kann aus mehreren Zeichenfolgen bestehen.
                    // DnsClient.NET gibt diese als IReadOnlyList<string> in TxtRecord.Text zurück.
                    // Wir fügen sie für die Anzeige zu einem einzelnen String pro Record zusammen.
                    var formattedRecords = txtRecords
                                            .Select(r => string.Join("", r.Text)) // Fügt die Teile eines Records zusammen
                                            .ToList();
                    return new ToolOutput { Success = true, ToolName = DisplayName, Data = formattedRecords };
                }
                else
                {
                    return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Keine TXT-Records für {targetDomain} gefunden.", Data = new List<string>() };
                }
            }
            catch (DnsResponseException dnsEx)
            {
                 return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"DNS-Abfragefehler für {targetDomain}: {dnsEx.Message} (Möglicherweise existiert die Domain nicht oder hat keine TXT-Records)" };
            }
            catch (Exception ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}" };
            }
        }
    }
}