using System.IO;
using System.Windows;
using System.Windows.Threading;
using SoftcurseLab.UI;

namespace SoftcurseLab;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Global exception guards — nothing should silently kill the process ──
        DispatcherUnhandledException += (_, ex) =>
        {
            ScreensaverWindow.Log($"DispatcherUnhandled: {ex.Exception}");
            ex.Handled = true;   // keep the window alive
        };
        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            ScreensaverWindow.Log($"AppDomainUnhandled: {ex.ExceptionObject}");
        };
        TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
            ScreensaverWindow.Log($"UnobservedTask: {ex.Exception}");
            ex.SetObserved();
        };

        // ── Log the raw command line so we can see exactly what Windows passed ──
        string rawArgs = string.Join(" | ", e.Args);
        ScreensaverWindow.Log($"Args: [{rawArgs}]  CmdLine: {Environment.CommandLine}");

        // ── Parse screensaver arguments ────────────────────────────────────────
        // Windows passes: /s  /p HWND  /p:HWND  /c  (or nothing for configure)
        string mode = e.Args.Length > 0 ? e.Args[0].Trim() : "/c";

        bool isShow    = mode.Equals("/s", StringComparison.OrdinalIgnoreCase) ||
                         mode.Equals("-s", StringComparison.OrdinalIgnoreCase);
        bool isPreview = mode.StartsWith("/p", StringComparison.OrdinalIgnoreCase) ||
                         mode.StartsWith("-p", StringComparison.OrdinalIgnoreCase);

        if (isShow)
        {
            ScreensaverWindow.Log("Mode: /s fullscreen");
            var win = new ScreensaverWindow(previewHwnd: IntPtr.Zero);
            win.Show();
            return;
        }

        if (isPreview)
        {
            IntPtr hwnd = IntPtr.Zero;

            // /p:12345  (colon form)
            string suffix = mode.Length > 2 ? mode[2..].TrimStart(':', ' ') : "";
            if (suffix.Length > 0 && long.TryParse(suffix, out long h1))
                hwnd = new IntPtr(h1);
            // /p 12345  (space-separated)
            else if (e.Args.Length > 1 && long.TryParse(e.Args[1].Trim(), out long h2))
                hwnd = new IntPtr(h2);

            ScreensaverWindow.Log($"Mode: /p  HWND={hwnd}");
            var win = new ScreensaverWindow(previewHwnd: hwnd);
            win.Show();
            return;
        }

        // /c or default — configuration dialog
        ScreensaverWindow.Log("Mode: /c configure");
        var cfg = new ConfigWindow();
        cfg.ShowDialog();
        Shutdown();
    }
}
