namespace Nanobot.Core.Models;

public record InboundMessage(
    string Channel,
    string SenderId,
    string ChatId,
    string Content,
    List<string>? Media = null,
    Dictionary<string, object>? Metadata = null
)
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string SessionKey => $"{Channel}:{ChatId}";
}

public record OutboundMessage(
    string Channel,
    string ChatId,
    string Content,
    string? ReplyTo = null,
    List<string>? Media = null,
    Dictionary<string, object>? Metadata = null
);
