using System;

using Visibility.Configuration;
using Visibility.Utils;

namespace Visibility.Handlers;

public class TerritoryChangeHandler: IDisposable
{
	private readonly FrameworkHandler frameworkHandler;
	private readonly VisibilityConfiguration configuration;

	public TerritoryChangeHandler(FrameworkHandler framework, VisibilityConfiguration config)
	{
		this.frameworkHandler = framework;
		this.configuration = config;

		Service.ClientState.TerritoryChanged += this.ClientStateOnTerritoryChanged;
	}

	public void Dispose()
	{
		Service.ClientState.TerritoryChanged -= this.ClientStateOnTerritoryChanged;
	}

	private void ClientStateOnTerritoryChanged(ushort territoryType)
	{
		this.frameworkHandler.OnTerritoryChanged();

		if (!this.configuration.AdvancedEnabled)
		{
			return;
		}

		this.configuration.Enabled = false; // Temporarily disable while updating
		this.configuration.UpdateCurrentConfig(territoryType);
		this.configuration.Enabled = true;
	}
}
