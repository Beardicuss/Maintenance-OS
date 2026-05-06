using System.IO;
using System.Windows;

namespace SoftcurseLab.UI;

public partial class ConfigWindow : Window
{
    private static readonly string ConfigDir  = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SoftcurseLab");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.ini");

    public ConfigWindow()
    {
        InitializeComponent();
        LoadConfig();
    }

    private void LoadConfig()
    {
        if (!File.Exists(ConfigPath)) return;
        var lines = File.ReadAllLines(ConfigPath)
                        .ToDictionary(
                            l => l.Split('=')[0].Trim(),
                            l => l.Contains('=') ? l.Split('=')[1].Trim() : "true",
                            StringComparer.OrdinalIgnoreCase);

        bool Get(string key) => !lines.TryGetValue(key, out var v) || v == "true";

        CbDefender.IsChecked     = Get("Defender");
        CbBrowser.IsChecked      = Get("BrowserCache");
        CbIntegrity.IsChecked    = Get("FileIntegrity");
        CbProcessGuard.IsChecked = Get("ProcessGuard");
        CbDefrag.IsChecked       = Get("Defrag");
        CbCleanup.IsChecked      = Get("DiskCleanup");
        CbDism.IsChecked         = Get("DismCleanup");
        CbDrivers.IsChecked      = Get("DriverHealth");
    }

    private void SaveConfig()
    {
        Directory.CreateDirectory(ConfigDir);
        var lines = new[]
        {
            $"Defender={CbDefender.IsChecked}",
            $"BrowserCache={CbBrowser.IsChecked}",
            $"FileIntegrity={CbIntegrity.IsChecked}",
            $"ProcessGuard={CbProcessGuard.IsChecked}",
            $"Defrag={CbDefrag.IsChecked}",
            $"DiskCleanup={CbCleanup.IsChecked}",
            $"DismCleanup={CbDism.IsChecked}",
            $"DriverHealth={CbDrivers.IsChecked}",
        };
        File.WriteAllLines(ConfigPath, lines);
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        SaveConfig();
        Close();
    }

    private void BtnPreview_Click(object sender, RoutedEventArgs e)
    {
        SaveConfig();
        var preview = new ScreensaverWindow(previewHwnd: IntPtr.Zero)
        {
            Width  = 960,
            Height = 600,
            WindowStyle = WindowStyle.SingleBorderWindow,
            WindowState = WindowState.Normal,
            Topmost = false,
            Title = "Softcurse LAB — Preview"
        };
        preview.Show();
    }
}
