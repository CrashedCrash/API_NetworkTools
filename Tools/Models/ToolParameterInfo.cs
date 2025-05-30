// API_NetworkTools/Tools/Models/ToolParameterInfo.cs
using System.Collections.Generic;

namespace API_NetworkTools.Tools.Models
{
    public class ToolParameterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public List<string> Options { get; set; }

        public ToolParameterInfo(string name, string label, string type, bool isRequired = true, string? defaultValue = null, List<string>? options = null)
        {
            Name = name;
            Label = label;
            Type = type;
            IsRequired = isRequired;
            DefaultValue = defaultValue;
            Options = options ?? new List<string>();
        }
    }
}