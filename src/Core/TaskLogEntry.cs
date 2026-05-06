using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoftcurseLab.Core;

public enum TaskStatus { Idle, Running, Success, Warning, Error, Skipped }

public class TaskLogEntry : INotifyPropertyChanged
{
    private string _message = string.Empty;
    private TaskStatus _status = TaskStatus.Idle;

    public string TaskName { get; init; } = string.Empty;
    public string Timestamp { get; } = DateTime.Now.ToString("HH:mm:ss");

    public string Message
    {
        get => _message;
        set { _message = value; OnPropertyChanged(); }
    }

    public TaskStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusIcon)); OnPropertyChanged(nameof(StatusColor)); }
    }

    public string StatusIcon => Status switch
    {
        TaskStatus.Running => "⟳",
        TaskStatus.Success => "✓",
        TaskStatus.Warning => "⚠",
        TaskStatus.Error   => "✗",
        TaskStatus.Skipped => "—",
        _                  => "◌"
    };

    public string StatusColor => Status switch
    {
        TaskStatus.Running => "#00ccff",
        TaskStatus.Success => "#00ff88",
        TaskStatus.Warning => "#ffcc00",
        TaskStatus.Error   => "#ff4466",
        TaskStatus.Skipped => "#557799",
        _                  => "#335566"
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
