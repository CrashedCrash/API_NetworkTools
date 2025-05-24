using System.Collections.Generic;
using System.Threading.Tasks;
// Stelle sicher, dass der Namespace zu deinem neuen Projektnamen passt
using API_NetworkTools.Tools.Models; // Für ToolParameterInfo und ToolOutput

namespace API_NetworkTools.Tools.Interfaces
{
    public interface INetworkTool
    {
        string Identifier { get; }
        string DisplayName { get; }
        string Description { get; }
        List<ToolParameterInfo> GetParameters();
        Task<ToolOutput> ExecuteAsync(string target, Dictionary<string, string> options);
    }
}