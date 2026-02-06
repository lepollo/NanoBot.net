using System.Text.Json.Nodes;

namespace Nanobot.Core.Tools.Builtin;

public class ReadFileTool : ITool
{
    public string Name => "read_file";
    public string Description => "Read the content of a file from the filesystem.";
    
    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "path": {
                "type": "string",
                "description": "The path to the file to read."
            }
        },
        "required": ["path"]
    }
    """)!;

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var path = arguments?["path"]?.ToString();
        if (string.IsNullOrEmpty(path))
        {
            return "Error: path is required";
        }

        try
        {
            if (!File.Exists(path))
            {
                return $"Error: File '{path}' does not exist.";
            }
            return await File.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }
}
