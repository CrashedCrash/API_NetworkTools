// API_NetworkTools/Tools/Models/TracerouteHop.cs
namespace API_NetworkTools.Tools.Models
{
    public class TracerouteHop
    {
        public int Hop { get; set; }
        public string? IpAddress { get; set; }
        public long? RoundtripTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}