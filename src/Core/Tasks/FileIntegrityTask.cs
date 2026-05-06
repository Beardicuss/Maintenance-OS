using System.Collections.Concurrent;
using System.IO;

namespace SoftcurseLab.Core.Tasks;

/// <summary>
/// Watches sensitive Windows folders for unauthorised file changes while the screensaver runs.
/// Uses FileSystemWatcher. Suspicious events are logged immediately.
/// </summary>
public class FileIntegrityTask : BaseTask
{
    public override bool RequiresAdmin => false;

    private static readonly string[] WatchedPaths =
    {
        @"C:\Windows\System32",
        @"C:\Windows\SysWOW64",
        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)),
    };

    // Extensions that are suspicious when written at runtime
    private static readonly HashSet<string> SuspiciousExts =
        new(StringComparer.OrdinalIgnoreCase) { ".exe", ".dll", ".sys", ".bat", ".cmd", ".ps1", ".vbs", ".js" };

    private readonly ConcurrentQueue<string> _alerts = new();
    private int _changeCount;

    public override async Task RunAsync(CancellationToken ct)
    {
        const string NAME = "File Integrity Monitor";
        Log(NAME, "Starting file system watchers...", TaskStatus.Running);

        var watchers = new List<FileSystemWatcher>();

        foreach (var path in WatchedPaths)
        {
            if (!Directory.Exists(path)) continue;
            try
            {
                var w = new FileSystemWatcher(path)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                    Filter = "*.*",
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };
                w.Created += OnChange;
                w.Changed += OnChange;
                w.Deleted += OnChange;
                w.Renamed += OnRename;
                watchers.Add(w);
            }
            catch { /* may not have read access to all dirs */ }
        }

        if (watchers.Count == 0)
        {
            Log(NAME, "No accessible paths to monitor.", TaskStatus.Skipped);
            return;
        }

        Stats?.SetFwPaths(watchers.Count);
        Log(NAME, $"Monitoring {watchers.Count} system folders for changes...", TaskStatus.Running);

        // Poll for alerts every 15 seconds while screensaver is running
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(15_000, ct).ContinueWith(_ => { });

            if (_alerts.Count > 0)
            {
                var items = new List<string>();
                while (_alerts.TryDequeue(out var a)) items.Add(a);
                Log(NAME, $"⚠ {items.Count} suspicious change(s): {string.Join("; ", items.Take(3))}", TaskStatus.Warning);
            }
            else
            {
                Log(NAME, $"Integrity OK — {_changeCount} benign events observed.", TaskStatus.Success);
            }
        }

        foreach (var w in watchers) { w.EnableRaisingEvents = false; w.Dispose(); }
    }

    private void OnChange(object s, FileSystemEventArgs e)
    {
        Interlocked.Increment(ref _changeCount);
        bool suspect = SuspiciousExts.Contains(Path.GetExtension(e.Name ?? ""));
        Stats?.AddFwEvent(suspect);
        if (suspect)
            _alerts.Enqueue($"{e.ChangeType} {e.Name}");
    }

    private void OnRename(object s, RenamedEventArgs e)
    {
        Interlocked.Increment(ref _changeCount);
        bool suspect = SuspiciousExts.Contains(Path.GetExtension(e.Name ?? ""));
        Stats?.AddFwEvent(suspect);
        if (suspect)
            _alerts.Enqueue($"RENAMED→{e.Name}");
    }
}
