// API_NetworkTools/Controllers/ToolsController.cs
using Microsoft.AspNetCore.Mvc;
using API_NetworkTools.Tools.Interfaces;
using API_NetworkTools.Tools.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_NetworkTools.Controllers
{
    [ApiController]
    [Route("api/tools")]
    public class ToolsController : ControllerBase
    {
        private readonly IEnumerable<INetworkTool> _networkTools;

        public ToolsController(IEnumerable<INetworkTool> networkTools)
        {
            _networkTools = networkTools;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ToolInfo>), 200)]
        public IActionResult GetAvailableTools()
        {
            return Ok(_networkTools.Select(tool => new ToolInfo
            {
                Identifier = tool.Identifier,
                DisplayName = tool.DisplayName,
                Description = tool.Description,
                Parameters = tool.GetParameters()
            }).ToList());
        }

        [HttpGet("execute")]
        [ProducesResponseType(typeof(ToolOutput), 200)]
        [ProducesResponseType(typeof(ToolOutput), 400)]
        [ProducesResponseType(typeof(ToolOutput), 404)]
        public async Task<IActionResult> ExecuteTool(
            [FromQuery] string toolIdentifier,
            [FromQuery] string target,
            [FromQuery] Dictionary<string, string> options)
        {
            if (string.IsNullOrWhiteSpace(toolIdentifier))
                return BadRequest(new ToolOutput { Success = false, ToolName = "Unknown", ErrorMessage = "toolIdentifier muss angegeben werden." });
            if (string.IsNullOrWhiteSpace(target))
                return BadRequest(new ToolOutput { Success = false, ToolName = toolIdentifier, ErrorMessage = "target muss angegeben werden." });

            var tool = _networkTools.FirstOrDefault(t => t.Identifier.Equals(toolIdentifier, System.StringComparison.OrdinalIgnoreCase));
            if (tool == null)
                return NotFound(new ToolOutput { Success = false, ToolName = toolIdentifier, ErrorMessage = $"Tool '{toolIdentifier}' nicht gefunden." });

            var result = await tool.ExecuteAsync(target, options ?? new Dictionary<string, string>());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}