using Microsoft.Extensions.Configuration;

namespace Nanobot.Core.Config;

public static class ConfigLoader
{
    public static AppConfig Load(string configPath)
    {
        // Ensure path is absolute or handle relative paths correctly
        if (!Path.IsPathRooted(configPath))
        {
            configPath = Path.Combine(Directory.GetCurrentDirectory(), configPath);
        }

        var builder = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: true, reloadOnChange: true);

        var configuration = builder.Build();
        var config = new AppConfig();
        configuration.Bind(config);
        return config;
    }
}
