// API_NetworkTools/Tools/Models/ToolInfo.cs
using System.Collections.Generic;

namespace API_NetworkTools.Tools.Models
{
    public class ToolInfo
    {
        public required string Identifier { get; set; }
        public required string DisplayName { get; set; }
        public string? Description { get; set; }
        public List<ToolParameterInfo> Parameters { get; set; } = new List<ToolParameterInfo>();
    }
}