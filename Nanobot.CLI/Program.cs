using System.CommandLine;
using Nanobot.Core.Config;
using Nanobot.Core.Providers;
using Nanobot.Core.Tools;
using Nanobot.Core.Tools.Builtin;
using Nanobot.Core.Memory;
using Nanobot.Core.Agent;
using Nanobot.Core.Cron;
using Nanobot.Core.Models;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Nanobot .NET CLI");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var nanoDir = Path.Combine(home, ".nanobot");
        var configFile = Path.Combine(nanoDir, "config.json");
        var workspace = Path.Combine(nanoDir, "workspace");
        var cronFile = Path.Combine(nanoDir, "cron.json");

        // Onboard Command
        var onboardCommand = new Command("onboard", "Initialize configuration and workspace");
        onboardCommand.SetHandler(() =>
        {
            if (!Directory.Exists(nanoDir)) Directory.CreateDirectory(nanoDir);

            if (!File.Exists(configFile))
            {
                var defaultConfig = """
                {
                  "providers": {
                    "openai": {
                      "apiKey": ""
                    }
                  },
                  "agents": {
                    "defaults": {
                      "model": "gpt-4o"
                    }
                  }
                }
                """;
                File.WriteAllText(configFile, defaultConfig);
                Console.WriteLine($"Created config at {configFile}");
                Console.WriteLine("Please edit the config file to add your API Key.");
            }
            else
            {
                Console.WriteLine("Config already exists.");
            }
            
            // Create workspace
            if (!Directory.Exists(workspace)) Directory.CreateDirectory(workspace);
            Console.WriteLine($"Workspace ready at {workspace}");
        });
        rootCommand.AddCommand(onboardCommand);

        // Cron Command
        var cronCommand = new Command("cron", "Manage scheduled tasks");
        
        var listJobsCommand = new Command("list", "List all scheduled jobs");
        listJobsCommand.SetHandler(() => {
            var cronService = new CronService(cronFile);
            var jobs = cronService.ListJobs(true);
            if (jobs.Count == 0) {
                Console.WriteLine("No jobs found.");
                return;
            }
            Console.WriteLine($"{"ID",-10} {"Name",-20} {"Schedule",-20} {"Next Run",-25} {"Status",-10}");
            Console.WriteLine(new string('-', 85));
            foreach (var job in jobs) {
                var nextRun = job.State.NextRunAtMs.HasValue 
                    ? DateTimeOffset.FromUnixTimeMilliseconds(job.State.NextRunAtMs.Value).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                    : "N/A";
                var schedule = job.Schedule.Kind == "cron" ? job.Schedule.Expr : (job.Schedule.Kind == "every" ? $"every {job.Schedule.EveryMs}ms" : "once");
                Console.WriteLine($"{job.Id,-10} {job.Name,-20} {schedule,-20} {nextRun,-25} {(job.Enabled ? "Enabled" : "Disabled"),-10}");
            }
        });
        cronCommand.AddCommand(listJobsCommand);

        var addJobCommand = new Command("add", "Add a new scheduled job");
        var nameOption = new Option<string>("--name", "Name of the job") { IsRequired = true };
        var msgOption = new Option<string>("--message", "Message to send to the agent") { IsRequired = true };
        var cronExprOption = new Option<string>("--cron", "Cron expression (e.g., '0 9 * * *')");
        var everyOption = new Option<long?>("--every", "Run every N milliseconds");
        
        addJobCommand.AddOption(nameOption);
        addJobCommand.AddOption(msgOption);
        addJobCommand.AddOption(cronExprOption);
        addJobCommand.AddOption(everyOption);

        addJobCommand.SetHandler((name, message, cronExpr, every) => {
            var cronService = new CronService(cronFile);
            CronSchedule schedule;
            if (!string.IsNullOrEmpty(cronExpr)) {
                schedule = new CronSchedule { Kind = "cron", Expr = cronExpr };
            } else if (every.HasValue) {
                schedule = new CronSchedule { Kind = "every", EveryMs = every.Value };
            } else {
                Console.WriteLine("Error: Either --cron or --every must be specified.");
                return;
            }
            var job = cronService.AddJob(name, schedule, message);
            Console.WriteLine($"Added job '{job.Name}' with ID {job.Id}");
        }, nameOption, msgOption, cronExprOption, everyOption);
        cronCommand.AddCommand(addJobCommand);

        var removeJobCommand = new Command("remove", "Remove a job by ID");
        var idArgument = new Argument<string>("id", "Job ID");
        removeJobCommand.AddArgument(idArgument);
        removeJobCommand.SetHandler((id) => {
            var cronService = new CronService(cronFile);
            if (cronService.RemoveJob(id)) {
                Console.WriteLine($"Removed job {id}");
            } else {
                Console.WriteLine($"Job {id} not found.");
            }
        }, idArgument);
        cronCommand.AddCommand(removeJobCommand);

        rootCommand.AddCommand(cronCommand);

        // Gateway Command
        var gatewayCommand = new Command("gateway", "Start the gateway (Telegram bot)");
        gatewayCommand.SetHandler(async () => {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var nanoDir = Path.Combine(home, ".nanobot");
            var configFile = Path.Combine(nanoDir, "config.json");
            var workspace = Path.Combine(nanoDir, "workspace");
            var cronFile = Path.Combine(nanoDir, "cron.json");

            if (!File.Exists(configFile)) {
                Console.WriteLine("Please run 'nanobot onboard' first.");
                return;
            }

            var config = ConfigLoader.Load(configFile);
            
            // LLM Provider setup
            string apiKey = "";
            string? baseUrl = null;
            string model = "gpt-4o";

            if (config.Providers.TryGetValue("openai", out var openAiConfig)) {
                apiKey = openAiConfig.ApiKey ?? "";
                baseUrl = openAiConfig.ApiBase;
            } else if (config.Providers.TryGetValue("openrouter", out var orConfig)) {
                apiKey = orConfig.ApiKey ?? "";
                baseUrl = orConfig.ApiBase ?? "https://openrouter.ai/api/v1";
            }
            
            // Fallback to environment variables
            if (string.IsNullOrEmpty(apiKey)) apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
            if (string.IsNullOrEmpty(baseUrl)) baseUrl = Environment.GetEnvironmentVariable("OPENAI_API_BASE");
            
            if (config.Agents.Defaults.Model != null) {
                model = config.Agents.Defaults.Model;
            } else {
                model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o";
            }

            if (string.IsNullOrEmpty(apiKey)) {
                Console.WriteLine("Error: No API Key found.");
                return;
            }

            var provider = new OpenAIProvider(apiKey, baseUrl, model);
            var registry = new ToolRegistry();
            registry.Register(new ReadFileTool());
            registry.Register(new WriteFileTool());
            registry.Register(new ShellTool());
            
            string braveKey = config.WebSearch?.ApiKey ?? Environment.GetEnvironmentVariable("BRAVE_API_KEY") ?? "";
            registry.Register(new WebSearchTool(braveKey));
            registry.Register(new WebFetchTool());
            var memory = new FileMemoryStore(workspace);
            var agent = new Agent(provider, registry, memory);

            // Services
            var cronService = new CronService(cronFile, async (job) => {
                Console.WriteLine($"Cron executing: {job.Name}");
                return await agent.RunAsync(job.Payload.Message);
            });
            await cronService.StartAsync();

            var heartbeatService = new Nanobot.Core.Heartbeat.HeartbeatService(workspace, async (prompt) => {
                Console.WriteLine("Heartbeat tick");
                return await agent.RunAsync(prompt);
            });
            await heartbeatService.StartAsync();

            // Telegram Channel
            if (config.Channels.TryGetValue("telegram", out var tgConfig) && tgConfig.Enabled && !string.IsNullOrEmpty(tgConfig.Token)) {
                var tgChannel = new Nanobot.Core.Channels.TelegramChannel(tgConfig.Token, async (inbound) => {
                    Console.WriteLine($"Received from Telegram ({inbound.SenderId}): {inbound.Content}");
                    var response = await agent.RunAsync(inbound.Content);
                    return new OutboundMessage(inbound.Channel, inbound.ChatId, response);
                });
                await tgChannel.StartAsync();
            } else {
                Console.WriteLine("Telegram channel disabled or token missing.");
            }

            Console.WriteLine("Gateway running. Press Ctrl+C to stop.");
            await Task.Delay(-1); // Keep running
        });
        rootCommand.AddCommand(gatewayCommand);

        // Agent Command
        var messageOption = new Option<string>(
            "--message", 
            "The message to send to the agent"
        );
        messageOption.AddAlias("-m");

        var agentCommand = new Command("agent", "Chat with the agent")
        {
            messageOption
        };

        agentCommand.SetHandler(async (message) =>
        {
            // Setup
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var nanoDir = Path.Combine(home, ".nanobot");
            var configFile = Path.Combine(nanoDir, "config.json");
            var workspace = Path.Combine(nanoDir, "workspace");

            if (!File.Exists(configFile))
            {
                Console.WriteLine("Please run 'nanobot onboard' first.");
                return;
            }

            var config = ConfigLoader.Load(configFile);
            
            // Provider
            // Simplified selection logic
            string apiKey = "";
            string? baseUrl = null;
            string model = "gpt-4o";

            if (config.Providers.TryGetValue("openai", out var openAiConfig))
            {
                apiKey = openAiConfig.ApiKey ?? "";
                baseUrl = openAiConfig.ApiBase;
            }
            else if (config.Providers.TryGetValue("openrouter", out var orConfig))
            {
                apiKey = orConfig.ApiKey ?? "";
                baseUrl = orConfig.ApiBase ?? "https://openrouter.ai/api/v1";
            }
            
            // Fallback to environment variables
            if (string.IsNullOrEmpty(apiKey)) apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
            if (string.IsNullOrEmpty(baseUrl)) baseUrl = Environment.GetEnvironmentVariable("OPENAI_API_BASE");

            if (config.Agents.Defaults.Model != null)
            {
                model = config.Agents.Defaults.Model;
            }
            else
            {
                model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o";
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Error: No API Key found in config or environment. Please set it in ~/.nanobot/config.json");
                return;
            }

            var provider = new OpenAIProvider(apiKey, baseUrl, model);

            // Tools
            var registry = new ToolRegistry();
            registry.Register(new ReadFileTool());
            registry.Register(new WriteFileTool());
            registry.Register(new ShellTool());
            
            string braveKey = config.WebSearch?.ApiKey ?? Environment.GetEnvironmentVariable("BRAVE_API_KEY") ?? "";
            registry.Register(new WebSearchTool(braveKey));
            registry.Register(new WebFetchTool());
            registry.Register(new WeatherTool());
            registry.Register(new SummarizeTool(provider));
            
            // Try to find github token in config (custom addition for demo)
            string? githubToken = config.Providers.TryGetValue("github", out var ghConfig) ? ghConfig.ApiKey : null;
            githubToken ??= Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            registry.Register(new GitHubTool(githubToken));

            // Memory
            var memory = new FileMemoryStore(workspace);

            // Agent
            var agent = new Agent(provider, registry, memory);

            if (!string.IsNullOrEmpty(message))
            {
                // Single shot
                Console.WriteLine($"User: {message}");
                try 
                {
                    var response = await agent.RunAsync(message);
                    Console.WriteLine($"Agent: {response}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                // Interactive
                Console.WriteLine("Entering interactive mode. Type 'exit' to quit.");
                while (true)
                {
                    Console.Write("> ");
                    var input = Console.ReadLine();
                    if (string.IsNullOrEmpty(input)) continue;
                    if (input.ToLower() == "exit") break;

                    try
                    {
                        var response = await agent.RunAsync(input);
                        Console.WriteLine($"Agent: {response}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }

        }, messageOption);
        rootCommand.AddCommand(agentCommand);

        return await rootCommand.InvokeAsync(args);
    }
}