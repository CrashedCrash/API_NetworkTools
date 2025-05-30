// API_NetworkTools/Tools/Models/ToolOutput.cs
namespace API_NetworkTools.Tools.Models
{
    public class ToolOutput
    {
        public bool Success { get; set; }
        public required string ToolName { get; set; }
        public object? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawOutput { get; set; }
    }
}