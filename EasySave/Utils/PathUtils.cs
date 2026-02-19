using System.Net;

namespace EasySave.Utils
{
	/// <summary>
	/// Provides utility methods for handling file paths.
	/// </summary>
	public static class PathUtils
	{
		/// <summary>
		/// Converts a local file path to a UNC (Universal Naming Convention) path.
		/// </summary>
		/// <remarks>
		/// This method converts a local path like "C:\Folder\File.txt" to a network share path 
		/// like "\\Hostname\C$\Folder\File.txt". If the path is already in UNC format or cannot be converted, 
		/// the original path is returned.
		/// </remarks>
		/// <param name="path">The local file path to convert.</param>
		/// <returns>The path in UNC format, or the original path if conversion is not applicable.</returns>
		public static string ToUnc(string? path)
		{
			if (string.IsNullOrEmpty(path)) return string.Empty;

			// If it is already a UNC path, return it as is
			if (path.StartsWith(@"\\")) return path;

			try
			{
				// Get the root of the path (e.g., "C:\")
				string? root = Path.GetPathRoot(path);

				// If no root is found or it's a relative path, return original
				if (string.IsNullOrEmpty(root)) return path;

				// Remove the trailing separator to get the drive (e.g., "C:")
				string drive = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

				// Replace the colon with a dollar sign for administrative share (e.g., "C$")
				if (drive.EndsWith(':'))
				{
					drive = drive[..^1] + "$";
				}
				else
				{
					// If it doesn't look like a drive letter, return original
					return path;
				}

				// Get the rest of the path without the root
				string relativePath = path[root.Length..];

				// Get the machine's host name
				string hostName = Dns.GetHostName();

				// Combine to form the UNC path: \\Hostname\Drive$\Path
				return Path.Combine($"\\\\{hostName}\\{drive}", relativePath);
			}
			catch
			{
				// In case of any parsing error, fallback to the original path
				return path;
			}
		}
	}
}