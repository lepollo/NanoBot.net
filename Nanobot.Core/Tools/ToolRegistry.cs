using System.Text.Json.Nodes;

namespace Nanobot.Core.Tools;

public class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new();

    public void Register(ITool tool)
    {
        _tools[tool.Name] = tool;
    }

    public void Unregister(string name)
    {
        _tools.Remove(name);
    }

    public ITool? Get(string name)
    {
        return _tools.GetValueOrDefault(name);
    }
    
    public bool Has(string name) => _tools.ContainsKey(name);

    public List<JsonNode> GetDefinitions()
    {
        // Convert to OpenAI schema format
        var list = new List<JsonNode>();
        foreach (var tool in _tools.Values)
        {
            var node = new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = tool.Parameters?.DeepClone() ?? new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() }
                }
            };
            list.Add(node);
        }
        return list;
    }
    
    public async Task<string> ExecuteAsync(string name, JsonNode? arguments)
    {
        if (!_tools.TryGetValue(name, out var tool))
        {
            return $"Error: Tool '{name}' not found";
        }
        
        try
        {
            return await tool.ExecuteAsync(arguments);
        }
        catch (Exception ex)
        {
            return $"Error executing {name}: {ex.Message}";
        }
    }
}
