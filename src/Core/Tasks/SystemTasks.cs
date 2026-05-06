namespace SoftcurseLab.Core.Tasks;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Runs Windows built-in Disk Cleanup in silent mode (sageset/sagerun).
/// Pre-configures all cleanup categories on first run.
/// </summary>
public class DiskCleanupTask : BaseTask
{
    public override bool RequiresAdmin => true;

    private const int SAGE_ID = 64; // arbitrary sageset ID we own

    public override async Task RunAsync(CancellationToken ct)
    {
        const string NAME = "Disk Cleanup";

        if (!IsElevated())
        {
            Log(NAME, "Skipped — requires Administrator.", TaskStatus.Skipped);
            return;
        }

        // Configure all cleanup flags silently via registry
        Log(NAME, "Configuring cleanup categories...", TaskStatus.Running);
        await ConfigureCleanupCategories();

        Log(NAME, "Running disk cleanup (silent)...", TaskStatus.Running);
        var (code, _, err) = await RunProcessAsync(
            "cleanmgr.exe", $"/sagerun:{SAGE_ID}", ct, timeoutMs: 600_000);

        if (code == 0)
            Log(NAME, "Disk cleanup completed successfully.", TaskStatus.Success);
        else
            Log(NAME, $"Cleanup exited {code}. {err[..Math.Min(80, err.Length)]}", TaskStatus.Warning);
    }

    private static Task ConfigureCleanupCategories()
    {
        // Write registry keys that cleanmgr reads for sagerun
        // Each DWORD value = 2 means "selected for cleanup"
        const string regBase = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches";
        string[] categories =
        {
            "Active Setup Temp Folders", "BranchCache", "Content Indexer Cleaner",
            "D3D Shader Cache", "Delivery Optimization Files", "Device Driver Packages",
            "Diagnostic Data Viewer database files", "Downloaded Program Files",
            "Internet Cache Files", "Memory Dump Files", "Offline Pages Files",
            "Old ChkDsk Files", "Previous Installations", "Recycle Bin",
            "RetailDemo Offline Content", "Service Pack Cleanup", "Setup Log Files",
            "System error memory dump files", "System error minidump files",
            "Temporary Files", "Temporary Setup Files", "Thumbnail Cache",
            "Update Cleanup", "Upgrade Discarded Files", "User file versions",
            "Windows Defender", "Windows Error Reporting Files",
            "Windows ESD installation files", "Windows Upgrade Log Files"
        };

        foreach (var cat in categories)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(
                    $@"{regBase}\{cat}", true);
                key?.SetValue($"StateFlags{64:D4}", 2, Microsoft.Win32.RegistryValueKind.DWord);
            }
            catch { /* category may not exist on this machine */ }
        }
        return Task.CompletedTask;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Runs DISM /StartComponentCleanup to remove superseded Windows Update components.
/// This can reclaim several GB that regular cleaners miss.
/// </summary>
public class DismCleanupTask : BaseTask
{
    public override bool RequiresAdmin => true;

    public override async Task RunAsync(CancellationToken ct)
    {
        const string NAME = "DISM Component Store";

        if (!IsElevated())
        {
            Log(NAME, "Skipped — requires Administrator.", TaskStatus.Skipped);
            return;
        }

        // First: check image health
        Log(NAME, "Checking Windows image health...", TaskStatus.Running);
        var (chkCode, chkOut, _) = await RunProcessAsync(
            "dism.exe", "/Online /Cleanup-Image /CheckHealth", ct, 120_000);

        bool healthy = chkCode == 0 && !chkOut.Contains("repairable", StringComparison.OrdinalIgnoreCase);
        Log(NAME, $"Image health: {(healthy ? "HEALTHY" : "NEEDS REPAIR")}.", healthy ? TaskStatus.Success : TaskStatus.Warning);

        // StartComponentCleanup
        Log(NAME, "Cleaning component store (may take 5–15 min)...", TaskStatus.Running);
        var (code, out_, err) = await RunProcessAsync(
            "dism.exe",
            "/Online /Cleanup-Image /StartComponentCleanup /ResetBase",
            ct,
            timeoutMs: 1_800_000); // 30 min max

        if (code == 0)
            Log(NAME, "Component store cleanup complete. Restart recommended.", TaskStatus.Success);
        else if (code == -2146498547) // 0x800F0816 — nothing to clean
            Log(NAME, "Component store already clean.", TaskStatus.Skipped);
        else
            Log(NAME, $"DISM exited {code}. {err[..Math.Min(80, err.Length)]}", TaskStatus.Warning);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Checks driver health using DISM and pnputil, logs unsigned or problematic drivers.
/// </summary>
public class DriverHealthTask : BaseTask
{
    public override bool RequiresAdmin => true;

    public override async Task RunAsync(CancellationToken ct)
    {
        const string NAME = "Driver Health Check";

        if (!IsElevated())
        {
            Log(NAME, "Skipped — requires Administrator.", TaskStatus.Skipped);
            return;
        }

        Log(NAME, "Querying installed drivers via pnputil...", TaskStatus.Running);
        var (code, out_, _) = await RunProcessAsync("pnputil.exe", "/enum-drivers", ct, 60_000);

        if (code != 0)
        {
            Log(NAME, "pnputil failed. Falling back to DISM driver enum...", TaskStatus.Warning);
            var (dc, dOut, _) = await RunProcessAsync(
                "dism.exe", "/Online /Get-Drivers /Format:Table", ct, 60_000);
            out_ = dOut;
        }

        // Count unsigned drivers
        int total = out_.Split("Published Name:", StringSplitOptions.RemoveEmptyEntries).Length - 1;
        int unsigned = out_.Split(["Not digitally signed", "Unsigned"], StringSplitOptions.None).Length - 1;

        if (unsigned > 0)
            Log(NAME, $"Found {unsigned} unsigned driver(s) out of {total}. Review Device Manager!", TaskStatus.Warning);
        else
            Log(NAME, $"All {total} drivers appear signed and healthy.", TaskStatus.Success);

        // Also run SFC scan report (non-interactive)
        Log(NAME, "Running System File Checker (sfc /verifyonly)...", TaskStatus.Running);
        var (sfcCode, sfcOut, _) = await RunProcessAsync("sfc.exe", "/verifyonly", ct, 300_000);

        bool sfcOk = sfcCode == 0 || sfcOut.Contains("did not find", StringComparison.OrdinalIgnoreCase);
        Log(NAME, sfcOk ? "SFC: No integrity violations found." : "SFC: Windows found integrity violations — run 'sfc /scannow'.", sfcOk ? TaskStatus.Success : TaskStatus.Warning);
    }
}
