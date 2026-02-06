using System.Text.Json.Nodes;
using Octokit;

namespace Nanobot.Core.Tools.Builtin;

public class GitHubTool : ITool
{
    private readonly GitHubClient _client;

    public string Name => "github";
    public string Description => "Interact with GitHub (search repos, list issues).";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "action": { "type": "string", "enum": ["search_repos", "list_issues"], "description": "Action to perform" },
            "query": { "type": "string", "description": "Search query or repo name (owner/repo)" }
        },
        "required": ["action", "query"]
    }
    """)!;

    public GitHubTool(string? token = null)
    {
        _client = new GitHubClient(new ProductHeaderValue("nanobot-dotnet"));
        if (!string.IsNullOrEmpty(token))
        {
            _client.Credentials = new Credentials(token);
        }
    }

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var action = arguments?["action"]?.ToString();
        var query = arguments?["query"]?.ToString();

        if (string.IsNullOrEmpty(action) || string.IsNullOrEmpty(query))
        {
            return "Error: action and query are required";
        }

        try
        {
            if (action == "search_repos")
            {
                var request = new SearchRepositoriesRequest(query);
                var result = await _client.Search.SearchRepo(request);
                var items = result.Items.Take(5).Select(r => $"{r.FullName}: {r.Description} ({r.HtmlUrl})");
                return $"Top 5 repos for '{query}':\n" + string.Join("\n", items);
            }
            else if (action == "list_issues")
            {
                var parts = query.Split('/');
                if (parts.Length != 2) return "Error: query must be 'owner/repo' for list_issues";
                
                var issues = await _client.Issue.GetAllForRepository(parts[0], parts[1]);
                var items = issues.Take(5).Select(i => $"#{i.Number}: {i.Title} ({i.State})");
                return $"Top 5 issues for {query}:\n" + string.Join("\n", items);
            }
            
            return $"Error: Unknown action '{action}'";
        }
        catch (Exception ex)
        {
            return $"GitHub Error: {ex.Message}";
        }
    }
}