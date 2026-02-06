using System.Text.Json;
using System.Text.Json.Nodes;
using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using Nanobot.Core.Models;

namespace Nanobot.Core.Providers;

public class OpenAIProvider : ILLMProvider
{
    private readonly string _apiKey;
    private readonly string? _baseUrl;
    private readonly string _defaultModel;
    private readonly OpenAIClient _client;

    public OpenAIProvider(string apiKey, string? baseUrl = null, string defaultModel = "gpt-4o")
    {
        _apiKey = apiKey;
        _baseUrl = baseUrl;
        _defaultModel = defaultModel;

        var options = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(baseUrl))
        {
            options.Endpoint = new Uri(baseUrl);
        }
        
        _client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
    }

    public string GetDefaultModel() => _defaultModel;

    public async Task<LLMResponse> ChatAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7)
    {
        var targetModel = model ?? _defaultModel;
        var chatClient = _client.GetChatClient(targetModel);

        var chatMessages = new List<ChatMessage>();
        foreach (var msg in messages)
        {
            var content = msg.Content ?? "";
            switch (msg.Role.ToLower())
            {
                case "system":
                    chatMessages.Add(new SystemChatMessage(content));
                    break;
                case "user":
                    chatMessages.Add(new UserChatMessage(content));
                    break;
                case "assistant":
                    AssistantChatMessage assistantMsg;
                    if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
                    {
                        var toolCalls = msg.ToolCalls.Select(tc => 
                            ChatToolCall.CreateFunctionToolCall(tc.Id, tc.Name, BinaryData.FromString(tc.Arguments?.ToJsonString() ?? "{}"))
                        ).ToList();

                        if (string.IsNullOrEmpty(msg.Content))
                        {
                            assistantMsg = new AssistantChatMessage(toolCalls);
                        }
                        else
                        {
                            assistantMsg = new AssistantChatMessage(toolCalls);
                            assistantMsg.Content.Add(ChatMessageContentPart.CreateTextPart(msg.Content));
                        }
                    }
                    else
                    {
                        assistantMsg = new AssistantChatMessage(content);
                    }
                    chatMessages.Add(assistantMsg);
                    break;
                case "tool":
                    if (!string.IsNullOrEmpty(msg.ToolCallId))
                    {
                        chatMessages.Add(new ToolChatMessage(msg.ToolCallId, content));
                    }
                    break;
                default:
                    chatMessages.Add(new UserChatMessage(content));
                    break;
            }
        }

        var options = new ChatCompletionOptions()
        {
            MaxOutputTokenCount = maxTokens,
            Temperature = (float)temperature
        };

        if (tools != null)
        {
            foreach (var toolNode in tools)
            {
                var funcNode = toolNode["function"];
                if (funcNode != null)
                {
                    string name = funcNode["name"]?.ToString() ?? "unknown";
                    string description = funcNode["description"]?.ToString() ?? "";
                    var parameters = funcNode["parameters"];

                    BinaryData? paramSchema = null;
                    if (parameters != null)
                    {
                        paramSchema = BinaryData.FromString(parameters.ToJsonString());
                    }

                    options.Tools.Add(ChatTool.CreateFunctionTool(name, description, paramSchema));
                }
            }
        }

        try
        {
            ChatCompletion completion = await chatClient.CompleteChatAsync(chatMessages, options);
            return ParseResponse(completion);
        }
        catch (Exception ex)
        {
            return new LLMResponse($"Error: {ex.Message}") { FinishReason = "error" };
        }
    }

    private LLMResponse ParseResponse(ChatCompletion completion)
    {
        var result = new LLMResponse
        {
            Content = completion.Content?.Count > 0 ? completion.Content[0].Text : null,
            FinishReason = completion.FinishReason.ToString().ToLower(),
            Usage = completion.Usage != null ? new Dictionary<string, int>
            {
                { "prompt_tokens", completion.Usage.InputTokenCount },
                { "completion_tokens", completion.Usage.OutputTokenCount },
                { "total_tokens", completion.Usage.TotalTokenCount }
            } : new Dictionary<string, int>()
        };

        if (completion.ToolCalls != null && completion.ToolCalls.Count > 0)
        {
            foreach (var toolCall in completion.ToolCalls)
            {
                string id = toolCall.Id;
                string name = toolCall.FunctionName;
                BinaryData argsData = toolCall.FunctionArguments;
                
                JsonNode? argsNode = null;
                if (argsData != null)
                {
                    try
                    {
                        argsNode = JsonNode.Parse(argsData.ToString());
                    }
                    catch { }
                }

                result.ToolCalls.Add(new ToolCallRequest(id, name, argsNode));
            }
        }

        return result;
    }
}