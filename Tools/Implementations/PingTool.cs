// API_NetworkTools/Tools/Implementations/PingTool.cs
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Net;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;

namespace API_NetworkTools.Tools.Implementations
{
    public class PingTool : INetworkTool
    {
        public string Identifier => "ping";
        public string DisplayName => "Ping";
        public string Description => "Sendet ICMP Echo-Anfragen an einen Host.";

        public List<ToolParameterInfo> GetParameters() => new List<ToolParameterInfo>();

        public async Task<ToolOutput> ExecuteAsync(string target, Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = "Ziel (Host/IP) darf nicht leer sein." };
            }
            if (Uri.CheckHostName(target) == UriHostNameType.Unknown && !IPAddress.TryParse(target, out _))
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ungültiges Zielformat: {target}" };
            }
            try
            {
                using (Ping pingSender = new Ping())
                {
                    PingOptions pingOptions = new PingOptions();
                    byte[] buffer = new byte[32];
                    PingReply reply = await pingSender.SendPingAsync(target, 4000, buffer, pingOptions);

                    if (reply.Status == IPStatus.Success)
                    {
                        var resultData = new {
                            Target = target,
                            IpAddress = reply.Address?.ToString(),
                            RoundtripTime = reply.RoundtripTime,
                            Ttl = reply.Options?.Ttl,
                            Status = reply.Status.ToString()
                        };
                        return new ToolOutput { Success = true, ToolName = DisplayName, Data = resultData };
                    }
                    else
                    {
                        return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ping-Anfrage an {target} fehlgeschlagen mit Status: {reply.Status}", Data = new { Target = target, Status = reply.Status.ToString() } };
                    }
                }
            }
            catch (PingException ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ping-Fehler: {ex.InnerException?.Message ?? ex.Message}" };
            }
            catch (System.Exception ex)
            {
                return new ToolOutput { Success = false, ToolName = DisplayName, ErrorMessage = $"Ein unerwarteter Serverfehler ist aufgetreten: {ex.Message}" };
            }
        }
    }
}