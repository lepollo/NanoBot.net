namespace Nanobot.Core.Models;

/// <summary>
/// Response from an LLM provider.
/// </summary>
public record LLMResponse
{
    public string? Content { get; init; }
    public List<ToolCallRequest> ToolCalls { get; init; } = new();
    public string FinishReason { get; init; } = "stop";
    public Dictionary<string, int> Usage { get; init; } = new();

    public bool HasToolCalls => ToolCalls.Count > 0;

    public LLMResponse(string? content)
    {
        Content = content;
    }

    public LLMResponse() { }
}
