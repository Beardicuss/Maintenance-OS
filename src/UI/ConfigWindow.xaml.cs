using System.IO;
using System.Windows;

namespace SoftcurseLab.UI;

public partial class ConfigWindow : Window
{
    public static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SoftcurseLab");
    public static readonly string ConfigPath = Path.Combine(ConfigDir, "config.ini");

    public ConfigWindow()
    {
        InitializeComponent();
        LoadConfig();
    }

    private void LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(ConfigPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith(';')) continue;
                int idx = line.IndexOf('=');
                if (idx > 0)
                {
                    string k = line[..idx].Trim();
                    string v = line[(idx + 1)..].Trim();
                    dict[k] = v;
                }
            }

            bool Get(string key) => !dict.TryGetValue(key, out var v) || !string.Equals(v, "False", StringComparison.OrdinalIgnoreCase);

            CbDefender.IsChecked     = Get("Defender");
            CbBrowser.IsChecked      = Get("BrowserCache");
            CbIntegrity.IsChecked    = Get("FileIntegrity");
            CbProcessGuard.IsChecked = Get("ProcessGuard");
            CbDefrag.IsChecked       = Get("Defrag");
            CbCleanup.IsChecked      = Get("DiskCleanup");
            CbDism.IsChecked         = Get("DismCleanup");
            CbDrivers.IsChecked      = Get("DriverHealth");
        }
        catch (Exception ex)
        {
            ScreensaverWindow.Log($"LoadConfig exception: {ex.Message}");
        }
    }

    private void SaveConfig()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var lines = new[]
            {
                $"Defender={CbDefender.IsChecked == true}",
                $"BrowserCache={CbBrowser.IsChecked == true}",
                $"FileIntegrity={CbIntegrity.IsChecked == true}",
                $"ProcessGuard={CbProcessGuard.IsChecked == true}",
                $"Defrag={CbDefrag.IsChecked == true}",
                $"DiskCleanup={CbCleanup.IsChecked == true}",
                $"DismCleanup={CbDism.IsChecked == true}",
                $"DriverHealth={CbDrivers.IsChecked == true}",
            };
            File.WriteAllLines(ConfigPath, lines);
        }
        catch (Exception ex)
        {
            ScreensaverWindow.Log($"SaveConfig exception: {ex.Message}");
        }
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
