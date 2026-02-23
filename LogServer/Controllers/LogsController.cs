using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using LogServer.Models;

namespace LogServer.Controllers
{
	/// <summary>
	/// API Controller for receiving and centralizing log entries from multiple EasySave clients.
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	public class LogsController : ControllerBase
	{
		private readonly string _logDirectory;
		private static readonly object _fileLock = new();
		private readonly ILogger<LogsController> _logger;

		public LogsController(ILogger<LogsController> logger, IConfiguration configuration)
		{
			_logger = logger;
			_logDirectory = configuration["LogDirectory"] ?? "/app/logs";
			
			if (!Directory.Exists(_logDirectory))
				Directory.CreateDirectory(_logDirectory);
		}

		/// <summary>
		/// Receives a log entry from a remote client and appends it to the daily centralized log file.
		/// </summary>
		/// <remarks>
		/// All logs from all machines are stored in a single daily file (e.g., 2025-02-19.json).
		/// The MachineName and UserName properties allow differentiation between clients.
		/// </remarks>
		/// <param name="entry">The log entry to store.</param>
		/// <returns>200 OK if successful, 500 if an error occurred.</returns>
		[HttpPost]
		public IActionResult PostLog([FromBody] LogEntry entry)
		{
			try
			{
				string dateFile = DateTime.Now.ToString("yyyy-MM-dd");
				string logFile = Path.Combine(_logDirectory, $"{dateFile}.json");

				lock (_fileLock)
				{
					List<LogEntry> logs = new();
					
					if (System.IO.File.Exists(logFile))
					{
						string existingContent = System.IO.File.ReadAllText(logFile);
						if (!string.IsNullOrWhiteSpace(existingContent))
						{
							logs = JsonSerializer.Deserialize<List<LogEntry>>(existingContent) ?? new();
						}
					}

					logs.Add(entry);

					var options = new JsonSerializerOptions { WriteIndented = true };
					System.IO.File.WriteAllText(logFile, JsonSerializer.Serialize(logs, options));
				}

				_logger.LogInformation($"Log received from {entry.MachineName}\\{entry.UserName}");
				return Ok(new { success = true, message = "Log entry stored successfully" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to store log entry");
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		/// <summary>
		/// Health check endpoint to verify server availability.
		/// </summary>
		[HttpGet("health")]
		public IActionResult HealthCheck()
		{
			return Ok(new { status = "healthy", timestamp = DateTime.Now });
		}

		/// <summary>
		/// Returns the list of available daily log files.
		/// </summary>
		[HttpGet("files")]
		public IActionResult GetLogFiles()
		{
			try
			{
				var files = Directory.GetFiles(_logDirectory, "*.json")
					.Select(Path.GetFileName)
					.OrderByDescending(f => f)
					.ToList();

				return Ok(files);
			}
			catch (Exception e)
			{
				return StatusCode(500, new { success = false, message = e.Message });
			}
		}
	}
}
