using System.Text.Json.Nodes;

namespace Nanobot.Core.Models;

/// <summary>
/// A tool call request from the LLM.
/// </summary>
public record ToolCallRequest(
    string Id,
    string Name,
    JsonNode? Arguments
);
