namespace EasyLog.Data
{
	public record LogEntry(int Timestamp, string Name, string Source, string Target, long Size, long ElapsedTime) {}
}
