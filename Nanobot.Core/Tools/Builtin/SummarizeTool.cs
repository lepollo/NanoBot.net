using System.Text.Json.Nodes;
using Nanobot.Core.Providers;
using Nanobot.Core.Models;

namespace Nanobot.Core.Tools.Builtin;

public class SummarizeTool : ITool
{
    private readonly ILLMProvider _provider;
    private readonly WebFetchTool _fetcher;

    public string Name => "summarize";
    public string Description => "Summarize text or content from a URL.";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "text": { "type": "string", "description": "The text to summarize." },
            "url": { "type": "string", "description": "The URL to fetch and summarize." },
            "length": { "type": "string", "enum": ["short", "medium", "long"], "default": "medium" }
        }
    }
    """)!;

    public SummarizeTool(ILLMProvider provider)
    {
        _provider = provider;
        _fetcher = new WebFetchTool();
    }

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        string? contentToSummarize = arguments?["text"]?.ToString();
        var url = arguments?["url"]?.ToString();
        var length = arguments?["length"]?.ToString() ?? "medium";

        if (string.IsNullOrEmpty(contentToSummarize) && !string.IsNullOrEmpty(url))
        {
            var fetchResult = await _fetcher.ExecuteAsync(JsonNode.Parse($"{{\"url\": \"{url}\"}}"));
            if (!string.IsNullOrEmpty(fetchResult))
            {
                try
                {
                    var json = JsonNode.Parse(fetchResult);
                    contentToSummarize = json?["text"]?.ToString() ?? fetchResult;
                }
                catch
                {
                    contentToSummarize = fetchResult;
                }
            }
        }

        if (string.IsNullOrEmpty(contentToSummarize))
        {
            return "Error: Either 'text' or 'url' must be provided.";
        }

        var prompt = $"Please provide a {length} summary of the following text:\n\n{contentToSummarize}";
        
        var response = await _provider.ChatAsync(new List<Message> 
        { 
            new Message("system", "You are a professional summarizer."),
            new Message("user", prompt)
        });

        return response.Content ?? "Error: Failed to generate summary.";
    }
}
