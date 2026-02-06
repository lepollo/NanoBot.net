using System.Text.Json.Nodes;

namespace Nanobot.Core.Tools.Builtin;

public class WriteFileTool : ITool
{
    public string Name => "write_file";
    public string Description => "Write content to a file. Overwrites existing file.";
    
    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "path": {
                "type": "string",
                "description": "The path to the file to write."
            },
            "content": {
                "type": "string",
                "description": "The content to write."
            }
        },
        "required": ["path", "content"]
    }
    """)!;

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var path = arguments?["path"]?.ToString();
        var content = arguments?["content"]?.ToString();

        if (string.IsNullOrEmpty(path) || content == null)
        {
            return "Error: path and content are required";
        }

        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(path, content);
            return $"Successfully wrote to {path}";
        }
        catch (Exception ex)
        {
            return $"Error writing file: {ex.Message}";
        }
    }
}
