using System.Diagnostics;
using System.Security.Principal;

namespace SoftcurseLab.Core.Tasks;

public abstract class BaseTask : IMaintenanceTask
{
    public abstract bool RequiresAdmin { get; }
    public event Action<TaskLogEntry>? OnLog;

    /// <summary>Shared stats accumulator injected by TaskEngine.</summary>
    public MaintenanceStats? Stats { get; set; }

    protected void Log(string taskName, string msg, TaskStatus status)
    {
        OnLog?.Invoke(new TaskLogEntry
        {
            TaskName = taskName,
            Message = msg,
            Status = status
        });
    }

    protected static bool IsElevated()
    {
        using var id = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }

    protected static async Task<(int ExitCode, string Output, string Error)> RunProcessAsync(
        string exe, string args, CancellationToken ct, int timeoutMs = 120_000)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
            WindowStyle            = ProcessWindowStyle.Hidden
        };

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdout = new System.Text.StringBuilder();
        var stderr = new System.Text.StringBuilder();
        proc.OutputDataReceived += (_, d) => { if (d.Data != null) stdout.AppendLine(d.Data); };
        proc.ErrorDataReceived  += (_, d) => { if (d.Data != null) stderr.AppendLine(d.Data); };

        try
        {
            if (!proc.Start())
                return (-1, string.Empty, $"Failed to start process '{exe}'.");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return (-1, string.Empty, $"Win32 error starting '{exe}': {ex.Message}");
        }
        catch (Exception ex)
        {
            return (-1, string.Empty, $"Error starting '{exe}': {ex.Message}");
        }

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeoutMs);
        try
        {
            await proc.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { proc.Kill(true); } catch { }
            return (-1, stdout.ToString().Trim(), "Process execution timed out or was cancelled.");
        }
        catch (Exception ex)
        {
            return (-1, stdout.ToString().Trim(), $"Process execution error: {ex.Message}");
        }

        return (proc.ExitCode, stdout.ToString().Trim(), stderr.ToString().Trim());
    }

    public abstract Task RunAsync(CancellationToken ct);
}
