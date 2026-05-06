namespace SoftcurseLab.Core;

/// <summary>
/// Thread-safe accumulator for stats reported by maintenance tasks.
/// UIBridge polls this periodically and pushes stat_update events to the JS UI.
/// </summary>
public class MaintenanceStats
{
    private long   _filesScanned;
    private long   _bytesFreed;
    private int    _threatsKilled;
    private int    _fwEvents;
    private int    _fwAlerts;
    private int    _fwPaths;
    private double _integrity = -1; // -1 = not yet measured
    private int    _unsignedDrivers;
    private int    _networkAnomalies;

    // ── Mutators (called from task threads) ───────────────────────────────
    public void AddFilesScanned(long n)    => Interlocked.Add(ref _filesScanned, n);
    public void AddBytesFreed(long n)      => Interlocked.Add(ref _bytesFreed, n);
    public void IncrementThreats()         => Interlocked.Increment(ref _threatsKilled);
    public void AddFwEvent(bool isSuspect) {
        Interlocked.Increment(ref _fwEvents);
        if (isSuspect) Interlocked.Increment(ref _fwAlerts);
    }
    public void SetFwPaths(int n)          => Volatile.Write(ref _fwPaths, n);
    public void SetIntegrity(double score) => Volatile.Write(ref _integrity, score);
    public void IncrementUnsignedDrivers() => Interlocked.Increment(ref _unsignedDrivers);
    public void IncrementNetworkAnomalies()=> Interlocked.Increment(ref _networkAnomalies);

    // ── Snapshot (read from UI thread) ────────────────────────────────────
    public StatsSnapshot Snapshot() => new()
    {
        FilesScanned     = Interlocked.Read(ref _filesScanned),
        BytesFreed       = Interlocked.Read(ref _bytesFreed),
        ThreatsKilled    = Volatile.Read(ref _threatsKilled),
        FwEvents         = Volatile.Read(ref _fwEvents),
        FwAlerts         = Volatile.Read(ref _fwAlerts),
        FwPaths          = Volatile.Read(ref _fwPaths),
        Integrity        = Volatile.Read(ref _integrity),
        UnsignedDrivers  = Volatile.Read(ref _unsignedDrivers),
        NetworkAnomalies = Volatile.Read(ref _networkAnomalies),
    };
}

public record StatsSnapshot
{
    public long   FilesScanned     { get; init; }
    public long   BytesFreed       { get; init; }
    public int    ThreatsKilled    { get; init; }
    public int    FwEvents         { get; init; }
    public int    FwAlerts         { get; init; }
    public int    FwPaths          { get; init; }
    public double Integrity        { get; init; }
    public int    UnsignedDrivers  { get; init; }
    public int    NetworkAnomalies { get; init; }
}
