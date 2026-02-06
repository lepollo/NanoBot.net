using Nanobot.Core.Tools;
using Nanobot.Core.Tools.Builtin;
using System.Text.Json.Nodes;

namespace Nanobot.Tests;

public class ToolTests
{
    [Fact]
    public void ToolRegistry_CanRegisterAndGet()
    {
        var registry = new ToolRegistry();
        var tool = new ReadFileTool();
        
        registry.Register(tool);
        
        Assert.True(registry.Has(tool.Name));
        Assert.Equal(tool, registry.Get(tool.Name));
    }

    [Fact]
    public void ToolRegistry_GetDefinitions_ReturnsValidOpenAISchema()
    {
        var registry = new ToolRegistry();
        registry.Register(new ReadFileTool());
        
        var definitions = registry.GetDefinitions();
        
        Assert.Single(definitions);
        Assert.Equal("function", definitions[0]["type"]?.ToString());
        Assert.Equal("read_file", definitions[0]["function"]?["name"]?.ToString());
    }

    [Fact]
    public async Task ShellTool_CanExecuteEcho()
    {
        var tool = new ShellTool();
        var args = JsonNode.Parse("{\"command\": \"echo hello\"}");
        
        var result = await tool.ExecuteAsync(args);
        
        Assert.Contains("hello", result.ToLower());
    }
}