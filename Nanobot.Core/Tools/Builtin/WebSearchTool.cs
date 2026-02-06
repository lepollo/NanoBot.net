using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Nanobot.Core.Tools.Builtin;

public class WebSearchTool : ITool
{
    private readonly string _apiKey;
    private readonly int _maxResults;
    private static readonly HttpClient _httpClient = new();

    public string Name => "web_search";
    public string Description => "Search the web. Returns titles, URLs, and snippets.";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "query": { "type": "string", "description": "Search query" },
            "count": { "type": "integer", "description": "Results (1-10)", "minimum": 1, "maximum": 10 }
        },
        "required": ["query"]
    }
    """)!;

    public WebSearchTool(string apiKey, int maxResults = 5)
    {
        _apiKey = apiKey;
        _maxResults = maxResults;
    }

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "Error: BRAVE_API_KEY not configured";
        }

        var query = arguments?["query"]?.ToString();
        if (string.IsNullOrEmpty(query))
        {
            return "Error: query is required";
        }

        var countNode = arguments?["count"];
        int count = countNode != null ? Math.Clamp(countNode.GetValue<int>(), 1, 10) : _maxResults;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.search.brave.com/res/v1/web/search?q={Uri.EscapeDataString(query)}&count={count}");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("X-Subscription-Token", _apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<JsonNode>();
            var results = data?["web"]?["results"]?.AsArray();

            if (results == null || results.Count == 0)
            {
                return $"No results for: {query}";
            }

            var lines = new List<string> { $"Results for: {query}\n" };
            for (int i = 0; i < Math.Min(results.Count, count); i++)
            {
                var item = results[i];
                var title = item?["title"]?.ToString() ?? "";
                var url = item?["url"]?.ToString() ?? "";
                var desc = item?["description"]?.ToString() ?? "";

                lines.Add($"{i + 1}. {title}\n   {url}");
                if (!string.IsNullOrEmpty(desc))
                {
                    lines.Add($"   {desc}");
                }
            }

            return string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}