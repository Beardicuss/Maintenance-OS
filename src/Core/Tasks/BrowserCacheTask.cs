using System.IO;

namespace SoftcurseLab.Core.Tasks;

/// <summary>Deletes Chromium (Chrome, Edge, Brave, Vivaldi) and Firefox cache folders to reclaim disk space.</summary>
public class BrowserCacheTask : BaseTask
{
    public override bool RequiresAdmin => false;

    private static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    private static readonly string[] ChromiumUserDataDirs =
    {
        Path.Combine(LocalAppData, @"Google\Chrome\User Data"),
        Path.Combine(LocalAppData, @"Microsoft\Edge\User Data"),
        Path.Combine(LocalAppData, @"BraveSoftware\Brave-Browser\User Data"),
        Path.Combine(LocalAppData, @"Vivaldi\User Data"),
    };

    private static readonly string[] FirefoxBaseDirs =
    {
        Path.Combine(LocalAppData, @"Mozilla\Firefox\Profiles"),
        Path.Combine(AppData, @"Mozilla\Firefox\Profiles"),
    };

    private static readonly string[] CacheSubDirs =
    {
        "Cache",
        "Code Cache",
        "GPUCache",
        Path.Combine("Service Worker", "CacheStorage")
    };

    public override async Task RunAsync(CancellationToken ct)
    {
        const string NAME = "Browser Cache Purge";
        Log(NAME, "Scanning Chrome/Edge/Brave/Vivaldi/Firefox caches...", TaskStatus.Running);
        await Task.Yield();

        long totalBytes = 0;
        int errors = 0;

        // Chromium browsers (scan all profiles: Default, Profile 1, Profile 2, etc.)
        foreach (var userData in ChromiumUserDataDirs)
        {
            if (ct.IsCancellationRequested) break;
            if (!Directory.Exists(userData)) continue;

            try
            {
                var profileDirs = Directory.GetDirectories(userData, "Default")
                    .Concat(Directory.GetDirectories(userData, "Profile *"));

                foreach (var profile in profileDirs)
                {
                    if (ct.IsCancellationRequested) break;
                    foreach (var sub in CacheSubDirs)
                    {
                        var target = Path.Combine(profile, sub);
                        totalBytes += DeleteDirectory(target, ref errors);
                    }
                }
            }
            catch { errors++; }
        }

        // Firefox — find profile cache2 folders
        foreach (var baseDir in FirefoxBaseDirs)
        {
            if (!Directory.Exists(baseDir)) continue;
            try
            {
                foreach (var profile in Directory.GetDirectories(baseDir))
                {
                    if (ct.IsCancellationRequested) break;
                    var cache = Path.Combine(profile, "cache2");
                    totalBytes += DeleteDirectory(cache, ref errors);
                }
            }
            catch { errors++; }
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
