using System.Text.Json.Nodes;

namespace Nanobot.Core.Models;

public record Message
{
    public string Role { get; init; }
    public string? Content { get; init; }
    public List<ToolCallRequest>? ToolCalls { get; init; }
    public string? ToolCallId { get; init; } // For tool response messages

    public Message(string role, string? content)
    {
        Role = role;
        Content = content;
    }
}