using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

using Newtonsoft.Json;

namespace Visibility;

public class Localization
{
	private readonly Dictionary<Language, Dictionary<string, string>> strings = new();
	public List<Language> AvailableLanguages { get; } = new();

	public Language CurrentLanguage;

	public Localization(Language language = Language.English)
	{
		this.LoadStrings(Language.English);
		this.LoadStrings(Language.French);
		this.LoadStrings(Language.German);
		this.LoadStrings(Language.Italian);
		this.LoadStrings(Language.Portuguese);
		this.LoadStrings(Language.Spanish);
		this.LoadStrings(Language.Swedish);
		this.LoadStrings(Language.Russian);
		this.LoadStrings(Language.Japanese);
		this.LoadStrings(Language.ChineseSimplified);
		this.LoadStrings(Language.ChineseTraditional);
		this.CurrentLanguage = this.AvailableLanguages.Contains(language) ? language : Language.English;
	}

	private void LoadStrings(Language language)
	{
		string? jsonString = language switch
		{
			Language.English => Properties.Resources.en_US_strings,
			Language.French => Properties.Resources.fr_FR_strings,
			Language.German => Properties.Resources.de_DE_strings,
			Language.Italian => Properties.Resources.it_IT_strings,
			Language.Japanese => Properties.Resources.ja_JP_strings,
			Language.Portuguese => Properties.Resources.pt_BR_strings,
			Language.Russian => Properties.Resources.ru_RU_strings,
			Language.Spanish => Properties.Resources.es_ES_strings,
			Language.Swedish => Properties.Resources.sv_SE_strings,
			Language.ChineseSimplified => Properties.Resources.zh_CN_strings,
			Language.ChineseTraditional => Properties.Resources.zh_TW_strings,
			_ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
		};

		this.AvailableLanguages.Add(language);
		this.strings[language] = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString)!;
	}

	private static SeString FormatSeString(string format, params SeString[] args)
	{
		SeString result = new();
		Regex rx = new(@"{(\d+)}", RegexOptions.Compiled);

		MatchCollection matches = rx.Matches(format);

		for (int i = 0; i < matches.Count; ++i)
		{
			Group matchGroup = matches[i].Groups[0];
			Group? nextMatchGroup = i + 1 < matches.Count ? matches[i + 1].Groups[0] : null;
			int index = int.Parse(matches[i].Groups[1].Value);

			// If index is less than array size, append string
			if (index < args.Length)
			{
				result.Append(args[index]);
			}

			// Copy substring between current match and next match
			if (nextMatchGroup != null && nextMatchGroup.Index - matchGroup.Index > matchGroup.Length)
			{
				result.Append(
					new TextPayload(
						format.Substring(
							matchGroup.Index + matchGroup.Length,
							nextMatchGroup.Index - (matchGroup.Index + matchGroup.Length))));
			}

			// Copy substring between current match and end of string
			else if (nextMatchGroup == null && format.Length - matchGroup.Index > matchGroup.Length)
			{
				result.Append(
					new TextPayload(
						format.Substring(
							matchGroup.Index + matchGroup.Length,
							format.Length - (matchGroup.Index + matchGroup.Length))));
			}
		}

		return result;
	}

	public string GetString(string key, Language language) =>
		this.strings[language].GetValueOrDefault(key, key);

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
		Swedish,
		ChineseSimplified,
		ChineseTraditional
	}

	public string PluginCommandHelpMessage => this.GetString("PluginCommandHelpMessage", this.CurrentLanguage);
	public string VoidPlayerHelpMessage => this.GetString("VoidPlayerHelpMessage", this.CurrentLanguage);
	public string VoidTargetPlayerHelpMessage => this.GetString("VoidTargetPlayerHelpMessage", this.CurrentLanguage);
	public string WhitelistPlayerHelpMessage => this.GetString("WhitelistPlayerHelpMessage", this.CurrentLanguage);

	public string WhitelistTargetPlayerHelpMessage =>
		this.GetString("WhitelistTargetPlayerHelpMessage", this.CurrentLanguage);

	public string RefreshComplete => this.GetString("RefreshComplete", this.CurrentLanguage);
	public string PluginCommandHelpMenu1 => this.GetString("PluginCommandHelpMenu1", this.CurrentLanguage);
	public string PluginCommandHelpMenu2 => this.GetString("PluginCommandHelpMenu2", this.CurrentLanguage);
	public string PluginCommandHelpMenu3 => this.GetString("PluginCommandHelpMenu3", this.CurrentLanguage);
	public string PluginCommandHelpMenu4 => this.GetString("PluginCommandHelpMenu4", this.CurrentLanguage);
	public string PluginCommandHelpMenuError => this.GetString("PluginCommandHelpMenuError", this.CurrentLanguage);

	public string PluginCommandHelpMenuInvalidValueError(string value) =>
		this.GetString("PluginCommandHelpMenuInvalidValueError", this.CurrentLanguage).Format(value);

	public string VoidListName => this.GetString("VoidListName", this.CurrentLanguage);
	public string WhitelistName => this.GetString("WhitelistName", this.CurrentLanguage);

	public string NoArgumentsError(string name) =>
		this.GetString("NoArgumentsError", this.CurrentLanguage).Format(name);

	public string NotEnoughArgumentsError(string name) =>
		this.GetString("NotEnoughArgumentsError", this.CurrentLanguage).Format(name);

	public string InvalidWorldNameError(string name, string worldName) =>
		this.GetString("InvalidWorldNameError", this.CurrentLanguage).Format(name, worldName);

	public string EntryAdded(string name, string entryName) =>
		this.GetString("EntryAdded", this.CurrentLanguage).Format(name, entryName);

	public string EntryExistsError(string name, string entryName) =>
		this.GetString("EntryExistsError", this.CurrentLanguage).Format(name, entryName);

	public string InvalidTargetError(string name) =>
		this.GetString("InvalidTargetError", this.CurrentLanguage).Format(name);

	public string ContextMenuAdd(string name) => this.GetString("ContextMenuAdd", this.CurrentLanguage).Format(name);

	public string ContextMenuRemove(string name) =>
		this.GetString("ContextMenuRemove", this.CurrentLanguage).Format(name);

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
	public string OptionShowTargetOfTarget => this.GetString("OptionShowTargetOfTarget", this.CurrentLanguage);
	public string OptionShowTargetOfTargetTip => this.GetString("OptionShowTargetOfTargetTip", this.CurrentLanguage);
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
	public string AdvancedOption => this.GetString("AdvancedOption", this.CurrentLanguage);
	public string AdvancedOptionTooltip => this.GetString("AdvancedOptionTooltip", this.CurrentLanguage);
	public string ResetToCurrentArea => this.GetString("ResetToCurrentArea", this.CurrentLanguage);

	public SeString EntryAdded(string name, SeString entryName) =>
		FormatSeString(this.GetString("EntryAdded", this.CurrentLanguage), name, entryName);

	public SeString EntryExistsError(string name, SeString entryName) =>
		FormatSeString(this.GetString("EntryExistsError", this.CurrentLanguage), name, entryName);
}
