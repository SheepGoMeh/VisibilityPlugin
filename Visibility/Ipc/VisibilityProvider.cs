using System;
using System.Collections.Generic;
using Dalamud.Logging;
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

		public VisibilityProvider(IVisibilityApi api)
		{
			this.Api = api;

			try
			{
				this.ProviderApiVersion = VisibilityPlugin.PluginInterface.GetIpcProvider<int>(LabelProviderApiVersion);
				this.ProviderApiVersion.RegisterFunc(() => api.ApiVersion);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderApiVersion}:\n{e}");
			}

			try
			{
				this.ProviderGetVoidListEntries =
					VisibilityPlugin.PluginInterface.GetIpcProvider<IEnumerable<string>>(LabelProviderGetVoidListEntries);
				this.ProviderGetVoidListEntries.RegisterFunc(api.GetVoidListEntries);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderGetVoidListEntries}:\n{e}");
			}

			try
			{
				this.ProviderAddToVoidList =
					VisibilityPlugin.PluginInterface.GetIpcProvider<string, uint, string, object>(LabelProviderAddToVoidList);
				this.ProviderAddToVoidList.RegisterAction(api.AddToVoidList);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderAddToVoidList}:\n{e}");
			}

			try
			{
				this.ProviderRemoveFromVoidList =
					VisibilityPlugin.PluginInterface.GetIpcProvider<string, uint, object>(LabelProviderRemoveFromVoidList);
				this.ProviderRemoveFromVoidList.RegisterAction(api.RemoveFromVoidList);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderRemoveFromVoidList}:\n{e}");
			}
			
			try
			{
				this.ProviderGetWhitelistEntries =
					VisibilityPlugin.PluginInterface.GetIpcProvider<IEnumerable<string>>(LabelProviderGetWhitelistEntries);
				this.ProviderGetWhitelistEntries.RegisterFunc(api.GetWhitelistEntries);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderGetWhitelistEntries}:\n{e}");
			}

			try
			{
				this.ProviderAddToWhitelist =
					VisibilityPlugin.PluginInterface.GetIpcProvider<string, uint, string, object>(LabelProviderAddToWhitelist);
				this.ProviderAddToWhitelist.RegisterAction(api.AddToWhitelist);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderAddToWhitelist}:\n{e}");
			}

			try
			{
				this.ProviderRemoveFromWhitelist =
					VisibilityPlugin.PluginInterface.GetIpcProvider<string, uint, object>(LabelProviderRemoveFromWhitelist);
				this.ProviderRemoveFromWhitelist.RegisterAction(api.RemoveFromWhitelist);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderRemoveFromWhitelist}:\n{e}");
			}
			
			try
			{
				this.ProviderEnable =
					VisibilityPlugin.PluginInterface.GetIpcProvider<bool, object>(LabelProviderEnable);
				this.ProviderEnable.RegisterAction(api.Enable);
				this.ProviderEnable?.SendMessage(true);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error registering IPC provider for {LabelProviderEnable}:\n{e}");
			}
		}

		public void Dispose()
		{
			this.ProviderApiVersion?.UnregisterFunc();
			this.ProviderGetVoidListEntries?.UnregisterFunc();
			this.ProviderAddToVoidList?.UnregisterAction();
			this.ProviderRemoveFromVoidList?.UnregisterAction();
			this.ProviderGetWhitelistEntries?.UnregisterFunc();
			this.ProviderAddToWhitelist?.UnregisterAction();
			this.ProviderRemoveFromWhitelist?.UnregisterAction();
			this.ProviderEnable?.UnregisterAction();
		}
	}
}