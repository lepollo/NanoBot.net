using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Nanobot.Core.Models;
using System.Net;

namespace Nanobot.Core.Channels;

public class TelegramChannel
{
    private readonly string _token;
    private readonly Func<InboundMessage, Task<OutboundMessage?>> _onMessage;
    private ITelegramBotClient? _botClient;
    private CancellationTokenSource? _cts;

    public TelegramChannel(string token, Func<InboundMessage, Task<OutboundMessage?>> onMessage)
    {
        _token = token;
        _onMessage = onMessage;
    }

    public async Task StartAsync()
    {
        _botClient = new TelegramBotClient(_token);
        _cts = new CancellationTokenSource();

        var receiverOptions = new Telegram.Bot.Polling.ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // Receive all update types
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: _cts.Token
        );

        var me = await _botClient.GetMe(_cts.Token);
        Console.WriteLine($"Telegram bot @{me.Username} started.");
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message) return;
        if (message.Text is not { } messageText) return;

        var senderId = message.From?.Id.ToString() ?? "unknown";
        var chatId = message.Chat.Id.ToString();

        var inbound = new InboundMessage(
            Channel: "telegram",
            SenderId: senderId,
            ChatId: chatId,
            Content: messageText
        );

        var response = await _onMessage(inbound);

        if (response != null)
        {
            var htmlContent = MarkdownToHtml(response.Content);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: htmlContent,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Telegram Error: {exception.Message}");
        return Task.CompletedTask;
    }

    private string MarkdownToHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        // Simple markdown to HTML conversion (Telegram subset)
        var result = text;
        
        // Escape HTML
        result = WebUtility.HtmlEncode(result);

        // Bold
        result = Regex.Replace(result, @"\*\*(.+?)\*\*", "<b>$1</b>");
        // Italic
        result = Regex.Replace(result, @"\*(.+?)\*", "<i>$1</i>");
        // Monospace (Code)
        result = Regex.Replace(result, @"`(.+?)`", "<code>$1</code>");
        // Pre
        result = Regex.Replace(result, @"```([\s\S]*?)```", "<pre>$1</pre>");

        return result;
    }
}
