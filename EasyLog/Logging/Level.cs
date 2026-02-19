namespace EasyLog.Logging
{
	/// <summary>
	/// Specifies the severity level of a log message.
	/// </summary>
	/// <remarks>Use the Level enumeration to indicate the importance or urgency of a log entry. Higher levels, such
	/// as Error and Fatal, typically represent more severe issues that may require immediate attention, while Info and
	/// Debug are used for informational or diagnostic messages.</remarks>
	public enum Level
	{
		Info,
		Debug,
		Warning,
		Error,
		Fatal
	}
}