using System.CommandLine;
using Nanobot.Core.Config;
using Nanobot.Core.Providers;
using Nanobot.Core.Tools;
using Nanobot.Core.Tools.Builtin;
using Nanobot.Core.Memory;
using Nanobot.Core.Agent;
using Nanobot.Core.Cron;
using Nanobot.Core.Models;
using Nanobot.Core.Channels;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Nanobot .NET CLI (Default: Chat Mode)");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var nanoDir = Path.Combine(home, ".nanobot");
        var configFile = Path.Combine(nanoDir, "config.json");
        var workspace = Path.Combine(nanoDir, "workspace");
        var cronFile = Path.Combine(nanoDir, "cron.json");

        // --- Helper: Setup Agent with Env Priority ---
        async Task<Agent> SetupAgent() {
            AppConfig config = File.Exists(configFile) ? ConfigLoader.Load(configFile) : new AppConfig();
            
            // Priority 1: Environment Variables
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
            string? baseUrl = Environment.GetEnvironmentVariable("OPENAI_API_BASE");
            string model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o";

            // Priority 2: Config File (if env is missing/default)
            if (string.IsNullOrEmpty(apiKey) && config.Providers.TryGetValue("openai", out var openAiConfig)) {
                apiKey = openAiConfig.ApiKey ?? "";
                baseUrl ??= openAiConfig.ApiBase;
            }
            if (model == "gpt-4o" && config.Agents.Defaults.Model != null) {
                model = config.Agents.Defaults.Model;
            }

            if (string.IsNullOrEmpty(apiKey)) {
                throw new Exception("Error: No API Key found. Please set 'OPENAI_API_KEY' environment variable.");
            }

            var provider = new OpenAIProvider(apiKey, baseUrl, model);
            var registry = new ToolRegistry();
            registry.Register(new ReadFileTool());
            registry.Register(new WriteFileTool());
            registry.Register(new EditFileTool());
            registry.Register(new ListDirTool());
            registry.Register(new ShellTool());
            registry.Register(new WebSearchTool(config.WebSearch?.ApiKey ?? Environment.GetEnvironmentVariable("BRAVE_API_KEY") ?? ""));
            registry.Register(new WebFetchTool());
            registry.Register(new WeatherTool());
            registry.Register(new StockTool());
            registry.Register(new SummarizeTool(provider));
            
            string? githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? (config.Providers.TryGetValue("github", out var gh) ? gh.ApiKey : null);
            registry.Register(new GitHubTool(githubToken));

            var memory = new FileMemoryStore(workspace);
            return new Agent(provider, registry, memory);
        }

        // --- Default Command Handler (Root) ---
        rootCommand.SetHandler(async () => {
            try {
                var agent = await SetupAgent();
                await RunChatLoop(agent);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        });

        // --- Command: Onboard ---
        var onboardCommand = new Command("onboard", "Initialize configuration and workspace");
        onboardCommand.SetHandler(() => {
            if (!Directory.Exists(nanoDir)) Directory.CreateDirectory(nanoDir);
            if (!File.Exists(configFile)) {
                File.WriteAllText(configFile, "{\n  \"providers\": {\n    \"openai\": { \"apiKey\": \"\" }\n  }\n}");
                Console.WriteLine($"Created default config at {configFile}");
            }
            if (!Directory.Exists(workspace)) Directory.CreateDirectory(workspace);
            Console.WriteLine("Onboarding complete.");
        });
        rootCommand.AddCommand(onboardCommand);

        // --- Command: Gateway ---
        var gatewayCommand = new Command("gateway", "Start the Telegram bot gateway");
        gatewayCommand.SetHandler(async () => {
            var agent = await SetupAgent();
            var config = ConfigLoader.Load(configFile);
            
            var cronService = new CronService(cronFile, async (job) => await agent.RunAsync(job.Payload.Message));
            await cronService.StartAsync();

            if (config.Channels.TryGetValue("telegram", out var tg) && tg.Enabled && !string.IsNullOrEmpty(tg.Token)) {
                var tgChannel = new TelegramChannel(tg.Token, async (inbound) => {
                    var response = await agent.RunAsync(inbound.Content);
                    return new OutboundMessage(inbound.Channel, inbound.ChatId, response);
                });
                await tgChannel.StartAsync();
                Console.WriteLine("Gateway running (Telegram)...");
                await Task.Delay(-1);
            } else {
                Console.WriteLine("Telegram configuration missing. Gateway cannot start.");
            }
        });
        rootCommand.AddCommand(gatewayCommand);

        // --- Command: Chat (Explicit) ---
        var chatCommand = new Command("chat", "Start interactive chat (Default)");
        chatCommand.SetHandler(async () => {
            var agent = await SetupAgent();
            await RunChatLoop(agent);
        });
        rootCommand.AddCommand(chatCommand);

        // --- Command: Agent (Single Message) ---
        var msgOption = new Option<string>("--message", "Message to send") { IsRequired = true };
        msgOption.AddAlias("-m");
        var agentCommand = new Command("agent", "Send a single message to the agent");
        agentCommand.AddOption(msgOption);
        agentCommand.SetHandler(async (message) => {
            var agent = await SetupAgent();
            var response = await agent.RunAsync(message);
            Console.WriteLine(response);
        }, msgOption);
        rootCommand.AddCommand(agentCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunChatLoop(Agent agent) {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════╗");
        Console.WriteLine("║        Nanobot.NET Interactive Chat          ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝");
        Console.ResetColor();

        while (true) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nYou: ");
            Console.ResetColor();
            
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.ToLower() is "exit" or "quit") break;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Thinking...");
            Console.ResetColor();

            try {
                var response = await agent.RunAsync(input);
                Console.Write("\r" + new string(' ', 15) + "\r"); // Clear line
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write("Agent: ");
                Console.ResetColor();
                Console.WriteLine(response);
            } catch (Exception ex) {
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }
    }
}
