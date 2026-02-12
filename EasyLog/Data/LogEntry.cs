namespace EasyLog.Data
{
    /// <summary>
    /// Represents a data record for a single log entry.
    /// This record holds immutable data regarding a specific file transfer operation during a backup.
    /// </summary>
    /// <param name="Timestamp">The timestamp indicating when the operation occurred.</param>
    /// <param name="Name">The name of the backup job associated with this log entry.</param>
    /// <param name="Source">The full path of the source file.</param>
    /// <param name="Target">The full path of the target (destination) file.</param>
    /// <param name="Size">The size of the transferred file (in bytes).</param>
    /// <param name="ElapsedTime">The duration of the transfer operation (in milliseconds).</param>
    public record LogEntry(int Timestamp, string Name, string Source, string Target, long Size, long ElapsedTime) { }
}