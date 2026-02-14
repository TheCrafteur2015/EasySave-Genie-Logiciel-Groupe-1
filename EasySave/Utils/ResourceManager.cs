using System.Reflection;

namespace EasySave.Utils
{
	/// <summary>
	/// Provides methods for reading embedded resource files from the assembly.
	/// </summary>
    public class ResourceManager
    {
		/// <summary>
		/// Initializes a new instance of the ResourceManager class.
		/// </summary>
        private ResourceManager() { }

		/// <summary>
		/// Reads the contents of an embedded resource file from the executing assembly.
		/// </summary>
		/// <remarks>Resource names are case-sensitive and must match the names used when embedding the resources in
		/// the assembly. This method is typically used to access files such as configuration data or templates that are
		/// included as embedded resources.</remarks>
		/// <param name="resourceName">The name of the embedded resource to read. If the name does not begin with "EasySave.", the prefix is
		/// automatically added.</param>
		/// <returns>A string containing the full contents of the specified embedded resource file.</returns>
		/// <exception cref="FileNotFoundException">Thrown if the specified resource cannot be found in the executing assembly.</exception>
		public static string ReadResourceFile(string resourceName)
        {
			var assembly = Assembly.GetExecutingAssembly();
			if (!resourceName.StartsWith("EasySave."))
				resourceName = "EasySave." + resourceName;
			using Stream? stream = assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"Unavailable resource: {resourceName}");
            using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}
	}
}
