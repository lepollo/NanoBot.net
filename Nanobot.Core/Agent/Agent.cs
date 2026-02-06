using Nanobot.Core.Providers;
using Nanobot.Core.Tools;
using Nanobot.Core.Models;
using Nanobot.Core.Memory;
using System.Text.Json.Nodes;

namespace Nanobot.Core.Agent;

public class Agent
{
    private readonly ILLMProvider _provider;
    private readonly ToolRegistry _tools;
    private readonly IMemory _memory;
    private readonly List<Message> _history = new();
    private const int MaxIterations = 20;

    public Agent(ILLMProvider provider, ToolRegistry tools, IMemory memory)
    {
        _provider = provider;
        _tools = tools;
        _memory = memory;
    }

    public async Task<string> RunAsync(string input)
    {
        // 1. Build Context
        var messages = new List<Message>();
        
        // Add System Prompt with Memory
        var context = _memory.GetContext();
        var systemPrompt = "You are nanobot, a helpful AI assistant.";
        if (!string.IsNullOrWhiteSpace(context))
        {
            systemPrompt += $"\n\nMemory Context:\n{context}";
        }
        messages.Add(new Message("system", systemPrompt));

        // Add History
        messages.AddRange(_history);
        
        // Add Current Input
        messages.Add(new Message("user", input));

        // 2. Loop
        int iteration = 0;
        string? finalContent = null;
        
        var currentLoopMessages = new List<Message>(messages);

        while (iteration < MaxIterations)
        {
            iteration++;
            
            var response = await _provider.ChatAsync(
                currentLoopMessages, 
                _tools.GetDefinitions()
            );

            if (!response.HasToolCalls)
            {
                finalContent = response.Content;
                break;
            }
            
            // Add Assistant Message with Tool Calls
            var assistantMsg = new Message("assistant", response.Content)
            {
                ToolCalls = response.ToolCalls
            };
            currentLoopMessages.Add(assistantMsg);

            // Execute Tools
            foreach (var toolCall in response.ToolCalls)
            {
                var result = await _tools.ExecuteAsync(toolCall.Name, toolCall.Arguments);
                result ??= "Tool execution returned no result.";
                
                // Add Tool Message
                var toolMsg = new Message("tool", result)
                {
                    ToolCallId = toolCall.Id
                };
                currentLoopMessages.Add(toolMsg);
            }
        }
        
        // Save to history
        _history.Add(new Message("user", input));
        if (finalContent != null)
        {
            _history.Add(new Message("assistant", finalContent));
        }

        return finalContent ?? "No response.";
    }
}