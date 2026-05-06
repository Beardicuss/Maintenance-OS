using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;
using SoftcurseLab.Core;

namespace SoftcurseLab.UI;

public partial class ScreensaverWindow : Window
{
    // ── Win32 for /p HWND child-window embedding ──────────────────────────
    private const int GWL_STYLE = -16;
    private const int WS_CHILD  = 0x40000000;
    private const int WS_POPUP  = unchecked((int)0x80000000);

    [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr hWnd, IntPtr hParent);
    [DllImport("user32.dll")] static extern int    SetWindowLong(IntPtr hWnd, int idx, int val);
    [DllImport("user32.dll")] static extern int    GetWindowLong(IntPtr hWnd, int idx);
    [DllImport("user32.dll")] static extern bool   GetClientRect(IntPtr hWnd, out RECT r);
    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int L, T, R, B; }

    // ── State ─────────────────────────────────────────────────────────────
    private TaskEngine? _engine;
    private UIBridge?   _bridge;
    private readonly IntPtr _previewHwnd;
    private readonly bool   _isPreviewPane;

    // Input arm: ignore ALL mouse/key input for 2 sec so the click that
    // opened the screensaver doesn't instantly close it again
    private bool _inputArmed;
    private readonly System.Windows.Threading.DispatcherTimer _armTimer;
    private Point _firstMouse;
    private bool  _mouseInit;

    // Crash log path — written any time we catch an exception
    private static readonly string LogPath =
        Path.Combine(Path.GetTempPath(), "SoftcurseLab_crash.txt");

    // ── Constructor ───────────────────────────────────────────────────────
    public ScreensaverWindow(IntPtr previewHwnd)
    {
        _previewHwnd   = previewHwnd;
        _isPreviewPane = previewHwnd != IntPtr.Zero;

        InitializeComponent();

        if (!_isPreviewPane)
        {
            WindowStyle   = WindowStyle.None;
            WindowState   = WindowState.Maximized;
            Topmost       = true;
            ResizeMode    = ResizeMode.NoResize;
            ShowInTaskbar = false;
        }
        else
        {
            WindowStyle   = WindowStyle.None;
            ResizeMode    = ResizeMode.NoResize;
            ShowInTaskbar = false;
            if (GetClientRect(previewHwnd, out RECT r))
            { Width = Math.Max(1, r.R - r.L); Height = Math.Max(1, r.B - r.T); }
            else { Width = 152; Height = 112; }
        }

        // Arm timer — block all input until it fires
        _armTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_isPreviewPane ? 600 : 2500)
        };
        _armTimer.Tick += (_, _) =>
        {
            _armTimer.Stop();
            _inputArmed = true;
            _mouseInit  = false;  // re-baseline after cursor has settled
        };
        _armTimer.Start();
    }

    // ── Loaded ────────────────────────────────────────────────────────────
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Cursor = Cursors.None;
        if (_isPreviewPane) EmbedInParent();

        try
        {
            await InitWebViewAsync();
        }
        catch (Exception ex)
        {
            Log($"Window_Loaded exception: {ex}");
            ShowFallbackError($"Startup error:\n{ex.GetType().Name}: {ex.Message}");
        }
    }

    // ── Win32 child-window embed for /p preview pane ──────────────────────
    private void EmbedInParent()
    {
        try
        {
            var helper = new WindowInteropHelper(this);
            IntPtr myHwnd = helper.Handle;
            if (myHwnd == IntPtr.Zero) return;

            int style = GetWindowLong(myHwnd, GWL_STYLE);
            style = (style & ~WS_POPUP) | WS_CHILD;
            SetWindowLong(myHwnd, GWL_STYLE, style);
            SetParent(myHwnd, _previewHwnd);
            Left = 0; Top = 0;
        }
        catch (Exception ex) { Log($"EmbedInParent: {ex.Message}"); }
    }

    // ── WebView2 init ─────────────────────────────────────────────────────
    private async Task InitWebViewAsync()
    {
        string dataDir = Path.Combine(
            Path.GetTempPath(), "SoftcurseLab",
            _isPreviewPane ? "wv2preview" : "wv2data");
        Directory.CreateDirectory(dataDir);

        try
        {
            // Disable GPU acceleration — WebView2 GPU subprocess crashes silently
            // in screensaver context (no interactive desktop GPU session).
            // Software rendering via SwiftShader is ~identical for 2D/CSS UI.
            var opts = new CoreWebView2EnvironmentOptions(
                "--disable-gpu " +
                "--disable-gpu-compositing " +
                "--disable-software-rasterizer " +
                "--use-gl=swiftshader " +
                "--disable-features=VideoPlaybackQuality");

            Log("Creating CoreWebView2Environment (software rendering)...");
            var env = await CoreWebView2Environment.CreateAsync(null, dataDir, opts);
            Log("EnsureCoreWebView2Async...");
            await WebView.EnsureCoreWebView2Async(env);
            Log("WebView2 init OK.");
        }
        catch (Exception ex)
        {
            Log($"WebView2 init failed: {ex}");
            ShowFallbackError(
                "WebView2 Runtime required.\n\n" +
                "Download: https://go.microsoft.com/fwlink/p/?LinkId=2124703\n\n" +
                $"Error: {ex.Message}");
            return;
        }

        var s = WebView.CoreWebView2.Settings;
        s.AreDefaultContextMenusEnabled  = false;
        s.AreDevToolsEnabled             = false;
        s.IsStatusBarEnabled             = false;
        s.IsSwipeNavigationEnabled       = false;
        s.IsZoomControlEnabled           = false;
        s.AreDefaultScriptDialogsEnabled = false;
        Log("Settings applied.");

        // IMPORTANT: allow about:blank (WebView2 internal init) + our file://
        WebView.CoreWebView2.NavigationStarting += (_, args) =>
        {
            var uri = args.Uri ?? "";
            Log($"NavigationStarting: {uri}");
            bool allowed = uri.Length == 0
                        || uri.Equals("about:blank", StringComparison.OrdinalIgnoreCase)
                        || uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase);
            if (!allowed)
                args.Cancel = true;
        };

        WebView.CoreWebView2.NavigationCompleted += (_, args) =>
        {
            Log($"NavigationCompleted: success={args.IsSuccess} status={args.WebErrorStatus}");
        };

        WebView.CoreWebView2.ProcessFailed += (_, args) =>
        {
            Log($"WebView2 PROCESS FAILED: kind={args.ProcessFailedKind} reason={args.Reason}");
        };

        // Only wire engine in full-screen mode
        if (!_isPreviewPane)
        {
            _engine = new TaskEngine();
            _bridge = new UIBridge(WebView, Dispatcher, _engine.Stats);
            _engine.OnLog += entry => _bridge.PostLog(entry);
        }

        WebView.CoreWebView2.DOMContentLoaded += OnDomContentLoaded;

        // Find CyberUI.html — check multiple locations
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string? htmlPath = new[]
        {
            Path.Combine(baseDir, "CyberUI.html"),
            Path.Combine(baseDir, "UI", "CyberUI.html"),
            Path.Combine(Path.GetDirectoryName(baseDir.TrimEnd('\\','/'))??"", "CyberUI.html"),
        }.FirstOrDefault(File.Exists);

        if (htmlPath == null)
        {
            string msg = $"CyberUI.html not found.\nBase: {baseDir}";
            Log(msg);
            ShowFallbackError(msg);
            return;
        }

        Log($"Loading: {htmlPath}");
        WebView.Source = new Uri(Path.GetFullPath(htmlPath));
    }

    // ── DOM ready → start tasks ───────────────────────────────────────────
    private void OnDomContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
    {
        Log($"DOMContentLoaded fired. isPreview={_isPreviewPane} bridge={_bridge != null}");
        if (_isPreviewPane || _bridge == null || _engine == null) return;
        try
        {
            _bridge.SetReady(true);
            _bridge.PushVitals();
            _bridge.SendSystemInfo(IsElevated(), fwPaths: 0);
            FireAndForget(_engine.RunAllAsync());
            Log("Engine started.");

            var statsTimer = new System.Windows.Threading.DispatcherTimer
                { Interval = TimeSpan.FromSeconds(4) };
            statsTimer.Tick += (_, _) => _bridge?.PushStats();
            statsTimer.Start();
        }
        catch (Exception ex) { Log($"OnDomContentLoaded: {ex}"); }
    }

    // ── Fallback UI when WebView2 unavailable ─────────────────────────────
    private void ShowFallbackError(string message)
    {
        Dispatcher.Invoke(() =>
        {
            var tb = new System.Windows.Controls.TextBlock
            {
                Text         = message,
                Foreground   = new System.Windows.Media.SolidColorBrush(
                                   System.Windows.Media.Color.FromRgb(0, 255, 200)),
                FontFamily   = new System.Windows.Media.FontFamily("Consolas"),
                FontSize     = _isPreviewPane ? 7 : 14,
                Margin       = new Thickness(_isPreviewPane ? 4 : 32),
                TextWrapping = System.Windows.TextWrapping.Wrap,
            };
            if (Content is System.Windows.Controls.Grid g)
            { g.Children.Clear(); g.Children.Add(tb); }
        });
    }

    // ── Input handlers ────────────────────────────────────────────────────
    private void Window_KeyDown(object s, KeyEventArgs e)
    {
        Log($"KeyDown: armed={_inputArmed} key={e.Key}");
        if (!_inputArmed) return;
        ExitScreensaver();
    }

    private void Window_MouseDown(object s, MouseButtonEventArgs e)
    {
        Log($"MouseDown: armed={_inputArmed}");
        if (!_inputArmed) return;
        ExitScreensaver();
    }

    private void Window_MouseMove(object s, MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        if (!_inputArmed)
        {
            Log($"MouseMove ignored (not armed): pos={pos.X:F0},{pos.Y:F0}");
            return;
        }
        if (!_mouseInit)
        {
            Log($"MouseMove baseline set: {pos.X:F0},{pos.Y:F0}");
            _firstMouse = pos; _mouseInit = true; return;
        }
        double dx = Math.Abs(pos.X - _firstMouse.X);
        double dy = Math.Abs(pos.Y - _firstMouse.Y);
        if (dx > 12 || dy > 12)
        {
            Log($"MouseMove EXIT: delta={dx:F0},{dy:F0}");
            ExitScreensaver();
        }
    }

    private void ExitScreensaver()
    {
        _armTimer.Stop();
        _engine?.Cancel();
        Application.Current.Shutdown();
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static bool IsElevated()
    {
        using var id = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static async void FireAndForget(Task task)
    {
        try   { await task.ConfigureAwait(false); }
        catch (Exception ex) { Log($"FireAndForget: {ex}"); }
    }

    internal static void Log(string msg)
    {
        try { File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n"); }
        catch { /* last-resort logging; never throw */ }
    }
}

// NOTE TO DEVELOPER: If the screensaver still flashes and closes,
// run check_webview2.bat from the build folder and paste the output
// to diagnose. Log is always at %TEMP%\SoftcurseLab_crash.txt
