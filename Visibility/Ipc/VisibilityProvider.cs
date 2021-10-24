using System;
using System.Collections.Generic;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Visibility.Api;

namespace Visibility.Ipc
{
	public class VisibilityProvider : IDisposable
	{
		public const string LabelProviderApiVersion = "Visibility.ApiVersion";
		public const string LabelProviderGetVoidListEntries = "Visibility.GetVoidListEntries";
		public const string LabelProviderAddToVoidList = "Visibility.AddToVoidList";
		public const string LabelProviderRemoveFromVoidList = "Visibility.RemoveFromVoidList";
		public const string LabelProviderGetWhitelistEntries = "Visibility.GetWhitelistEntries";
		public const string LabelProviderAddToWhitelist = "Visibility.AddToWhitelist";
		public const string LabelProviderRemoveFromWhitelist = "Visibility.RemoveFromWhitelist";
		public const string LabelProviderEnable = "Visibility.Enable";

		internal ICallGateProvider<int>? ProviderApiVersion;
		internal ICallGateProvider<IEnumerable<string>>? ProviderGetVoidListEntries;
		internal ICallGateProvider<string, uint, string, object>? ProviderAddToVoidList;
		internal ICallGateProvider<string, uint, object>? ProviderRemoveFromVoidList;
		internal ICallGateProvider<IEnumerable<string>>? ProviderGetWhitelistEntries;
		internal ICallGateProvider<string, uint, string, object>? ProviderAddToWhitelist;
		internal ICallGateProvider<string, uint, object>? ProviderRemoveFromWhitelist;
		internal ICallGateProvider<bool, object>? ProviderEnable;

		internal readonly IVisibilityApi Api;

		public VisibilityProvider(DalamudPluginInterface pluginInterface, IVisibilityApi api)
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
			
			try
			{
				ProviderGetWhitelistEntries =
					pluginInterface.GetIpcProvider<IEnumerable<string>>(LabelProviderGetWhitelistEntries);
				ProviderGetWhitelistEntries.RegisterFunc(api.GetWhitelistEntries);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderGetWhitelistEntries}:\n{e}");
			}

			try
			{
				ProviderAddToWhitelist =
					pluginInterface.GetIpcProvider<string, uint, string, object>(LabelProviderAddToWhitelist);
				ProviderAddToWhitelist.RegisterAction(api.AddToWhitelist);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderAddToWhitelist}:\n{e}");
			}

			try
			{
				ProviderRemoveFromWhitelist =
					pluginInterface.GetIpcProvider<string, uint, object>(LabelProviderRemoveFromWhitelist);
				ProviderRemoveFromWhitelist.RegisterAction(api.RemoveFromWhitelist);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderRemoveFromWhitelist}:\n{e}");
			}
			
			try
			{
				ProviderEnable =
					pluginInterface.GetIpcProvider<bool, object>(LabelProviderEnable);
				ProviderEnable.RegisterAction(api.Enable);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderEnable}:\n{e}");
			}
		}

		public void Dispose()
		{
			ProviderApiVersion?.UnregisterFunc();
			ProviderGetVoidListEntries?.UnregisterFunc();
			ProviderAddToVoidList?.UnregisterAction();
			ProviderRemoveFromVoidList?.UnregisterAction();
			ProviderGetWhitelistEntries?.UnregisterFunc();
			ProviderAddToWhitelist?.UnregisterAction();
			ProviderRemoveFromWhitelist?.UnregisterAction();
			ProviderEnable?.UnregisterAction();
		}
	}
}