using System;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

namespace Visibility.Managers;

public class UiManager : IDisposable
{
    private readonly WindowSystem windowSystem;
    private readonly Windows.Configuration configurationWindow;

    public UiManager(IDalamudPluginInterface pluginInterface)
    {
        this.windowSystem = new WindowSystem("VisibilityPlugin");
        this.configurationWindow = new Windows.Configuration(this.windowSystem);
        this.windowSystem.AddWindow(this.configurationWindow);

        Service.PluginInterface.UiBuilder.Draw += this.BuildUi;
        Service.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
    }

    public void Dispose()
    {
        Service.PluginInterface.UiBuilder.Draw -= this.BuildUi;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
        this.windowSystem.RemoveAllWindows();
    }

    private void BuildUi()
    {
        this.windowSystem.Draw();
    }

    public void ToggleConfigWindow()
    {
        this.configurationWindow.Toggle();
    }

    public void OpenConfigUi()
    {
        this.ToggleConfigWindow();
    }
}
