using System;
using System.Linq;

using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;

using Visibility.Api;
using Visibility.Configuration;
using Visibility.Ipc;
using Visibility.Utils;
using Visibility.Handlers;
using Visibility.Managers;

namespace Visibility;

public class VisibilityPlugin: IDalamudPlugin
{
	public string Name => "Visibility";

	public readonly Localization PluginLocalization;
	public readonly VisibilityConfiguration Configuration;
	public readonly FrameworkHandler FrameworkHandler;

	public static VisibilityPlugin Instance { get; private set; } = null!;

	public bool Disable
	{
		get => this.frameworkUpdateHandler.Disable;
		set => this.frameworkUpdateHandler.Disable = value;
	}

	public VisibilityApi Api { get; }
	public VisibilityProvider IpcProvider { get; }

	public readonly CommandManagerHandler CommandManagerHandler;
	private readonly ChatHandler chatHandler;
	private readonly FrameworkUpdateHandler frameworkUpdateHandler;
	private readonly TerritoryChangeHandler territoryChangeHandler;
	private readonly UiManager uiManager;

	public VisibilityPlugin(IDalamudPluginInterface pluginInterface)
	{
		Instance = this;

		pluginInterface.Create<Service>();
		this.Configuration = Service.PluginInterface.GetPluginConfig() as VisibilityConfiguration ??
		                     new VisibilityConfiguration();
		this.Configuration.Init(Service.ClientState.TerritoryType);
		this.PluginLocalization = new Localization(this.Configuration.Language);

		this.FrameworkHandler = new FrameworkHandler();

		this.uiManager = new UiManager(pluginInterface);
		this.frameworkUpdateHandler =
			new FrameworkUpdateHandler(this.FrameworkHandler, this.Configuration, this.PluginLocalization);
		this.CommandManagerHandler = new CommandManagerHandler(this.Configuration, this.PluginLocalization,
			this.FrameworkHandler, this.uiManager, this.frameworkUpdateHandler);
		this.chatHandler = new ChatHandler(this.Configuration);
		this.territoryChangeHandler = new TerritoryChangeHandler(this.FrameworkHandler, this.Configuration);

		this.Api = new VisibilityApi();
		this.IpcProvider = new VisibilityProvider(this.Api);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		this.IpcProvider.Dispose();
		this.Api.Dispose();
		this.territoryChangeHandler.Dispose();
		this.chatHandler.Dispose();
		this.CommandManagerHandler.Dispose();
		this.frameworkUpdateHandler.Dispose();
		this.uiManager.Dispose();
		this.FrameworkHandler.Dispose();
	}

	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	public void Show(UnitType unitType, ContainerType containerType) =>
		this.FrameworkHandler.Show(unitType, containerType);

	public void ShowPlayers(ContainerType type) => this.FrameworkHandler.ShowPlayers(type);

	public void ShowPets(ContainerType type) => this.FrameworkHandler.ShowPets(type);

	public void ShowMinions(ContainerType type) => this.FrameworkHandler.ShowMinions(type);

	public void ShowChocobos(ContainerType type) => this.FrameworkHandler.ShowChocobos(type);

	public void ShowPlayer(uint id) => this.FrameworkHandler.ShowPlayer(id);

	public void RemoveChecked(uint id) => this.FrameworkHandler.RemoveChecked(id);

	public void RemoveChecked(string name)
	{
		IGameObject? gameObject = Service.ObjectTable.SingleOrDefault(
			x => x is IPlayerCharacter character && character.Name.TextValue.Equals(
				name,
				StringComparison.InvariantCultureIgnoreCase));

		if (gameObject != null)
		{
			this.FrameworkHandler.RemoveChecked(gameObject.EntityId);
		}
	}

	public void ShowPlayer(string name)
	{
		IGameObject? gameObject = Service.ObjectTable.SingleOrDefault(
			x => x is IPlayerCharacter character && character.Name.TextValue.Equals(
				name,
				StringComparison.InvariantCultureIgnoreCase));

		if (gameObject != null)
		{
			this.FrameworkHandler.ShowPlayer(gameObject.EntityId);
		}
	}

	public void RefreshActors() => this.frameworkUpdateHandler.RequestRefresh();
}
