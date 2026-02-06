using System.Text.Json.Nodes;

namespace Nanobot.Core.Tools.Builtin;

public class WeatherTool : ITool
{
    private static readonly HttpClient _httpClient = new();

    public string Name => "get_weather";
    public string Description => "Get current weather and forecasts for a location (no API key required).";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "location": { "type": "string", "description": "The city name, airport code, or IP address." },
            "format": { "type": "string", "description": "Output format. Use '3' for one-liner, '0' for current only, 'T' for no colors.", "default": "3" }
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
