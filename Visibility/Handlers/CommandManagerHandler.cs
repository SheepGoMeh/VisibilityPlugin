using System;
using System.Linq;

using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

using Visibility.Configuration;
using Visibility.Utils;
using Visibility.Void;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Lumina.Excel.Sheets;

using Visibility.Managers;

namespace Visibility.Handlers;

public class CommandManagerHandler: IDisposable
{
	private readonly VisibilityConfiguration configuration;
	private readonly Localization pluginLocalization;
	private readonly FrameworkHandler frameworkHandler;
	private readonly UiManager uiManager;
	private readonly FrameworkUpdateHandler frameworkUpdateHandler;

	private static string PluginCommandName => "/pvis";
	private static string VoidCommandName => "/void";
	private static string VoidTargetCommandName => "/voidtarget";
	private static string WhitelistCommandName => "/whitelist";
	private static string WhitelistTargetCommandName => "/whitelisttarget";

	public CommandManagerHandler(
		VisibilityConfiguration config,
		Localization localization,
		FrameworkHandler framework,
		UiManager uiManager,
		FrameworkUpdateHandler frameworkUpdateHandler)
	{
		this.configuration = config;
		this.pluginLocalization = localization;
		this.frameworkHandler = framework;
		this.uiManager = uiManager;
		this.frameworkUpdateHandler = frameworkUpdateHandler;

		this.RegisterCommands();
	}

	private void RegisterCommands()
	{
		Service.CommandManager.AddHandler(
			PluginCommandName,
			new CommandInfo(this.PluginCommand)
			{
				HelpMessage = this.pluginLocalization.PluginCommandHelpMessage, ShowInHelp = true
			});

		Service.CommandManager.AddHandler(
			VoidCommandName,
			new CommandInfo(this.VoidPlayer)
			{
				HelpMessage = this.pluginLocalization.VoidPlayerHelpMessage, ShowInHelp = true
			});

		Service.CommandManager.AddHandler(
			VoidTargetCommandName,
			new CommandInfo(this.VoidTargetPlayer)
			{
				HelpMessage = this.pluginLocalization.VoidTargetPlayerHelpMessage, ShowInHelp = true
			});

		Service.CommandManager.AddHandler(
			WhitelistCommandName,
			new CommandInfo(this.WhitelistPlayer)
			{
				HelpMessage = this.pluginLocalization.WhitelistPlayerHelpMessage, ShowInHelp = true
			});

		Service.CommandManager.AddHandler(
			WhitelistTargetCommandName,
			new CommandInfo(this.WhitelistTargetPlayer)
			{
				HelpMessage = this.pluginLocalization.WhitelistTargetPlayerHelpMessage, ShowInHelp = true
			});
	}

	public void Dispose()
	{
		Service.CommandManager.RemoveHandler(PluginCommandName);
		Service.CommandManager.RemoveHandler(VoidCommandName);
		Service.CommandManager.RemoveHandler(VoidTargetCommandName);
		Service.CommandManager.RemoveHandler(WhitelistCommandName);
		Service.CommandManager.RemoveHandler(WhitelistTargetCommandName);
	}

	private void PluginCommand(string command, string arguments)
	{
		if (this.frameworkUpdateHandler.Disable)
		{
			return;
		}

		if (string.IsNullOrEmpty(arguments))
		{
			this.uiManager.OpenConfigUi();
		}
		else
		{
			string[] args = arguments.Split([' '], 2);

			if (args[0].Equals("help", StringComparison.InvariantCultureIgnoreCase))
			{
				Service.ChatGui.Print(this.pluginLocalization.PluginCommandHelpMenu1);
				Service.ChatGui.Print(this.pluginLocalization.PluginCommandHelpMenu2);
				Service.ChatGui.Print(this.pluginLocalization.PluginCommandHelpMenu3);
				Service.ChatGui.Print(this.pluginLocalization.PluginCommandHelpMenu4);

				foreach (string? key in this.configuration.SettingsHandler.GetKeys())
				{
					Service.ChatGui.Print($"{key}");
				}

				return;
			}

			if (args[0].Equals("refresh", StringComparison.InvariantCulture))
			{
				this.frameworkUpdateHandler.RequestRefresh();
				return;
			}

			if (args.Length != 2)
			{
				Service.ChatGui.Print(this.pluginLocalization.PluginCommandHelpMenuError);
				return;
			}

			if (!this.configuration.SettingsHandler.ContainsKey(args[0]))
			{
				Service.ChatGui.Print(this.pluginLocalization.PluginCommandHelpMenuInvalidValueError(args[0]));
				return;
			}

			bool value = false;
			bool toggle = false;

			switch (args[1].ToLowerInvariant())
			{
				case "0":
				case "off":
				case "false":
					value = false;
					break;

				case "1":
				case "on":
				case "true":
					value = true;
					break;

				case "toggle":
					toggle = true;
					break;
				default:
					Service.ChatGui.Print(this.pluginLocalization.PluginCommandHelpMenuInvalidValueError(args[1]));
					return;
			}

			this.configuration.SettingsHandler.Invoke(args[0], value, toggle, false);
			this.configuration.Save();
		}
	}

	public void VoidPlayer(string command, string arguments)
	{
		if (string.IsNullOrEmpty(arguments))
		{
			Service.ChatGui.Print(this.pluginLocalization.NoArgumentsError(this.pluginLocalization.VoidListName));
			return;
		}

		string[] args = arguments.Split([' '], 4);

		if (args.Length < 3)
		{
			Service.ChatGui.Print(
				this.pluginLocalization.NotEnoughArgumentsError(this.pluginLocalization.VoidListName));
			return;
		}

		World? world = Service.DataManager.GetExcelSheet<World>().SingleOrDefault(x =>
			x.DataCenter.ValueNullable?.Region != 0 &&
			x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

		if (world is null)
		{
			Service.ChatGui.Print(
				this.pluginLocalization.InvalidWorldNameError(this.pluginLocalization.VoidListName, args[2]));
			return;
		}

		string playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

		VoidItem voidItem;
		IGameObject? playerCharacter = Service.ObjectTable.SingleOrDefault(
				x => x is IPlayerCharacter character && character.HomeWorld.Value.RowId == world.Value.RowId &&
				     character.Name.TextValue.Equals(playerName, StringComparison.InvariantCultureIgnoreCase)) as
			IPlayerCharacter;

		if (playerCharacter != null)
		{
			unsafe
			{
				Character* character = (Character*)playerCharacter.Address;
				voidItem = new VoidItem
				{
					Id = character->ContentId,
					Name = character->NameString,
					HomeworldId = world.Value.RowId,
					HomeworldName = world.Value.Name.ToString(),
					Reason = args.Length == 3 ? string.Empty : args[3],
					Manual = command == "VoidUIManual" // Or handle UI source differently
				};
			}
		}
		else
		{
			voidItem = new VoidItem
			{
				Name = playerName,
				HomeworldId = world.Value.RowId,
				HomeworldName = world.Value.Name.ToString(),
				Reason = args.Length == 3 ? string.Empty : args[3],
				Manual = command == "VoidUIManual" // Or handle UI source differently
			};
		}

		SeString playerString = new(
			new PlayerPayload(playerName, world.Value.RowId),
			new IconPayload(BitmapFontIcon.CrossWorld),
			new TextPayload(world.Value.Name.ToString()));

		if (!this.configuration.VoidList.Any(
			    x =>
				    x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId))
		{
			this.configuration.VoidList.Add(voidItem);
			this.configuration.Save();

			if (playerCharacter != null)
			{
				this.frameworkHandler.RemoveChecked(playerCharacter.EntityId);
			}

			Service.ChatGui.Print(
				this.pluginLocalization.EntryAdded(this.pluginLocalization.VoidListName, playerString));
		}
		else
		{
			Service.ChatGui.Print(
				this.pluginLocalization.EntryExistsError(this.pluginLocalization.VoidListName, playerString));
		}
	}

	private void VoidTargetPlayer(string command, string arguments)
	{
		if (this.GetTargetPlayer() is { } playerCharacter)
		{
			VoidItem voidItem;

			unsafe
			{
				Character* character = (Character*)playerCharacter.Address;
				voidItem = new VoidItem
				{
					Id = character->ContentId,
					Name = character->NameString,
					HomeworldId = character->HomeWorld,
					HomeworldName = playerCharacter.HomeWorld.Value.Name.ToString(),
					Reason = arguments,
					Manual = false
				};
			}

			SeString playerString = new(
				new PlayerPayload(playerCharacter.Name.TextValue, playerCharacter.HomeWorld.Value.RowId),
				new IconPayload(BitmapFontIcon.CrossWorld),
				new TextPayload(playerCharacter.HomeWorld.Value.Name.ToString()));

			if (!this.configuration.VoidList.Any(
				    x =>
					    x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId))
			{
				this.configuration.VoidList.Add(voidItem);
				this.configuration.Save();
				this.frameworkHandler.RemoveChecked(playerCharacter.EntityId);
				Service.ChatGui.Print(
					this.pluginLocalization.EntryAdded(this.pluginLocalization.VoidListName, playerString));
			}
			else
			{
				Service.ChatGui.Print(
					this.pluginLocalization.EntryExistsError(this.pluginLocalization.VoidListName, playerString));
			}
		}
		else
		{
			Service.ChatGui.Print(this.pluginLocalization.InvalidTargetError(this.pluginLocalization.VoidListName));
		}
	}

	public void WhitelistPlayer(string command, string arguments)
	{
		if (string.IsNullOrEmpty(arguments))
		{
			Service.ChatGui.Print(this.pluginLocalization.NoArgumentsError(this.pluginLocalization.WhitelistName));
			return;
		}

		string[] args = arguments.Split([' '], 4);

		if (args.Length < 3)
		{
			Service.ChatGui.Print(
				this.pluginLocalization.NotEnoughArgumentsError(this.pluginLocalization.WhitelistName));
			return;
		}

		World? world = Service.DataManager.GetExcelSheet<World>().SingleOrDefault(x =>
			x.DataCenter.ValueNullable?.Region != 0 &&
			x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

		if (world is null)
		{
			Service.ChatGui.Print(
				this.pluginLocalization.InvalidWorldNameError(this.pluginLocalization.WhitelistName, args[2]));
			return;
		}

		string playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

		IPlayerCharacter? playerCharacter = Service.ObjectTable.SingleOrDefault(
			x =>
				x is IPlayerCharacter character && character.HomeWorld.Value.RowId == world.Value.RowId &&
				character.Name.TextValue.Equals(playerName, StringComparison.Ordinal)) as IPlayerCharacter;

		VoidItem item;

		if (playerCharacter != null)
		{
			unsafe
			{
				Character* character = (Character*)playerCharacter.Address;
				item = new VoidItem
				{
					Id = character->ContentId,
					Name = character->NameString,
					HomeworldId = world.Value.RowId,
					HomeworldName = world.Value.Name.ToString(),
					Reason = args.Length == 3 ? string.Empty : args[3],
					Manual = command == "WhitelistUIManual" // Or handle UI source differently
				};
			}
		}
		else
		{
			item = new VoidItem
			{
				Name = playerName,
				HomeworldId = world.Value.RowId,
				HomeworldName = world.Value.Name.ToString(),
				Reason = args.Length == 3 ? string.Empty : args[3],
				Manual = command == "WhitelistUIManual" // Or handle UI source differently
			};
		}

		SeString playerString = new(
			new PlayerPayload(playerName, world.Value.RowId),
			new IconPayload(BitmapFontIcon.CrossWorld),
			new TextPayload(world.Value.Name.ToString()));

		if (!this.configuration.Whitelist.Any(
			    x =>
				    x.Name == item.Name && x.HomeworldId == item.HomeworldId))
		{
			this.configuration.Whitelist.Add(item);
			this.configuration.Save();

			if (playerCharacter != null)
			{
				this.frameworkHandler.RemoveChecked(playerCharacter.EntityId);
				this.frameworkHandler.ShowPlayer(playerCharacter.EntityId);
			}

			Service.ChatGui.Print(
				this.pluginLocalization.EntryAdded(this.pluginLocalization.WhitelistName, playerString));
		}
		else
		{
			Service.ChatGui.Print(
				this.pluginLocalization.EntryExistsError(this.pluginLocalization.WhitelistName, playerString));
		}
	}

	private void WhitelistTargetPlayer(string command, string arguments)
	{
		if (this.GetTargetPlayer() is IPlayerCharacter playerCharacter)
		{
			VoidItem item;

			unsafe
			{
				Character* character = (Character*)playerCharacter.Address;
				item = new VoidItem
				{
					Id = character->ContentId,
					Name = character->NameString,
					HomeworldId = character->HomeWorld,
					HomeworldName = playerCharacter.HomeWorld.Value.Name.ToString(),
					Reason = arguments,
					Manual = false
				};
			}

			SeString playerString = new(
				new PlayerPayload(playerCharacter.Name.TextValue, playerCharacter.HomeWorld.Value.RowId),
				new IconPayload(BitmapFontIcon.CrossWorld),
				new TextPayload(playerCharacter.HomeWorld.Value.Name.ToString()));

			if (!this.configuration.Whitelist.Any(
				    x =>
					    x.Name == item.Name && x.HomeworldId == item.HomeworldId))
			{
				this.configuration.Whitelist.Add(item);
				this.configuration.Save();
				this.frameworkHandler.RemoveChecked(playerCharacter.EntityId);
				this.frameworkHandler.ShowPlayer(playerCharacter.EntityId);
				Service.ChatGui.Print(
					this.pluginLocalization.EntryAdded(this.pluginLocalization.WhitelistName, playerString));
			}
			else
			{
				Service.ChatGui.Print(
					this.pluginLocalization.EntryExistsError(this.pluginLocalization.WhitelistName, playerString));
			}
		}
		else
		{
			Service.ChatGui.Print(this.pluginLocalization.InvalidTargetError(this.pluginLocalization.WhitelistName));
		}
	}

	private IPlayerCharacter? GetTargetPlayer()
	{
		return Service.ObjectTable.SingleOrDefault(
			x => x is IPlayerCharacter
			     && x.EntityId != 0
			     && x.EntityId != Service.ClientState.LocalPlayer?.EntityId
			     && x.EntityId == Service.ClientState.LocalPlayer?.TargetObjectId) as IPlayerCharacter;
	}
}
