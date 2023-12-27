using System;
using System.Collections.Generic;

using Dalamud.Plugin.Ipc;

using Visibility.Api;

namespace Visibility.Ipc;

public class VisibilityProvider: IDisposable
{
	public const string LabelProviderApiVersion = "Visibility.ApiVersion";
	public const string LabelProviderGetVoidListEntries = "Visibility.GetVoidListEntries";
	public const string LabelProviderAddToVoidList = "Visibility.AddToVoidList";
	public const string LabelProviderRemoveFromVoidList = "Visibility.RemoveFromVoidList";
	public const string LabelProviderGetWhitelistEntries = "Visibility.GetWhitelistEntries";
	public const string LabelProviderAddToWhitelist = "Visibility.AddToWhitelist";
	public const string LabelProviderRemoveFromWhitelist = "Visibility.RemoveFromWhitelist";
	public const string LabelProviderEnable = "Visibility.Enable";

	internal readonly ICallGateProvider<int>? providerApiVersion;
	internal readonly ICallGateProvider<IEnumerable<string>>? providerGetVoidListEntries;
	internal readonly ICallGateProvider<string, uint, string, object>? providerAddToVoidList;
	internal readonly ICallGateProvider<string, uint, object>? providerRemoveFromVoidList;
	internal readonly ICallGateProvider<IEnumerable<string>>? providerGetWhitelistEntries;
	internal readonly ICallGateProvider<string, uint, string, object>? providerAddToWhitelist;
	internal readonly ICallGateProvider<string, uint, object>? providerRemoveFromWhitelist;
	internal readonly ICallGateProvider<bool, object>? providerEnable;

	internal readonly IVisibilityApi api;

	public VisibilityProvider(IVisibilityApi api)
	{
		this.api = api;

		try
		{
			this.providerApiVersion = Service.PluginInterface.GetIpcProvider<int>(LabelProviderApiVersion);
			this.providerApiVersion.RegisterFunc(() => api.ApiVersion);
		}
		catch (Exception e)
		{
			Service.PluginLog.Error($"Error registering IPC provider for {LabelProviderApiVersion}:\n{e}");
		}

		try
		{
			this.providerGetVoidListEntries =
				Service.PluginInterface.GetIpcProvider<IEnumerable<string>>(LabelProviderGetVoidListEntries);
			this.providerGetVoidListEntries.RegisterFunc(api.GetVoidListEntries);
		}
		catch (Exception e)
		{
			Service.PluginLog.Error($"Error registering IPC provider for {LabelProviderGetVoidListEntries}:\n{e}");
		}

		try
		{
			this.providerAddToVoidList =
				Service.PluginInterface.GetIpcProvider<string, uint, string, object>(LabelProviderAddToVoidList);
			this.providerAddToVoidList.RegisterAction(api.AddToVoidList);
		}
		catch (Exception e)
		{
			Service.PluginLog.Error($"Error registering IPC provider for {LabelProviderAddToVoidList}:\n{e}");
		}

		try
		{
			this.providerRemoveFromVoidList =
				Service.PluginInterface.GetIpcProvider<string, uint, object>(LabelProviderRemoveFromVoidList);
			this.providerRemoveFromVoidList.RegisterAction(api.RemoveFromVoidList);
		}
		catch (Exception e)
		{
			Service.PluginLog.Error($"Error registering IPC provider for {LabelProviderRemoveFromVoidList}:\n{e}");
		}

		try
		{
			this.providerGetWhitelistEntries =
				Service.PluginInterface.GetIpcProvider<IEnumerable<string>>(LabelProviderGetWhitelistEntries);
			this.providerGetWhitelistEntries.RegisterFunc(api.GetWhitelistEntries);
		}
		catch (Exception e)
		{
			Service.PluginLog.Error($"Error registering IPC provider for {LabelProviderGetWhitelistEntries}:\n{e}");
		}

		try
		{
			this.providerAddToWhitelist =
				Service.PluginInterface.GetIpcProvider<string, uint, string, object>(LabelProviderAddToWhitelist);
			this.providerAddToWhitelist.RegisterAction(api.AddToWhitelist);
		}
		catch (Exception e)
		{
			Service.PluginLog.Error($"Error registering IPC provider for {LabelProviderAddToWhitelist}:\n{e}");
		}

		try
		{
			this.providerRemoveFromWhitelist =
				Service.PluginInterface.GetIpcProvider<string, uint, object>(LabelProviderRemoveFromWhitelist);
			this.providerRemoveFromWhitelist.RegisterAction(api.RemoveFromWhitelist);
		}
		catch (Exception e)
		{
			Service.PluginLog.Error($"Error registering IPC provider for {LabelProviderRemoveFromWhitelist}:\n{e}");
		}

		try
		{
			this.providerEnable =
				Service.PluginInterface.GetIpcProvider<bool, object>(LabelProviderEnable);
			this.providerEnable.RegisterAction(api.Enable);
			this.providerEnable?.SendMessage(true);
		}
		catch (Exception e)
		{
			Service.PluginLog.Error($"Error registering IPC provider for {LabelProviderEnable}:\n{e}");
		}
	}

	public void Dispose()
	{
		this.providerApiVersion?.UnregisterFunc();
		this.providerGetVoidListEntries?.UnregisterFunc();
		this.providerAddToVoidList?.UnregisterAction();
		this.providerRemoveFromVoidList?.UnregisterAction();
		this.providerGetWhitelistEntries?.UnregisterFunc();
		this.providerAddToWhitelist?.UnregisterAction();
		this.providerRemoveFromWhitelist?.UnregisterAction();
		this.providerEnable?.UnregisterAction();
	}
}
