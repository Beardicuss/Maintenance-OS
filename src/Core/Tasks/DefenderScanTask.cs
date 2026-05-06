using System.IO;

namespace SoftcurseLab.Core.Tasks;

/// <summary>Triggers Windows Defender quick scan via MpCmdRun.exe</summary>
public class DefenderScanTask : BaseTask
{
    public override bool RequiresAdmin => false;

    private static readonly string[] _defenderPaths =
    {
        @"C:\Program Files\Windows Defender\MpCmdRun.exe",
        @"C:\ProgramData\Microsoft\Windows Defender\Platform"
    };

    public override async Task RunAsync(CancellationToken ct)
    {
        const string NAME = "Defender Quick Scan";
        Log(NAME, "Locating Windows Defender...", TaskStatus.Running);

        // Find MpCmdRun.exe (may be in versioned subdir under Platform)
        string? mpCmd = FindMpCmdRun();
        if (mpCmd == null)
        {
            Log(NAME, "MpCmdRun.exe not found — Defender may not be installed.", TaskStatus.Warning);
            return;
        }

        Log(NAME, $"Starting quick scan via {System.IO.Path.GetDirectoryName(mpCmd)}...", TaskStatus.Running);
        var (code, out_, err) = await RunProcessAsync(mpCmd, "-Scan -ScanType 1", ct, timeoutMs: 300_000);

        // Parse file count from output (Defender reports "Scanning x files")
        var match = System.Text.RegularExpressions.Regex.Match(out_, @"(\d[\d,]+)\s+files?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && long.TryParse(match.Groups[1].Value.Replace(",",""), out long fc))
            Stats?.AddFilesScanned(fc);

        if (code == 0)
            Log(NAME, "Quick scan completed — no threats found.", TaskStatus.Success);
        else if (code == 2)
            Log(NAME, "Scan complete — THREATS DETECTED. Open Defender immediately!", TaskStatus.Error);
        else
            Log(NAME, $"Scan finished (exit {code}). {(err.Length > 0 ? err[..Math.Min(80, err.Length)] : out_[..Math.Min(80, out_.Length)])}", TaskStatus.Warning);
    }

    private static string? FindMpCmdRun()
    {
        // Direct path
        if (File.Exists(_defenderPaths[0])) return _defenderPaths[0];

        // Versioned path under Platform
        if (Directory.Exists(_defenderPaths[1]))
        {
            var dirs = Directory.GetDirectories(_defenderPaths[1])
                                .OrderByDescending(d => d).ToArray();
            foreach (var d in dirs)
            {
                var candidate = System.IO.Path.Combine(d, "MpCmdRun.exe");
                if (File.Exists(candidate)) return candidate;
            }
        }
        return null;
    }
}
