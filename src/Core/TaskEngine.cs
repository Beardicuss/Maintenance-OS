using System.IO;
using SoftcurseLab.Core.Tasks;
using SoftcurseLab.UI;

namespace SoftcurseLab.Core;

public class TaskEngine
{
    private readonly CancellationTokenSource _cts = new();
    private readonly List<IMaintenanceTask> _tasks = new();
    public readonly MaintenanceStats Stats = new();

    public event Action<TaskLogEntry>? OnLog;

    public TaskEngine()
    {
        var config = LoadConfig();

        if (IsTaskEnabled(config, "Defender"))     _tasks.Add(new DefenderScanTask());
        if (IsTaskEnabled(config, "BrowserCache")) _tasks.Add(new BrowserCacheTask());
        if (IsTaskEnabled(config, "FileIntegrity"))_tasks.Add(new FileIntegrityTask());
        if (IsTaskEnabled(config, "ProcessGuard")) _tasks.Add(new ProcessGuardTask());
        if (IsTaskEnabled(config, "Defrag"))       _tasks.Add(new DefragTask());
        if (IsTaskEnabled(config, "DiskCleanup"))  _tasks.Add(new DiskCleanupTask());
        if (IsTaskEnabled(config, "DismCleanup"))  _tasks.Add(new DismCleanupTask());
        if (IsTaskEnabled(config, "DriverHealth")) _tasks.Add(new DriverHealthTask());

        // Wire log events + inject shared stats accumulator
        foreach (var t in _tasks)
        {
            t.OnLog += entry => OnLog?.Invoke(entry);
            if (t is BaseTask bt) bt.Stats = Stats;
        }
    }

    private static Dictionary<string, string> LoadConfig()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            string path = ConfigWindow.ConfigPath;
            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith(';')) continue;
                    int idx = line.IndexOf('=');
                    if (idx > 0)
                    {
                        dict[line[..idx].Trim()] = line[(idx + 1)..].Trim();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ScreensaverWindow.Log($"TaskEngine.LoadConfig exception: {ex.Message}");
        }
        return dict;
    }

    private static bool IsTaskEnabled(Dictionary<string, string> config, string key)
    {
        return !config.TryGetValue(key, out var val) || !string.Equals(val, "False", StringComparison.OrdinalIgnoreCase);
    }

    public async Task RunAllAsync()
    {
        var ct = _cts.Token;

        // Non-privileged tasks run immediately and in parallel (with exception isolation)
        var nonPrivTasks = _tasks
            .Where(t => !t.RequiresAdmin)
            .Select(async t =>
            {
                try
                {
                    await t.RunAsync(ct);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    ScreensaverWindow.Log($"Task execution error in non-priv task {t.GetType().Name}: {ex}");
                }
            });

        // Admin tasks run sequentially to avoid I/O contention (with exception isolation)
        var adminTasks = async () =>
        {
            foreach (var t in _tasks.Where(t => t.RequiresAdmin))
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    await t.RunAsync(ct);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    ScreensaverWindow.Log($"Task execution error in admin task {t.GetType().Name}: {ex}");
                }
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
