using System;
using System.Threading.Tasks;

using Dalamud.Plugin.Services;

using Visibility.Configuration;
using Visibility.Utils;

namespace Visibility.Handlers;

public class FrameworkUpdateHandler: IDisposable
{
	private readonly FrameworkHandler frameworkHandler;
	private readonly VisibilityConfiguration configuration;
	private readonly Localization pluginLocalization;

	private bool refresh;
	public bool Disable { get; set; }

	public FrameworkUpdateHandler(FrameworkHandler framework, VisibilityConfiguration config, Localization localization)
	{
		this.frameworkHandler = framework;
		this.configuration = config;
		this.pluginLocalization = localization;

		Service.Framework.Update += this.FrameworkOnOnUpdateEvent;
	}

	public void Dispose()
	{
		Service.Framework.Update -= this.FrameworkOnOnUpdateEvent;
	}

	public void RequestRefresh()
	{
		if (!this.refresh)
		{
			this.refresh = true;
		}
	}

	private void FrameworkOnOnUpdateEvent(IFramework framework)
	{
		if (this.Disable)
		{
			this.frameworkHandler.ShowAll();
			this.Disable = false;

			if (this.refresh)
			{
				Task.Run(
					async () =>
					{
						await Task.Delay(250);
						this.configuration.Enabled = true;
						Service.ChatGui.Print(this.pluginLocalization.RefreshComplete);
					});
			}

			this.refresh = false;
		}
		else if (this.refresh)
		{
			this.Disable = true;
			this.configuration.Enabled = false;
		}
		else
		{
			this.frameworkHandler.Update();
		}
	}
}
