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
			this.LoadStrings(Language.English, "en");
			this.LoadStrings(Language.French, "fr");
			this.LoadStrings(Language.German, "de");
			this.LoadStrings(Language.Italian, "it");
			this.LoadStrings(Language.Portuguese, "pt-BR");
			this.LoadStrings(Language.Spanish, "es-ES");
			this.LoadStrings(Language.Russian, "ru");
			this.LoadStrings(Language.Japanese, "ja");
			this.LoadStrings(Language.ChineseSimplified, "zh-CN");
			this.LoadStrings(Language.ChineseTraditional, "zh-TW");
			this.CurrentLanguage = this.AvailableLanguages.Contains(language) ? language : Language.English;
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
				Language.Russian => Properties.Resources.ru_strings,
				Language.Spanish => Properties.Resources.es_ES_strings,
				Language.ChineseSimplified => Properties.Resources.zh_CN_strings,
				Language.ChineseTraditional => Properties.Resources.zh_TW_strings,
				_ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
			};

			this.AvailableLanguages.Add(language);
			this._strings[language] = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString)!;
		}

		public string GetString(string key, Language language) => this._strings[language].ContainsKey(key) ? this._strings[language][key] : key;

		public enum Language
		{
			English,
			French,
			German,
			Italian,
			Japanese,
			Portuguese,
			Russian,
			Spanish,
			ChineseSimplified,
			ChineseTraditional
		}

		public string PluginCommandHelpMessage => this.GetString("PluginCommandHelpMessage", this.CurrentLanguage);
		public string VoidPlayerHelpMessage => this.GetString("VoidPlayerHelpMessage", this.CurrentLanguage);
		public string VoidTargetPlayerHelpMessage => this.GetString("VoidTargetPlayerHelpMessage", this.CurrentLanguage);
		public string WhitelistPlayerHelpMessage => this.GetString("WhitelistPlayerHelpMessage", this.CurrentLanguage);
		public string WhitelistTargetPlayerHelpMessage => this.GetString("WhitelistTargetPlayerHelpMessage", this.CurrentLanguage);
		public string RefreshComplete => this.GetString("RefreshComplete", this.CurrentLanguage);
		public string PluginCommandHelpMenu1 => this.GetString("PluginCommandHelpMenu1", this.CurrentLanguage);
		public string PluginCommandHelpMenu2 => this.GetString("PluginCommandHelpMenu2", this.CurrentLanguage);
		public string PluginCommandHelpMenu3 => this.GetString("PluginCommandHelpMenu3", this.CurrentLanguage);
		public string PluginCommandHelpMenu4 => this.GetString("PluginCommandHelpMenu4", this.CurrentLanguage);
		public string PluginCommandHelpMenuError => this.GetString("PluginCommandHelpMenuError", this.CurrentLanguage);
		public string PluginCommandHelpMenuInvalidValueError(string value) => this.GetString("PluginCommandHelpMenuInvalidValueError", this.CurrentLanguage).Format(value);
		public string VoidListName => this.GetString("VoidListName", this.CurrentLanguage);
		public string WhitelistName => this.GetString("WhitelistName", this.CurrentLanguage);
		public string NoArgumentsError(string name) => this.GetString("NoArgumentsError", this.CurrentLanguage).Format(name);
		public string NotEnoughArgumentsError(string name) => this.GetString("NotEnoughArgumentsError", this.CurrentLanguage).Format(name);
		public string InvalidWorldNameError(string name, string worldName) => this.GetString("InvalidWorldNameError", this.CurrentLanguage).Format(name, worldName);
		public string EntryAdded(string name, string entryName) => this.GetString("EntryAdded", this.CurrentLanguage).Format(name, entryName);
		public string EntryExistsError(string name, string entryName) => this.GetString("EntryExistsError", this.CurrentLanguage).Format(name, entryName);
		public string InvalidTargetError(string name) => this.GetString("InvalidTargetError", this.CurrentLanguage).Format(name);
		public string ContextMenuAdd(string name) => this.GetString("ContextMenuAdd", this.CurrentLanguage).Format(name);
		public string ContextMenuRemove(string name) => this.GetString("ContextMenuRemove", this.CurrentLanguage).Format(name);
		public string OptionEnable => this.GetString("OptionEnable", this.CurrentLanguage);
		public string OptionHideAll => this.GetString("OptionHideAll", this.CurrentLanguage);
		public string OptionShowParty => this.GetString("OptionShowParty", this.CurrentLanguage);
		public string OptionShowFriends => this.GetString("OptionShowFriends", this.CurrentLanguage);
		public string OptionShowFc => this.GetString("OptionShowFC", this.CurrentLanguage);
		public string OptionShowDead => this.GetString("OptionShowDead", this.CurrentLanguage);
		public string OptionPlayers => this.GetString("OptionPlayers", this.CurrentLanguage);
		public string OptionPets => this.GetString("OptionPets", this.CurrentLanguage);
		public string OptionChocobos => this.GetString("OptionChocobos", this.CurrentLanguage);
		public string OptionMinions => this.GetString("OptionMinions", this.CurrentLanguage);
		public string OptionEarthlyStar => this.GetString("OptionEarthlyStar", this.CurrentLanguage);
		public string OptionEarthlyStarTip => this.GetString("OptionEarthlyStarTip", this.CurrentLanguage);
		public string OptionContextMenu => this.GetString("OptionContextMenu", this.CurrentLanguage);
		public string OptionContextMenuTip => this.GetString("OptionContextMenuTip", this.CurrentLanguage);
		public string OptionRefresh => this.GetString("OptionRefresh", this.CurrentLanguage);
		public string OptionAddPlayer => this.GetString("OptionAddPlayer", this.CurrentLanguage);
		public string OptionRemovePlayer => this.GetString("OptionRemovePlayer", this.CurrentLanguage);
		public string ColumnFirstname => this.GetString("ColumnFirstname", this.CurrentLanguage);
		public string ColumnLastname => this.GetString("ColumnLastname", this.CurrentLanguage);
		public string ColumnWorld => this.GetString("ColumnWorld", this.CurrentLanguage);
		public string ColumnDate => this.GetString("ColumnDate", this.CurrentLanguage);
		public string ColumnReason => this.GetString("ColumnReason", this.CurrentLanguage);
		public string ColumnAction => this.GetString("ColumnAction", this.CurrentLanguage);
		public string OptionLanguage => this.GetString("OptionLanguage", this.CurrentLanguage);
		public string LanguageName => this.GetString("LanguageName", this.CurrentLanguage);
	}
}