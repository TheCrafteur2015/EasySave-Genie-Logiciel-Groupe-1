using System.Net.Http.Json;
using EasyLog.Data;

namespace EasyLog.Logging
{
    /// <summary>
    /// Logger implementation that sends log entries to a remote centralized log server via HTTP.
    /// </summary>
    /// <remarks>
    /// This logger is designed to work with a Docker-hosted log centralization service.
    /// If the remote server is unavailable, log entries are silently discarded.
    /// </remarks>
    public class RemoteLogger : ILogger
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl;
        private static readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteLogger"/> class.
        /// </summary>
        /// <param name="serverUrl">The base URL of the remote log server (e.g., "http://localhost:5000").</param>
        public RemoteLogger(string serverUrl)
        {
            _serverUrl = serverUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(serverUrl));
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        /// <summary>
        /// Sends a log entry to the remote server asynchronously.
        /// </summary>
        /// <remarks>
        /// This method does not block the calling thread. If the request fails (network error, server down),
        /// the error is caught and silently ignored to prevent disrupting the application flow.
        /// </remarks>
        /// <param name="entry">The log entry to send to the remote server.</param>
        public void Log(LogEntry entry)
        {
            Task.Run(async () =>
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/logs", entry);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RemoteLogger] Failed to send log to {_serverUrl}: {ex.Message}");
                }
            });
        }
    }
}
