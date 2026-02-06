namespace Nanobot.Core.Heartbeat;

public class HeartbeatService
{
    private readonly string _workspace;
    private readonly Func<string, Task<string>>? _onHeartbeat;
    private readonly int _intervalS;
    private bool _running;
    private CancellationTokenSource? _cts;

    private const string HeartbeatPrompt = """
Read HEARTBEAT.md in your workspace (if it exists).
Follow any instructions or tasks listed there.
If nothing needs attention, reply with just: HEARTBEAT_OK
""";

    private const string HeartbeatOkToken = "HEARTBEAT_OK";

    public HeartbeatService(string workspace, Func<string, Task<string>>? onHeartbeat = null, int intervalS = 1800)
    {
        _workspace = workspace;
        _onHeartbeat = onHeartbeat;
        _intervalS = intervalS;
    }

    private string HeartbeatFile => Path.Combine(_workspace, "HEARTBEAT.md");

    private string? ReadHeartbeatFile()
    {
        if (File.Exists(HeartbeatFile))
        {
            try { return File.ReadAllText(HeartbeatFile); }
            catch { return null; }
        }
        return null;
    }

    private bool IsHeartbeatEmpty(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return true;

        var skipPatterns = new[] { "- [ ]", "* [ ]", "- [x]", "* [x]" };
        foreach (var line in content.Split('\n'))
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("<!--") || skipPatterns.Contains(trimmedLine))
                continue;
            return false;
        }
        return true;
    }

    public async Task StartAsync()
    {
        _running = true;
        _cts = new CancellationTokenSource();
        _ = RunLoop(_cts.Token);
    }

    public void Stop()
    {
        _running = false;
        _cts?.Cancel();
    }

    private async Task RunLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _running)
        {
            try
            {
                await Task.Delay(_intervalS * 1000, token);
                if (_running) await TickAsync();
            }
            catch (TaskCanceledException) { break; }
            catch (Exception) { /* Log error */ }
        }
    }

    private async Task TickAsync()
    {
        var content = ReadHeartbeatFile();
        if (IsHeartbeatEmpty(content)) return;

        if (_onHeartbeat != null)
        {
            try
            {
                var response = await _onHeartbeat(HeartbeatPrompt);
                if (response.ToUpper().Replace("_", "").Contains(HeartbeatOkToken))
                {
                    // No action needed
                }
            }
            catch { /* Log error */ }
        }
    }
}