using EasySave.Models;
using EasySave.Strategies.Implementations;
using EasySave.Strategies.Interfaces;
using EasySave.Interfaces;

namespace EasySave.Strategies
{
    /// <summary>
    /// Factory for creating backup strategies (Factory Pattern)
    /// </summary>
    public class BackupStrategyFactory
    {
        private readonly ILogger _logger;

        public BackupStrategyFactory(ILogger logger)
        {
            _logger = logger;
        }

        public IBackupStrategy CreateStrategy(BackupType type)
        {
            return type switch
            {
                BackupType.Complete => new CompleteBackupStrategy(_logger),
                BackupType.Differential => new DifferentialBackupStrategy(_logger),
                _ => throw new ArgumentException($"Unknown backup type: {type}")
            };
        }
    }
}
