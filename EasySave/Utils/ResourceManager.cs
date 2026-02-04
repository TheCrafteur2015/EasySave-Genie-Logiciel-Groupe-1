using System.Reflection;

namespace EasySave.Utils
{
    internal class ResourceManager
    {
        private ResourceManager() { }

		// MonNamespace.Data.monfichier.txt
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
