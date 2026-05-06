using SoftcurseLab.Core.Tasks;

namespace SoftcurseLab.Core;

public class TaskEngine
{
    private readonly CancellationTokenSource _cts = new();
    private readonly List<IMaintenanceTask> _tasks;
    public readonly MaintenanceStats Stats = new();

    public event Action<TaskLogEntry>? OnLog;

    public TaskEngine()
    {
        _tasks = new List<IMaintenanceTask>
        {
            new DefenderScanTask(),
            new BrowserCacheTask(),
            new FileIntegrityTask(),
            new ProcessGuardTask(),
            new DefragTask(),
            new DiskCleanupTask(),
            new DismCleanupTask(),
            new DriverHealthTask(),
        };

        // Wire log events + inject shared stats accumulator
        foreach (var t in _tasks)
        {
            t.OnLog  += entry => OnLog?.Invoke(entry);
            if (t is BaseTask bt) bt.Stats = Stats;
        }
    }

    public async Task RunAllAsync()
    {
        var ct = _cts.Token;

        // Non-privileged tasks run immediately and in parallel
        var nonPrivTasks = _tasks
            .Where(t => !t.RequiresAdmin)
            .Select(t => t.RunAsync(ct));

        // Admin tasks run sequentially to avoid I/O contention
        var adminTasks = async () =>
        {
            foreach (var t in _tasks.Where(t => t.RequiresAdmin))
            {
                if (ct.IsCancellationRequested) break;
                await t.RunAsync(ct);
                await Task.Delay(5000, ct).ContinueWith(_ => { });
            }
        };

        await Task.WhenAll(nonPrivTasks.Append(adminTasks()));
    }

    public void Cancel() => _cts.Cancel();
}

public interface IMaintenanceTask
{
    bool RequiresAdmin { get; }
    event Action<TaskLogEntry>? OnLog;
    Task RunAsync(CancellationToken ct);
}
