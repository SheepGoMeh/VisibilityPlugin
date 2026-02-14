using System;

using Dalamud.Plugin;

using Visibility.Api;
using Visibility.Configuration;
using Visibility.Ipc;
using Visibility.Handlers;
using Visibility.Managers;
using Visibility.Utils;

namespace Visibility;

public class VisibilityPlugin: IDalamudPlugin
{
	public string Name => "Visibility";

	private readonly Localization pluginLocalization;
	private readonly VisibilityConfiguration configuration;
	private readonly FrameworkHandler frameworkHandler;
	private readonly FrameworkUpdateHandler frameworkUpdateHandler;
	private readonly CommandManagerHandler commandManagerHandler;
	private readonly ChatHandler chatHandler;
	private readonly TerritoryChangeHandler territoryChangeHandler;
	private readonly UiManager uiManager;
	private readonly VisibilityApi api;
	private readonly VisibilityProvider ipcProvider;

	public VisibilityPlugin(IDalamudPluginInterface pluginInterface)
	{
		pluginInterface.Create<Service>();

		this.configuration = Service.PluginInterface.GetPluginConfig() as VisibilityConfiguration ??
		                     new VisibilityConfiguration();
		this.configuration.Init(Service.ClientState.TerritoryType);
		this.pluginLocalization = new Localization(this.configuration.Language);

		this.frameworkHandler = new FrameworkHandler(this.configuration);
		this.frameworkUpdateHandler =
			new FrameworkUpdateHandler(this.frameworkHandler, this.configuration, this.pluginLocalization);
		this.frameworkHandler.SetDisableCheck(() => this.frameworkUpdateHandler.Disable);

		this.configuration.SettingsHandler =
			new SettingsHandler(this.configuration, this.frameworkHandler, this.frameworkUpdateHandler);

		this.commandManagerHandler = new CommandManagerHandler(
			this.configuration, this.pluginLocalization, this.frameworkHandler, this.frameworkUpdateHandler);
		this.frameworkUpdateHandler.SetCommandManagerHandler(this.commandManagerHandler);

		this.uiManager = new UiManager(
			pluginInterface, this.configuration, this.pluginLocalization,
			this.commandManagerHandler, this.frameworkHandler, this.frameworkUpdateHandler);
		this.commandManagerHandler.SetOpenConfigUi(this.uiManager.OpenConfigUi);

		this.chatHandler = new ChatHandler(this.configuration);
		this.territoryChangeHandler = new TerritoryChangeHandler(this.frameworkHandler, this.configuration);

		this.api = new VisibilityApi(this.configuration, this.commandManagerHandler, this.frameworkHandler);
		this.ipcProvider = new VisibilityProvider(this.api);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		this.ipcProvider.Dispose();
		this.api.Dispose();
		this.territoryChangeHandler.Dispose();
		this.chatHandler.Dispose();
		this.commandManagerHandler.Dispose();
		this.frameworkUpdateHandler.Dispose();
		this.uiManager.Dispose();
		this.frameworkHandler.Dispose();
	}

	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

}
