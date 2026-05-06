using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;

namespace SoftcurseLab.Core;

/// <summary>
/// Bridges the C# maintenance engine to the JavaScript UI running in WebView2.
/// All public methods are thread-safe and marshal to the UI dispatcher internally.
/// Calls window.handleCyberEvent(data) in the loaded HTML page.
/// </summary>
public class UIBridge
{
    private readonly WebView2    _wv;
    private readonly Dispatcher  _disp;
    private readonly MaintenanceStats _stats;
    private bool _webviewReady;

    // C# task name → JS taskId (must match TASK_NAME_MAP in CyberUI.html)
    private static readonly Dictionary<string, string> TaskIdMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Defender Quick Scan"]    = "defender",
            ["Browser Cache Purge"]    = "cache",
            ["File Integrity Monitor"] = "integrity",
            ["Process Guard"]          = "process",
            ["Auto Defrag"]            = "defrag",
            ["Disk Cleanup"]           = "cleanup",
            ["DISM Component Store"]   = "dism",
            ["Driver Health Check"]    = "driver",
        };

    private static readonly Dictionary<TaskStatus, (string status, double progress)> StatusMap = new()
    {
        [TaskStatus.Idle]    = ("idle",    0),
        [TaskStatus.Running] = ("running", -1),
        [TaskStatus.Success] = ("done",    100),
        [TaskStatus.Warning] = ("warning", 100),
        [TaskStatus.Error]   = ("error",   100),
        [TaskStatus.Skipped] = ("skipped", 0),
    };

    public UIBridge(WebView2 webView, Dispatcher dispatcher, MaintenanceStats stats)
    {
        _wv    = webView;
        _disp  = dispatcher;
        _stats = stats;
    }

    public void SetReady(bool ready) => _webviewReady = ready;

    // ── Called by TaskEngine after WebView2 is ready ─────────────────────
    public void SendSystemInfo(bool isAdmin, int fwPaths)
    {
        Send(new { type = "system_info", adminMode = isAdmin, fwPaths });
    }

    // ── Called for every TaskLogEntry ─────────────────────────────────────
    public void PostLog(TaskLogEntry entry)
    {
        if (!_webviewReady) return;

        // 1. Log entry → terminal feed
        string level = entry.Status switch
        {
            TaskStatus.Error   => "err",
            TaskStatus.Warning => "wrn",
            TaskStatus.Success => "ok",
            _                  => "sys"
        };
        Send(new { type = "log_entry", source = entry.TaskName, message = entry.Message, level });

        // 2. Task card update
        string taskId = TaskIdMap.TryGetValue(entry.TaskName, out var id) ? id : "sys";
        if (TaskIdMap.ContainsKey(entry.TaskName))
        {
            var (status, progress) = StatusMap.TryGetValue(entry.Status, out var sm)
                ? sm : ("idle", 0.0);

            Send(new
            {
                type     = "task_update",
                taskName = entry.TaskName,
                taskId,
                status,
                message  = entry.Message,
                progress
            });
        }

        // 3. Push updated stats snapshot
        PushStats();
    }

    // ── Push stats snapshot to JS ─────────────────────────────────────────
    public void PushStats()
    {
        if (!_webviewReady) return;
        var s = _stats.Snapshot();

        Send(new
        {
            type           = "stat_update",
            filesScanned   = s.FilesScanned,
            bytesFreed     = s.BytesFreed,
            threatsKilled  = s.ThreatsKilled,
            integrity      = s.Integrity >= 0 ? (object)s.Integrity : null,
            fwEvents       = s.FwEvents,
            fwAlerts       = s.FwAlerts,
            fwPaths        = s.FwPaths,
        });

        // Build threat matrix: [injection, suspicious, network, integrity, hashes, drivers]
        int[] matrix = new[]
        {
            0,                      // process injection — no direct measure
            0,                      // suspicious files  — from file watcher alerts
            s.NetworkAnomalies,
            s.FwAlerts,
            s.ThreatsKilled,        // malicious hashes
            s.UnsignedDrivers,
        };
        // borrow FwAlerts for suspicious files too
        matrix[1] = s.FwAlerts;

        Send(new { type = "threat_update", matrix });
    }

    // ── Push simulated vitals (approximated from environment) ─────────────
    public void PushVitals()
    {
        if (!_webviewReady) return;
        var proc  = System.Diagnostics.Process.GetCurrentProcess();
        long   wsMb = proc.WorkingSet64 / 1_048_576;
        int    cores = Environment.ProcessorCount;

        // We can't get real CPU% without a PerformanceCounter (requires admin or
        // elevated startup) — send structural info only; JS handles fake fluctuation
        Send(new
        {
            type     = "vitals_update",
            cores    = cores,
            totalRam = GetTotalRamGb(),
        });
    }

    // ── Serialise and inject ──────────────────────────────────────────────
    private void Send(object payload)
    {
        if (!_webviewReady) return;
        try
        {
            string json = JsonSerializer.Serialize(payload,
                new JsonSerializerOptions { PropertyNamingPolicy = null });
            _disp.BeginInvoke(async () =>
            {
                try { await _wv.ExecuteScriptAsync($"window.handleCyberEvent({json})"); }
                catch { /* WebView2 may not be ready yet */ }
            });
        }
        catch { }
    }

    private static int GetTotalRamGb()
    {
        try
        {
            // Quick WMI-free read via GlobalMemoryStatusEx
            var status = new NativeMethods.MEMORYSTATUSEX();
            NativeMethods.GlobalMemoryStatusEx(status);
            return (int)(status.ullTotalPhys / 1_073_741_824);
        }
        catch { return 0; }
    }
}

// P/Invoke helper for memory info
internal static class NativeMethods
{
    [System.Runtime.InteropServices.StructLayout(
        System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public class MEMORYSTATUSEX
    {
        public uint  dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint  dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll",
        CharSet = System.Runtime.InteropServices.CharSet.Auto,
        SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(
        System.Runtime.InteropServices.UnmanagedType.Bool)]
    public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
}
