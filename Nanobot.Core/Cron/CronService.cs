using System.Text.Json;
using Cronos;
using Microsoft.Extensions.Logging;

namespace Nanobot.Core.Cron;

public class CronService
{
    private readonly string _storePath;
    private readonly Func<CronJob, Task<string?>>? _onJob;
    private CronStore? _store;
    private bool _running;
    private CancellationTokenSource? _cts;

    public CronService(string storePath, Func<CronJob, Task<string?>>? onJob = null)
    {
        _storePath = storePath;
        _onJob = onJob;
    }

    private long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private long? ComputeNextRun(CronSchedule schedule, long nowMs)
    {
        if (schedule.Kind == "at")
        {
            return schedule.AtMs > nowMs ? schedule.AtMs : null;
        }

        if (schedule.Kind == "every")
        {
            if (schedule.EveryMs == null || schedule.EveryMs <= 0) return null;
            return nowMs + schedule.EveryMs;
        }

        if (schedule.Kind == "cron" && !string.IsNullOrEmpty(schedule.Expr))
        {
            try
            {
                var cronExpression = CronExpression.Parse(schedule.Expr, CronFormat.Standard);
                var nextUtc = cronExpression.GetNextOccurrence(DateTime.UtcNow);
                return nextUtc.HasValue ? new DateTimeOffset(nextUtc.Value).ToUnixTimeMilliseconds() : null;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private void LoadStore()
    {
        if (_store != null) return;

        if (File.Exists(_storePath))
        {
            try
            {
                var json = File.ReadAllText(_storePath);
                _store = JsonSerializer.Deserialize<CronStore>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                _store = new CronStore();
            }
        }
        
        _store ??= new CronStore();
    }

    private void SaveStore()
    {
        if (_store == null) return;
        var dir = Path.GetDirectoryName(_storePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        
        var json = JsonSerializer.Serialize(_store, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_storePath, json);
    }

    public async Task StartAsync()
    {
        _running = true;
        _cts = new CancellationTokenSource();
        LoadStore();
        RecomputeNextRuns();
        SaveStore();
        _ = RunLoop(_cts.Token);
    }

    public void Stop()
    {
        _running = false;
        _cts?.Cancel();
    }

    private void RecomputeNextRuns()
    {
        if (_store == null) return;
        long now = NowMs();
        foreach (var job in _store.Jobs)
        {
            if (job.Enabled)
            {
                job.State.NextRunAtMs = ComputeNextRun(job.Schedule, now);
            }
        }
    }

    private async Task RunLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _running)
        {
            long now = NowMs();
            var dueJobs = _store?.Jobs.Where(j => j.Enabled && j.State.NextRunAtMs != null && now >= j.State.NextRunAtMs).ToList();

            if (dueJobs != null && dueJobs.Count > 0)
            {
                foreach (var job in dueJobs)
                {
                    await ExecuteJobAsync(job);
                }
                SaveStore();
            }

            // Find next wake time
            long? nextWake = _store?.Jobs
                .Where(j => j.Enabled && j.State.NextRunAtMs != null)
                .Select(j => j.State.NextRunAtMs)
                .Min();

            int delayMs = 1000; // Check every second by default
            if (nextWake.HasValue)
            {
                var waitTime = (int)(nextWake.Value - NowMs());
                delayMs = Math.Clamp(waitTime, 100, 1000);
            }

            try
            {
                await Task.Delay(delayMs, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task ExecuteJobAsync(CronJob job)
    {
        long startMs = NowMs();
        try
        {
            if (_onJob != null)
            {
                await _onJob(job);
            }
            job.State.LastStatus = "ok";
            job.State.LastError = null;
        }
        catch (Exception ex)
        {
            job.State.LastStatus = "error";
            job.State.LastError = ex.Message;
        }

        job.State.LastRunAtMs = startMs;
        job.UpdatedAtMs = NowMs();

        if (job.Schedule.Kind == "at")
        {
            if (job.DeleteAfterRun)
            {
                _store?.Jobs.Remove(job);
            }
            else
            {
                job.Enabled = false;
                job.State.NextRunAtMs = null;
            }
        }
        else
        {
            job.State.NextRunAtMs = ComputeNextRun(job.Schedule, NowMs());
        }
    }

    public List<CronJob> ListJobs(bool includeDisabled = false)
    {
        LoadStore();
        var jobs = includeDisabled ? _store!.Jobs : _store!.Jobs.Where(j => j.Enabled).ToList();
        return jobs.OrderBy(j => j.State.NextRunAtMs ?? long.MaxValue).ToList();
    }

    public CronJob AddJob(string name, CronSchedule schedule, string message, bool deleteAfterRun = false)
    {
        LoadStore();
        var now = NowMs();
        var job = new CronJob
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            Name = name,
            Enabled = true,
            Schedule = schedule,
            Payload = new CronPayload { Message = message },
            State = new CronJobState { NextRunAtMs = ComputeNextRun(schedule, now) },
            CreatedAtMs = now,
            UpdatedAtMs = now,
            DeleteAfterRun = deleteAfterRun
        };

        _store!.Jobs.Add(job);
        SaveStore();
        return job;
    }

    public bool RemoveJob(string jobId)
    {
        LoadStore();
        var job = _store!.Jobs.FirstOrDefault(j => j.Id == jobId);
        if (job != null)
        {
            _store.Jobs.Remove(job);
            SaveStore();
            return true;
        }
        return false;
    }
}