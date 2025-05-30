using System.Collections.Generic;
using System.Threading.Tasks;
using API_NetworkTools.Tools.Models;

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