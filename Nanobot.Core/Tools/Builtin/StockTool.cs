using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Nanobot.Core.Tools.Builtin;

public class StockTool : ITool
{
    private static readonly HttpClient _httpClient = new();

    public string Name => "get_stock_price";
    public string Description => "获取股票实时价格和涨跌幅（无需 API Key）。支持美股（如 AAPL）、港股和 A 股（如 600519:SHA）。";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "symbol": { "type": "string", "description": "股票代码，例如 'AAPL' 或 'TSLA'。对于 A 股请包含市场代码，如 '600519:SHA'。" }
        },
        "required": ["symbol"]
    }
    """)!;

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var symbol = arguments?["symbol"]?.ToString();
        if (string.IsNullOrEmpty(symbol)) return "错误：必须提供股票代码 (symbol)";

        try
        {
            // 设置 User-Agent 以模拟浏览器，防止被拦截
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            }

            var url = $"https://www.google.com/finance/quote/{symbol}";
            var html = await _httpClient.GetStringAsync(url);

            // Google Finance 的价格通常在 class="YMlKec fxKbKc" 的 div 中
            var priceMatch = Regex.Match(html, @"class=""YMlKec fxKbKc"">([^<]+)</div>");
            // 涨跌值和涨跌幅
            var changeMatch = Regex.Match(html, @"class=""[^""]*P63p9c[^""]*"">([^<]+)</div>");
            var percentMatch = Regex.Match(html, @"class=""[^""]*W67mY[^""]*"">([^<]+)</div>");

            if (!priceMatch.Success) 
            {
                return $"未能获取股票 {symbol} 的实时数据。提示：请尝试包含交易所代码，例如 '600519:SHA' 或 '700:HKG'。";
            }

            var price = priceMatch.Groups[1].Value;
            var change = changeMatch.Success ? changeMatch.Groups[1].Value : "未知";
            var percent = percentMatch.Success ? percentMatch.Groups[1].Value : "未知";

            return $@"📊 股票: {symbol.ToUpper()}
💰 当前价格: {price}
📈 今日涨跌: {change} ({percent})";
        }
        catch (Exception ex)
        {
            return $"获取数据时发生异常: {ex.Message}";
        }
    }
}
