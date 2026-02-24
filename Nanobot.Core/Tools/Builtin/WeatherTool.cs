using System.Text.Json.Nodes;

namespace Nanobot.Core.Tools.Builtin;

public class WeatherTool : ITool
{
    private static readonly HttpClient _httpClient = new();

    public string Name => "get_weather";
    public string Description => "获取指定城市或地点的当前天气预报（无需 API Key）。";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "location": { "type": "string", "description": "城市名称、机场代码或 IP 地址。" },
            "format": { "type": "string", "description": "输出格式。'3' 表示单行摘要，'0' 仅当前天气，'T' 不带颜色。", "default": "3" }
        },
        "required": ["location"]
    }
    """)!;

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var location = arguments?["location"]?.ToString();
        if (string.IsNullOrEmpty(location)) return "Error: location is required";

        var format = arguments?["format"]?.ToString() ?? "3";
        // URL encode the location
        var encodedLocation = Uri.EscapeDataString(location);

        try
        {
            var url = $"https://wttr.in/{encodedLocation}?{format}";
            var response = await _httpClient.GetStringAsync(url);
            return response.Trim();
        }
        catch (Exception ex)
        {
            return $"Error fetching weather: {ex.Message}";
        }
    }
}
