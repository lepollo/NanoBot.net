using System.Text.Json.Nodes;

namespace Nanobot.Core.Tools;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    JsonNode Parameters { get; } // The JSON Schema for parameters

    Task<string> ExecuteAsync(JsonNode? arguments);
}
