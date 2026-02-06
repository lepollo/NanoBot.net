using System.Text.Json.Nodes;
using Nanobot.Core.Models;

namespace Nanobot.Core.Providers;

public interface ILLMProvider
{
    Task<LLMResponse> ChatAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7
    );

    string GetDefaultModel();
}
