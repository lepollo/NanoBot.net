namespace Nanobot.Core.Cron;

public class CronSchedule
{
    public string Kind { get; set; } = "every"; // "at", "every", "cron"
    public long? AtMs { get; set; }
    public long? EveryMs { get; set; }
    public string? Expr { get; set; }
    public string? Tz { get; set; }
}

public class CronPayload
{
    public string Kind { get; set; } = "agent_turn";
    public string Message { get; set; } = "";
    public bool Deliver { get; set; }
    public string? Channel { get; set; }
    public string? To { get; set; }
}

public class CronJobState
{
    public long? NextRunAtMs { get; set; }
    public long? LastRunAtMs { get; set; }
    public string? LastStatus { get; set; }
    public string? LastError { get; set; }
}

public class CronJob
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public CronSchedule Schedule { get; set; } = new();
    public CronPayload Payload { get; set; } = new();
    public CronJobState State { get; set; } = new();
    public long CreatedAtMs { get; set; }
    public long UpdatedAtMs { get; set; }
    public bool DeleteAfterRun { get; set; }
}

public class CronStore
{
    public int Version { get; set; } = 1;
    public List<CronJob> Jobs { get; set; } = new();
}
