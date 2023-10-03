using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Visibility;

public class Service
{
		[PluginService]
		public static DalamudPluginInterface PluginInterface { get; set; } = null!;

		[PluginService]
		public static ICommandManager CommandManager { get; set; } = null!;

		[PluginService]
		public static IChatGui ChatGui { get; set; } = null!;

		[PluginService]
		public static IDataManager DataManager { get; set; } = null!;

		[PluginService]
		public static IGameGui GameGui { get; set; } = null!;

		[PluginService]
		public static IClientState ClientState { get; set; } = null!;

		[PluginService]
		public static IFramework Framework { get; set; } = null!;

		[PluginService]
		public static IObjectTable ObjectTable { get; set; } = null!;

		[PluginService]
		public static ICondition Condition { get; set; } = null!;
		
		[PluginService]
		public static IPluginLog PluginLog { get; set; } = null!;
}