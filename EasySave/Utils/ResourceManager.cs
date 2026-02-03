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
			string validatedResourceName = "EasySave." + resourceName;
			using Stream? stream = assembly.GetManifestResourceStream(validatedResourceName) ?? throw new FileNotFoundException($"Unavailable resource: {validatedResourceName}");
            using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}
	}
}
