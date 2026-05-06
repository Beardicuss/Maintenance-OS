using System.IO;

namespace SoftcurseLab.Core.Tasks;

/// <summary>Deletes Chrome and Firefox cache folders to reclaim disk space.</summary>
public class BrowserCacheTask : BaseTask
{
    public override bool RequiresAdmin => false;

    private static readonly string[] _chromePaths =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Google\Chrome\User Data\Default\Cache"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Google\Chrome\User Data\Default\Code Cache"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Google\Chrome\User Data\Default\GPUCache"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Microsoft\Edge\User Data\Default\Cache"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Microsoft\Edge\User Data\Default\Code Cache"),
    };

    private static readonly string[] _firefoxBaseDirs =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Mozilla\Firefox\Profiles"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Mozilla\Firefox\Profiles"),
    };

    public override async Task RunAsync(CancellationToken ct)
    {
        const string NAME = "Browser Cache Purge";
        Log(NAME, "Scanning Chrome/Edge/Firefox caches...", TaskStatus.Running);
        await Task.Yield();

        long totalBytes = 0;
        int errors = 0;

        // Chrome / Edge
        foreach (var dir in _chromePaths)
        {
            if (ct.IsCancellationRequested) break;
            totalBytes += DeleteDirectory(dir, ref errors);
        }

        // Firefox — find profile cache2 folders
        foreach (var baseDir in _firefoxBaseDirs)
        {
            if (!Directory.Exists(baseDir)) continue;
            foreach (var profile in Directory.GetDirectories(baseDir))
            {
                if (ct.IsCancellationRequested) break;
                var cache = Path.Combine(profile, "cache2");
                totalBytes += DeleteDirectory(cache, ref errors);
            }
        }

        double mb = totalBytes / 1_048_576.0;
        Stats?.AddBytesFreed(totalBytes);
        if (mb > 0.1)
            Log(NAME, $"Freed {mb:F1} MB of browser cache.{(errors > 0 ? $" ({errors} locked files skipped)" : "")}", TaskStatus.Success);
        else
            Log(NAME, "No browser cache found or already clean.", TaskStatus.Skipped);
    }

    private static long DeleteDirectory(string path, ref int errors)
    {
        if (!Directory.Exists(path)) return 0;
        long size = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var fi = new FileInfo(file);
                    size += fi.Length;
                    fi.Delete();
                }
                catch { errors++; }
            }
            // Try to clean empty subdirs
            foreach (var sub in Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly))
                try { Directory.Delete(sub, true); } catch { }
        }
        catch { errors++; }
        return size;
    }
}
