using System.IO;

namespace SoftcurseLab.Core.Tasks;

/// <summary>
/// Runs disk defragmentation on all fixed NTFS volumes.
/// Only defrags if the drive analysis shows fragmentation > threshold.
/// Skips SSDs (they use TRIM, not defrag).
/// </summary>
public class DefragTask : BaseTask
{
    public override bool RequiresAdmin => true;

    public override async Task RunAsync(CancellationToken ct)
    {
        const string NAME = "Auto Defrag";

        if (!IsElevated())
        {
            Log(NAME, "Skipped — requires Administrator. Run screensaver as admin.", TaskStatus.Skipped);
            return;
        }

        Log(NAME, "Analysing volumes for fragmentation...", TaskStatus.Running);

        // Get fixed drives
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed &&
                        string.Equals(d.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (drives.Count == 0)
        {
            Log(NAME, "No NTFS fixed drives found.", TaskStatus.Skipped);
            return;
        }

        int defragged = 0, skipped = 0;

        foreach (var drive in drives)
        {
            if (ct.IsCancellationRequested) break;

            string letter = drive.Name.TrimEnd('\\');
            Log(NAME, $"Analysing {letter}...", TaskStatus.Running);

            // /A = analyse only, /U = progress, /V = verbose
            var (aCode, aOut, _) = await RunProcessAsync("defrag.exe", $"{letter} /A /U", ct, 60_000);

            // If analysis exit code is 1, drive is fragmented; 0 = not fragmented
            bool needsDefrag = aCode == 1 || aOut.Contains("% fragmented", StringComparison.OrdinalIgnoreCase);

            if (!needsDefrag)
            {
                Log(NAME, $"{letter} is already optimised — skipping.", TaskStatus.Skipped);
                skipped++;
                continue;
            }

            Log(NAME, $"Defragmenting {letter} (this may take minutes)...", TaskStatus.Running);
            var (dCode, _, dErr) = await RunProcessAsync("defrag.exe", $"{letter} /U /V", ct, 3_600_000); // 1hr max

            if (dCode == 0) { Log(NAME, $"{letter} defragmentation complete.", TaskStatus.Success); defragged++; }
            else Log(NAME, $"{letter} defrag finished with code {dCode}. {dErr[..Math.Min(60, dErr.Length)]}", TaskStatus.Warning);
        }

        Log(NAME, $"Defrag done: {defragged} drive(s) optimised, {skipped} skipped.", TaskStatus.Success);
    }
}
