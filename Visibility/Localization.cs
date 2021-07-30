using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using Newtonsoft.Json;

namespace Visibility
{
	public class Localization
	{
		private readonly Dictionary<Language, Dictionary<string, string>> _strings = new();

		public Language CurrentLanguage;

		public Localization(Language language = Language.English)
		{
			CurrentLanguage = language;
			LoadStrings(Language.English, "en");
			LoadStrings(Language.French, "fr");
			LoadStrings(Language.Italian, "it");
			LoadStrings(Language.Portuguese, "pt-BR");
			LoadStrings(Language.Spanish, "es-ES");
		}

		private void LoadStrings(Language language, string languageName)
		{
			var jsonString = string.Empty;
			switch (language)
			{
				case Language.English:
					jsonString = Properties.Resources.en_strings;
					break;
				case Language.French:
					jsonString = Properties.Resources.fr_strings;
					break;
				case Language.German:
					break;
				case Language.Italian:
					jsonString = Properties.Resources.it_strings;
					break;
				case Language.Japanese:
					break;
				case Language.Portuguese:
					jsonString = Properties.Resources.pt_BR_strings;
					break;
				case Language.Russian:
					break;
				case Language.Spanish:
					jsonString = Properties.Resources.es_ES_strings;
					break;
				case Language.ChineseSimplified:
					break;
				case Language.ChineseTraditional:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(language), language, null);
			}

			_strings[language] = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
		}

		private string GetString(string key) => _strings[CurrentLanguage].ContainsKey(key) ? _strings[CurrentLanguage][key] : key;

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

		public string PluginCommandHelpMessage => GetString("PluginCommandHelpMessage");
		public string VoidPlayerHelpMessage => GetString("VoidPlayerHelpMessage");
		public string VoidTargetPlayerHelpMessage => GetString("VoidTargetPlayerHelpMessage");
		public string WhitelistPlayerHelpMessage => GetString("WhitelistPlayerHelpMessage");
		public string WhitelistTargetPlayerHelpMessage => GetString("WhitelistTargetPlayerHelpMessage");
		public string RefreshComplete => GetString("RefreshComplete");
		public string PluginCommandHelpMenu1 => GetString("PluginCommandHelpMenu1");
		public string PluginCommandHelpMenu2 => GetString("PluginCommandHelpMenu2");
		public string PluginCommandHelpMenu3 => GetString("PluginCommandHelpMenu3");
		public string PluginCommandHelpMenu4 => GetString("PluginCommandHelpMenu4");
		public string PluginCommandHelpMenuError => GetString("PluginCommandHelpMenuError");
		public string PluginCommandHelpMenuInvalidValueError(string value) => GetString("PluginCommandHelpMenuInvalidValueError").Format(value);
		public string VoidListName => GetString("VoidListName");
		public string WhitelistName => GetString("WhitelistName");
		public string NoArgumentsError(string name) => GetString("NoArgumentsError").Format(name);
		public string NotEnoughArgumentsError(string name) => GetString("NotEnoughArgumentsError").Format(name);
		public string InvalidWorldNameError(string name, string worldName) => GetString("InvalidWorldNameError").Format(name, worldName);
		public string EntryAdded(string name, string entryName) => GetString("EntryAdded").Format(name, entryName);
		public string EntryExistsError(string name, string entryName) => GetString("EntryExistsError").Format(name, entryName);
		public string InvalidTargetError(string name) => GetString("InvalidTargetError").Format(name);
		public string ContextMenuAdd(string name) => GetString("ContextMenuAdd").Format(name);
		public string ContextMenuRemove(string name) => GetString("ContextMenuRemove").Format(name);
		public string OptionEnable => GetString("OptionEnable");
		public string OptionHideAll => GetString("OptionHideAll");
		public string OptionShowParty => GetString("OptionShowParty");
		public string OptionShowFriends => GetString("OptionShowFriends");
		public string OptionShowFc => GetString("OptionShowFC");
		public string OptionShowDead => GetString("OptionShowDead");
		public string OptionPlayers => GetString("OptionPlayers");
		public string OptionPets => GetString("OptionPets");
		public string OptionChocobos => GetString("OptionChocobos");
		public string OptionMinions => GetString("OptionMinions");
		public string OptionEarthlyStar => GetString("OptionEarthlyStar");
		public string OptionEarthlyStarTip => GetString("OptionEarthlyStarTip");
		public string OptionContextMenu => GetString("OptionContextMenu");
		public string OptionContextMenuTip => GetString("OptionContextMenuTip");
		public string OptionRefresh => GetString("OptionRefresh");
		public string OptionAddPlayer => GetString("OptionAddPlayer");
		public string OptionRemovePlayer => GetString("OptionRemovePlayer");
		public string ColumnFirstname => GetString("ColumnFirstname");
		public string ColumnLastname => GetString("ColumnLastname");
		public string ColumnWorld => GetString("ColumnWorld");
		public string ColumnDate => GetString("ColumnDate");
		public string ColumnReason => GetString("ColumnReason");
		public string ColumnAction => GetString("ColumnAction");
		public string OptionLanguage => GetString("OptionLanguage");
		public string LanguageName => GetString("LanguageName");
	}
}