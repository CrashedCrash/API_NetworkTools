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
        private static readonly HttpClient httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false, 
            UseCookies = false
        })
        {
            Timeout = TimeSpan.FromSeconds(15) 
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
                    type: "select", 
                    isRequired: false,
                    defaultValue: "HEAD",
                    options: new List<string> { "HEAD", "GET" }
                )
            };
        }

        public async Task<ToolOutput> ExecuteAsync(string targetUrlInput, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(targetUrlInput))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "URL darf nicht leer sein." };
            }

            Uri? validatedUri = null;
            string attemptedUrl = targetUrlInput; // Wir verwenden eine neue Variable, um die ursprüngliche Eingabe bei Bedarf noch zu haben

            // Prüfen, ob bereits ein Schema vorhanden ist
            if (!targetUrlInput.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !targetUrlInput.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Kein Schema vorhanden, versuche zuerst https://
                string httpsUrl = "https://" + targetUrlInput;
                if (Uri.TryCreate(httpsUrl, UriKind.Absolute, out Uri? tempUriHttps) &&
                    (tempUriHttps.Scheme == Uri.UriSchemeHttp || tempUriHttps.Scheme == Uri.UriSchemeHttps))
                {
                    validatedUri = tempUriHttps;
                    attemptedUrl = httpsUrl;
                }
                else
                {
                    // Wenn https:// fehlschlägt, versuche http://
                    string httpUrl = "http://" + targetUrlInput;
                    if (Uri.TryCreate(httpUrl, UriKind.Absolute, out Uri? tempUriHttp) &&
                        (tempUriHttp.Scheme == Uri.UriSchemeHttp || tempUriHttp.Scheme == Uri.UriSchemeHttps))
                    {
                        validatedUri = tempUriHttp;
                        attemptedUrl = httpUrl;
                    }
                }
            }
            else
            {
                // Schema ist vorhanden, validiere die eingegebene URL direkt
                if (Uri.TryCreate(targetUrlInput, UriKind.Absolute, out Uri? tempUri) &&
                    (tempUri.Scheme == Uri.UriSchemeHttp || tempUri.Scheme == Uri.UriSchemeHttps))
                {
                    validatedUri = tempUri;
                    // attemptedUrl bleibt targetUrlInput
                }
            }

            // Wenn nach allen Versuchen keine gültige URI erstellt werden konnte
            if (validatedUri == null)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiges URL-Format für '{targetUrlInput}'. Bitte eine vollständige URL (z.B. https://example.com) oder einen gültigen Hostnamen eingeben." };
            }

            options.TryGetValue("method", out string? httpMethodString);
            bool useHeadMethod = "HEAD".Equals(httpMethodString, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(httpMethodString);
            HttpMethod httpMethod = useHeadMethod ? HttpMethod.Head : HttpMethod.Get;

            try
            {
                // Verwende die 'validatedUri' für die Anfrage
                using (var requestMessage = new HttpRequestMessage(httpMethod, validatedUri)) 
                {
                    requestMessage.Headers.UserAgent.TryParseAdd("API_NetworkTools-HttpHeaderTool/1.0");

                    HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

                    var headers = new Dictionary<string, List<string>>();
                    headers.Add("Status-Line", new List<string> { $"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}" });

                    foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
                    {
                        headers.Add(header.Key, header.Value.ToList());
                    }

                    if (response.Content?.Headers != null)
                    {
                        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
                        {
                            if (!headers.ContainsKey(header.Key))
                            {
                                headers.Add(header.Key, header.Value.ToList());
                            }
                            else
                            {
                                headers[header.Key].AddRange(header.Value);
                                headers[header.Key] = headers[header.Key].Distinct().ToList();
                            }
                        }
                    }
                    
                    StringBuilder rawOutputBuilder = new StringBuilder();
                    rawOutputBuilder.AppendLine($"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}");
                    foreach (var headerEntry in headers)
                    {
                        if (headerEntry.Key == "Status-Line") continue;
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
                string errorMessage = $"HTTP-Anfragefehler für '{attemptedUrl}': {httpEx.Message}";
                if (httpEx.InnerException is SocketException sockEx && 
                    (sockEx.SocketErrorCode == SocketError.HostNotFound || sockEx.SocketErrorCode == SocketError.TryAgain || sockEx.SocketErrorCode == SocketError.NoData))
                {
                     errorMessage = $"Fehler beim Auflösen des Hosts '{validatedUri.Host}' (von '{attemptedUrl}'): {sockEx.Message}";
                }
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = errorMessage };
            }
            catch (TaskCanceledException tex) 
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Die Anfrage an '{attemptedUrl}' hat das Zeitlimit überschritten. Fehler: {tex.Message}" };
            }
            catch (Exception ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Fehler ist bei der Anfrage an '{attemptedUrl}' aufgetreten: {ex.Message}" };
            }
        }
    }
}