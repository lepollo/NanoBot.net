namespace Nanobot.Core.Config;

public class AppConfig
{
    public Dictionary<string, ProviderSettings> Providers { get; set; } = new();
    public AgentSettings Agents { get; set; } = new();
    public WebSearchSettings WebSearch { get; set; } = new();
    public Dictionary<string, ChannelSettings> Channels { get; set; } = new();
}

public class ProviderSettings
{
    public string? ApiKey { get; set; }
    public string? ApiBase { get; set; }
}

public class AgentSettings
{
    public DefaultAgentSettings Defaults { get; set; } = new();
}

public class DefaultAgentSettings
{
    public string? Model { get; set; }
}

public class WebSearchSettings
{
    public string? ApiKey { get; set; }
}

public class ChannelSettings
{
    public bool Enabled { get; set; }
    public string? Token { get; set; }
    public List<string> AllowFrom { get; set; } = new();
}