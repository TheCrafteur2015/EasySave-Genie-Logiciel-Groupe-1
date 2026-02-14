namespace EasySave.Backup
{
    /// <summary>
    /// Represents control signals used to manage the application's flow and state.
    /// </summary>
    public enum Signal
    {
        /// <summary>
        /// No active signal or default state.
        /// </summary>
        None,

        /// <summary>
        /// Signal to continue normal execution or resume operations.
        /// </summary>
        Continue,

        /// <summary>
        /// Signal to terminate the application or stop the current process.
        /// </summary>
        Exit
    }
}