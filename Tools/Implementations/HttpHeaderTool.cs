// API_NetworkTools/Tools/Implementations/HttpHeaderTool.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;
using System.Net.Sockets;

namespace API_NetworkTools.Tools.Implementations
{
    public class HttpHeaderTool : INetworkTool
    {
        // HttpClient sollte als Singleton oder statisch instanziiert werden, wenn möglich.
        // Für die Tool-Struktur hier erstellen wir ihn pro Aufruf, aber mit PooledConnectionLifetime.
        // Eine bessere Lösung wäre, IHttpClientFactory zu verwenden, wenn die Tools als Scoped/Transient Services komplexer werden.
        private static readonly HttpClient httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false, // Wichtig, um Redirect-Header sehen zu können, anstatt ihnen zu folgen
            UseCookies = false
        })
        {
            Timeout = TimeSpan.FromSeconds(15) // Timeout für die gesamte Anfrage
        };

        public string Identifier => "http-headers";
        public string DisplayName => "HTTP Header Viewer";
        public string Description => "Zeigt die HTTP-Antwort-Header von einer URL an.";

        public List<ToolParameterInfo> GetParameters()
        {
            return new List<ToolParameterInfo>
            {
                new ToolParameterInfo(
                    name: "method",
                    label: "HTTP Method (HEAD oder GET)",
                    type: "select", // Typ 'select' für eine Dropdown-Liste in der UI
                    isRequired: false,
                    defaultValue: "HEAD",
                    options: new List<string> { "HEAD", "GET" }
                )
            };
        }

        public async Task<ToolOutput> ExecuteAsync(string targetUrl, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "URL darf nicht leer sein." };
            }

            if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Ungültiges URL-Format. Bitte eine vollständige URL mit http:// oder https:// eingeben." };
            }

            options.TryGetValue("method", out string? httpMethodString);
            bool useHeadMethod = "HEAD".Equals(httpMethodString, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(httpMethodString);

            HttpMethod httpMethod = useHeadMethod ? HttpMethod.Head : HttpMethod.Get;

            try
            {
                using (var requestMessage = new HttpRequestMessage(httpMethod, uri))
                {
                    // Standard-User-Agent setzen, um Blockaden durch einige Server zu vermeiden
                    requestMessage.Headers.UserAgent.TryParseAdd("API_NetworkTools-HttpHeaderTool/1.0");

                    HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

                    var headers = new Dictionary<string, List<string>>();
                    // Statuszeile hinzufügen
                    headers.Add("Status-Line", new List<string> { $"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}" });

                    // Response Headers
                    foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
                    {
                        headers.Add(header.Key, header.Value.ToList());
                    }

                    // Content Headers (falls vorhanden, auch bei HEAD-Anfragen können einige Content-Header gesendet werden)
                    if (response.Content?.Headers != null)
                    {
                        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
                        {
                            if (!headers.ContainsKey(header.Key)) // Füge nur hinzu, wenn nicht bereits als Response-Header vorhanden
                            {
                                headers.Add(header.Key, header.Value.ToList());
                            }
                            else // Manchmal gibt es Header sowohl im Response- als auch im Content-Teil, z.B. "Content-Type"
                            {
                                headers[header.Key].AddRange(header.Value);
                                headers[header.Key] = headers[header.Key].Distinct().ToList(); // Duplikate entfernen
                            }
                        }
                    }
                    
                    // Erstelle einen RawOutput String
                    StringBuilder rawOutputBuilder = new StringBuilder();
                    rawOutputBuilder.AppendLine($"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}");
                    foreach (var headerEntry in headers)
                    {
                        if (headerEntry.Key == "Status-Line") continue; // Bereits oben hinzugefügt
                        foreach (var value in headerEntry.Value)
                        {
                            rawOutputBuilder.AppendLine($"{headerEntry.Key}: {value}");
                        }
                    }


                    return new ToolOutput { Success = true, ToolName = DisplayName, Data = headers, RawOutput = rawOutputBuilder.ToString() };
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Speziellere Fehlermeldung für DNS-Probleme innerhalb einer HttpRequestException
                if (httpEx.InnerException is SocketException sockEx && 
                    (sockEx.SocketErrorCode == SocketError.HostNotFound || sockEx.SocketErrorCode == SocketError.TryAgain || sockEx.SocketErrorCode == SocketError.NoData))
                {
                     return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Fehler beim Auflösen des Hosts '{uri.Host}': {sockEx.Message}" };
                }
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"HTTP-Anfragefehler: {httpEx.Message}" };
            }
            catch (TaskCanceledException tex) // Tritt auf, wenn das HttpClient-Timeout erreicht wird
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Die Anfrage an {targetUrl} hat das Zeitlimit überschritten. Fehler: {tex.Message}" };
            }
            catch (Exception ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}" };
            }
        }
    }
}