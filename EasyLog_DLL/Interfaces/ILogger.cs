namespace EasyLog_DLL.Interfaces
{
    /// <summary>
    /// Interface for logging implementations
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a transfer operation
        /// </summary>
        void LogTransfer(string backupName, string sourceFile, string targetFile, long fileSize, long transferTime);

        /// <summary>
        /// Logs an error during backup
        /// </summary>
        void LogError(string backupName, string message, string sourceFile = "");
    }
}
