using EasySave.Utils;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace EasySave.View.Localization
{
	/// <summary>
	/// Provides internationalization (i18n) support by managing available languages and retrieving localized strings for
	/// the application.
	/// </summary>
	/// <remarks>This class implements a singleton pattern to ensure a single instance manages language resources
	/// throughout the application. It allows switching between supported languages at runtime and retrieving localized
	/// strings based on the current language. The class loads language resources embedded in the assembly and exposes
	/// methods to access translations and language metadata.</remarks>
	public partial class I18n : INotifyPropertyChanged
	{
		private readonly Dictionary<string, string> availableLanguages;

		private Dictionary<string, string> translations;

		private Dictionary<string, Dictionary<string, string>> properties;

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public string this[string key] => GetString(key);

		public string Language { get; private set; } = string.Empty;

		private static I18n? _instance;

		/// <summary>
		/// Static singleton getter of I18n class.
		/// </summary>
		/// <returns>
		/// An instance of I18n class.
		/// </returns>
		public static I18n Instance
		{

			get
			{
				_instance ??= new I18n();
				return _instance;
			}

		}

		/// <summary>
		/// Initializes a new instance of the I18n class and loads available language resources from the executing assembly.
		/// </summary>
		/// <remarks>This constructor scans the assembly for embedded language resources and prepares the internal
		/// language mapping. The default language is set to English ("en_us") upon initialization. This constructor is
		/// intended for internal use and is not accessible outside the class.</remarks>
		private I18n() {
			availableLanguages = [];
			translations       = [];
			properties         = [];
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

		/// <summary>
		/// Sets the current language for translations using the specified language name.
		/// </summary>
		/// <remarks>Calling this method updates the active translations to those associated with the specified
		/// language. Any subsequent translation lookups will use the newly set language.</remarks>
		/// <param name="languageName">The name of the language to set as the current language. Must correspond to an available language.</param>
		/// <exception cref="ArgumentException">Thrown if the specified language name does not exist in the available languages.</exception>
		public void SetLanguage(string languageName)
		{
			if (!availableLanguages.TryGetValue(languageName, out string? value))
				throw new ArgumentException("This language does not exists!");
			Language = languageName;
			string jsonContent = ResourceManager.ReadResourceFile(value);
			translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? [];

			// Notifier que toute l'instance a changé
			OnPropertyChanged(string.Empty); // Notifie TOUTES les propriétés
		}

		/// <summary>
		/// Retrieves the localized string associated with the specified key.
		/// </summary>
		/// <param name="key">The key that identifies the localized string to retrieve. Cannot be null.</param>
		/// <returns>The localized string corresponding to the specified key if found; otherwise, the key itself.</returns>
		public string GetString(string key)
		{
			if (translations.TryGetValue(key, out string? value))
				return value;
			return key;
		}

		/// <summary>
		/// Loads the language property dictionaries for all available languages.
		/// </summary>
		/// <remarks>Use this method to retrieve all language-specific properties that are marked with a leading '@'
		/// in their keys. The returned structure allows access to these properties by language.</remarks>
		/// <returns>A dictionary where each key is a language identifier and each value is a dictionary containing the language's
		/// properties. Only properties with keys that start with '@' are included.</returns>
		public Dictionary<string, Dictionary<string, string>> LoadLanguagesProperties()
		{
			if (properties.Count == 0)
			{
				foreach (var pair in availableLanguages)
				{
					properties[pair.Key] = JsonConvert
						.DeserializeObject<Dictionary<string, string>>(ResourceManager.ReadResourceFile(pair.Value))
						?.Where(p => p.Key.StartsWith('@'))
						?.ToDictionary<string, string>() ?? [];
				}
			}
			return properties;
		}

		[GeneratedRegex(@".*(\w{2}_\w{2}\.json?)")]
		private static partial Regex LocaleRegex();
	}
}