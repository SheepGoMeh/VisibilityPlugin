using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Visibility
{
	public class Localization
	{
		private readonly Dictionary<Language, Dictionary<string, string>> _strings = new();
		public List<Language> AvailableLanguages { get; } = new();

		public Language CurrentLanguage;

		public Localization(Language language = Language.English)
		{
			LoadStrings(Language.English, "en");
			LoadStrings(Language.French, "fr");
			LoadStrings(Language.German, "de");
			LoadStrings(Language.Italian, "it");
			LoadStrings(Language.Portuguese, "pt-BR");
			LoadStrings(Language.Spanish, "es-ES");
			LoadStrings(Language.Swedish, "sv");
			LoadStrings(Language.Russian, "ru");
			LoadStrings(Language.Japanese, "ja");
			LoadStrings(Language.ChineseSimplified, "zh-CN");
			LoadStrings(Language.ChineseTraditional, "zh-TW");
			CurrentLanguage = AvailableLanguages.Contains(language) ? language : Language.English;
		}

		private void LoadStrings(Language language, string languageName)
		{
			string jsonString = language switch
			{
				Language.English => Properties.Resources.en_strings,
				Language.French => Properties.Resources.fr_strings,
				Language.German => Properties.Resources.de_strings,
				Language.Italian => Properties.Resources.it_strings,
				Language.Japanese => Properties.Resources.ja_strings,
				Language.Portuguese => Properties.Resources.pt_BR_strings,
                Language.Swedish => Properties.Resources.sv_strings,
				Language.Russian => Properties.Resources.ru_strings,
				Language.Spanish => Properties.Resources.es_ES_strings,
				Language.ChineseSimplified => Properties.Resources.zh_CN_strings,
				Language.ChineseTraditional => Properties.Resources.zh_TW_strings,
				_ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
			};

			AvailableLanguages.Add(language);
			_strings[language] = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString)!;
		}

		public string GetString(string key, Language language) => _strings[language].ContainsKey(key) ? _strings[language][key] : key;

		public enum Language
		{
			English,
			French,
			German,
			Italian,
			Japanese,
			Portuguese,
            Swedish,
			Russian,
			Spanish,
			ChineseSimplified,
			ChineseTraditional
		}

		public string PluginCommandHelpMessage => GetString("PluginCommandHelpMessage", CurrentLanguage);
		public string VoidPlayerHelpMessage => GetString("VoidPlayerHelpMessage", CurrentLanguage);
		public string VoidTargetPlayerHelpMessage => GetString("VoidTargetPlayerHelpMessage", CurrentLanguage);
		public string WhitelistPlayerHelpMessage => GetString("WhitelistPlayerHelpMessage", CurrentLanguage);
		public string WhitelistTargetPlayerHelpMessage => GetString("WhitelistTargetPlayerHelpMessage", CurrentLanguage);
		public string RefreshComplete => GetString("RefreshComplete", CurrentLanguage);
		public string PluginCommandHelpMenu1 => GetString("PluginCommandHelpMenu1", CurrentLanguage);
		public string PluginCommandHelpMenu2 => GetString("PluginCommandHelpMenu2", CurrentLanguage);
		public string PluginCommandHelpMenu3 => GetString("PluginCommandHelpMenu3", CurrentLanguage);
		public string PluginCommandHelpMenu4 => GetString("PluginCommandHelpMenu4", CurrentLanguage);
		public string PluginCommandHelpMenuError => GetString("PluginCommandHelpMenuError", CurrentLanguage);
		public string PluginCommandHelpMenuInvalidValueError(string value) => GetString("PluginCommandHelpMenuInvalidValueError", CurrentLanguage).Format(value);
		public string VoidListName => GetString("VoidListName", CurrentLanguage);
		public string WhitelistName => GetString("WhitelistName", CurrentLanguage);
		public string NoArgumentsError(string name) => GetString("NoArgumentsError", CurrentLanguage).Format(name);
		public string NotEnoughArgumentsError(string name) => GetString("NotEnoughArgumentsError", CurrentLanguage).Format(name);
		public string InvalidWorldNameError(string name, string worldName) => GetString("InvalidWorldNameError", CurrentLanguage).Format(name, worldName);
		public string EntryAdded(string name, string entryName) => GetString("EntryAdded", CurrentLanguage).Format(name, entryName);
		public string EntryExistsError(string name, string entryName) => GetString("EntryExistsError", CurrentLanguage).Format(name, entryName);
		public string InvalidTargetError(string name) => GetString("InvalidTargetError", CurrentLanguage).Format(name);
		public string ContextMenuAdd(string name) => GetString("ContextMenuAdd", CurrentLanguage).Format(name);
		public string ContextMenuRemove(string name) => GetString("ContextMenuRemove", CurrentLanguage).Format(name);
		public string OptionEnable => GetString("OptionEnable", CurrentLanguage);
		public string OptionHideAll => GetString("OptionHideAll", CurrentLanguage);
		public string OptionShowParty => GetString("OptionShowParty", CurrentLanguage);
		public string OptionShowFriends => GetString("OptionShowFriends", CurrentLanguage);
		public string OptionShowFc => GetString("OptionShowFC", CurrentLanguage);
		public string OptionShowDead => GetString("OptionShowDead", CurrentLanguage);
		public string OptionPlayers => GetString("OptionPlayers", CurrentLanguage);
		public string OptionPets => GetString("OptionPets", CurrentLanguage);
		public string OptionChocobos => GetString("OptionChocobos", CurrentLanguage);
		public string OptionMinions => GetString("OptionMinions", CurrentLanguage);
		public string OptionEarthlyStar => GetString("OptionEarthlyStar", CurrentLanguage);
		public string OptionEarthlyStarTip => GetString("OptionEarthlyStarTip", CurrentLanguage);
		public string OptionContextMenu => GetString("OptionContextMenu", CurrentLanguage);
		public string OptionContextMenuTip => GetString("OptionContextMenuTip", CurrentLanguage);
		public string OptionRefresh => GetString("OptionRefresh", CurrentLanguage);
		public string OptionAddPlayer => GetString("OptionAddPlayer", CurrentLanguage);
		public string OptionRemovePlayer => GetString("OptionRemovePlayer", CurrentLanguage);
		public string ColumnFirstname => GetString("ColumnFirstname", CurrentLanguage);
		public string ColumnLastname => GetString("ColumnLastname", CurrentLanguage);
		public string ColumnWorld => GetString("ColumnWorld", CurrentLanguage);
		public string ColumnDate => GetString("ColumnDate", CurrentLanguage);
		public string ColumnReason => GetString("ColumnReason", CurrentLanguage);
		public string ColumnAction => GetString("ColumnAction", CurrentLanguage);
		public string OptionLanguage => GetString("OptionLanguage", CurrentLanguage);
		public string LanguageName => GetString("LanguageName", CurrentLanguage);
	}
}