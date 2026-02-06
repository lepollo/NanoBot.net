using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Net;

namespace Nanobot.Core.Tools.Builtin;

public class WebFetchTool : ITool
{
    private const string UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_7_2) AppleWebKit/537.36";
    private readonly int _maxChars;
    private static readonly HttpClient _httpClient = new();

    public string Name => "web_fetch";
    public string Description => "Fetch URL and extract readable content (HTML → markdown/text).";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "url": { "type": "string", "description": "URL to fetch" },
            "extractMode": { "type": "string", "enum": ["markdown", "text"], "default": "markdown" },
            "maxChars": { "type": "integer", "minimum": 100 }
        },
        "required": ["url"]
    }
    """)!;

    public WebFetchTool(int maxChars = 50000)
    {
        _maxChars = maxChars;
    }

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var url = arguments?["url"]?.ToString();
        if (string.IsNullOrEmpty(url)) return "Error: url is required";

        var extractMode = arguments?["extractMode"]?.ToString() ?? "markdown";
        var maxCharsNode = arguments?["maxChars"];
        int maxChars = maxCharsNode != null ? maxCharsNode.GetValue<int>() : _maxChars;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            var content = await response.Content.ReadAsStringAsync();

            string text;
            string extractor;

            if (contentType.Contains("application/json"))
            {
                text = content;
                extractor = "json";
            }
            else if (contentType.Contains("text/html") || content.TrimStart().ToLower().StartsWith("<!doctype") || content.TrimStart().ToLower().StartsWith("<html"))
            {
                // Basic HTML to Markdown/Text conversion (simplified version of Python's logic)
                text = extractMode == "markdown" ? ToMarkdown(content) : StripTags(content);
                extractor = "simple-html";
            }
            else
            {
                text = content;
                extractor = "raw";
            }

            bool truncated = text.Length > maxChars;
            if (truncated)
            {
                text = text.Substring(0, maxChars);
            }

            var result = new
            {
                url = url,
                finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url,
                status = (int)response.StatusCode,
                extractor = extractor,
                truncated = truncated,
                length = text.Length,
                text = text
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, url = url });
        }
    }

    private string StripTags(string html)
    {
        var text = Regex.Replace(html, @"<script[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<style[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<[^>]+>", "");
        return WebUtility.HtmlDecode(text).Trim();
    }

    private string ToMarkdown(string html)
    {
        // Simple regex-based markdown conversion
        var text = Regex.Replace(html, @"<a\s+[^>]*href=[""']([^""']+)[""'][^>]*>([\s\S]*?)</a>", m => $"[{StripTags(m.Groups[2].Value)}]({m.Groups[1].Value})", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<h([1-6])[^>]*>([\s\S]*?)</h\1>", m => $"\n{new string('#', int.Parse(m.Groups[1].Value))} {StripTags(m.Groups[2].Value)}\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<li[^>]*>([\s\S]*?)</li>", m => $"\n- {StripTags(m.Groups[1].Value)}", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</(p|div|section|article)>", "\n\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<(br|hr)\s*/?>", "\n", RegexOptions.IgnoreCase);
        
        text = StripTags(text);
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }
}