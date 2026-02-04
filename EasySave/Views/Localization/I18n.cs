using EasySave.Utils;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.RegularExpressions;

namespace EasySave.Views.Localization
{
	internal partial class I18n
	{
		private readonly Dictionary<string, string> availableLanguages;

		private Dictionary<string, string> translations;

		public string Language { get; private set; } = string.Empty;

		public I18n() {
			availableLanguages = [];
			translations = [];
			var langs = Assembly.GetExecutingAssembly()
				.GetManifestResourceNames()
				.Where(e => e.Contains(".i18n."));
			foreach (var lang in langs)
			{
				Match match = LocaleRegex().Match(lang);
				if (match.Success)
				{
					string localeName = match.Groups[1].Value.Split('.')[0];  // 1 = premier groupe de capture
					availableLanguages[localeName] = lang;
				}
			}
			SetLanguage("en_us");
		}

		public void SetLanguage(string languageName)
		{
			if (!availableLanguages.ContainsKey(languageName))
				throw new ArgumentException("This language does not exists!");
			Language = languageName;
			string jsonContent = ResourceManager.ReadResourceFile(availableLanguages[languageName]);
			translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
		}

		public string GetString(string key)
		{
			if (translations.TryGetValue(key, out string? value))
				return value;
			return key;
		}

		public Dictionary<string, Dictionary<string, string>> LoadLanguagesProperties()
		{
			var properties = new Dictionary<string, Dictionary<string, string>>();
			foreach (var pair in availableLanguages)
			{
				properties[pair.Key] = JsonConvert
					.DeserializeObject<Dictionary<string, string>>(ResourceManager.ReadResourceFile(pair.Value))
					.Where(p => p.Key.StartsWith("@"))
					.ToDictionary<string, string>();
			}
			return properties;
		}

		[GeneratedRegex(@".*(\w{2}_\w{2}\.json?)")]
        private static partial Regex LocaleRegex();
    }
}
