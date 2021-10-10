using System;
using System.Collections.Generic;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Visibility.Api
{
	public class VisibilityIpc : IDisposable
	{
		public const string LabelProviderApiVersion = "Visibility.ApiVersion";
		public const string LabelProviderGetVoidListEntries = "Visibility.GetVoidListEntries";
		public const string LabelProviderAddToVoidList = "Visibility.AddToVoidList";
		public const string LabelProviderRemoveFromVoidList = "Visibility.RemoveFromVoidList";

		internal ICallGateProvider<int>? ProviderApiVersion;
		internal ICallGateProvider<IEnumerable<string>>? ProviderGetVoidListEntries;
		internal ICallGateProvider<string, uint, string, object>? ProviderAddToVoidList;
		internal ICallGateProvider<string, uint, object>? ProviderRemoveFromVoidList;

		internal readonly IVisibilityApi Api;

		public VisibilityIpc(DalamudPluginInterface pluginInterface, IVisibilityApi api)
		{
			Api = api;

			try
			{
				ProviderApiVersion = pluginInterface.GetIpcProvider<int>(LabelProviderApiVersion);
				ProviderApiVersion.RegisterFunc(() => api.ApiVersion);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderApiVersion}:\n{e}");
			}

			try
			{
				ProviderGetVoidListEntries =
					pluginInterface.GetIpcProvider<IEnumerable<string>>(LabelProviderGetVoidListEntries);
				ProviderGetVoidListEntries.RegisterFunc(api.GetVoidListEntries);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderGetVoidListEntries}:\n{e}");
			}

			try
			{
				ProviderAddToVoidList =
					pluginInterface.GetIpcProvider<string, uint, string, object>(LabelProviderAddToVoidList);
				ProviderAddToVoidList.RegisterAction(api.AddToVoidList);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderAddToVoidList}:\n{e}");
			}

			try
			{
				ProviderRemoveFromVoidList =
					pluginInterface.GetIpcProvider<string, uint, object>(LabelProviderRemoveFromVoidList);
				ProviderRemoveFromVoidList.RegisterAction(api.RemoveFromVoidList);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderRemoveFromVoidList}:\n{e}");
			}
		}

		public void Dispose()
		{
			ProviderApiVersion?.UnregisterFunc();
			ProviderGetVoidListEntries?.UnregisterFunc();
			ProviderAddToVoidList?.UnregisterAction();
			ProviderRemoveFromVoidList?.UnregisterAction();
		}
	}
}