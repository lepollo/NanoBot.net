using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace Nanobot.Core.Tools.Builtin;

public class ShellTool : ITool
{
    public string Name => "run_shell";
    public string Description => "Execute a shell command.";
    
    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "command": {
                "type": "string",
                "description": "The command to execute."
            }
        },
        "required": ["command"]
    }
    """)!;

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var command = arguments?["command"]?.ToString();
        if (string.IsNullOrEmpty(command))
        {
            return "Error: command is required";
        }

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                processStartInfo.FileName = "cmd.exe";
                processStartInfo.Arguments = $"/c {command}";
            }
            else
            {
                processStartInfo.FileName = "/bin/sh";
                processStartInfo.Arguments = $"-c \"{command}\"";
            }

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;
            
            var result = output;
            if (!string.IsNullOrEmpty(error))
            {
                result += $"\nSTDERR:\n{error}";
            }
            
            return string.IsNullOrWhiteSpace(result) ? "(No output)" : result;
        }
        catch (Exception ex)
        {
            return $"Error executing command: {ex.Message}";
        }
    }
}