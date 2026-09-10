using System;
using Dalamud.Game.Command;
using Dalamud.Bindings.ImGui;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using MouseCircle.Windows;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace MouseCircle;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    

    private const string CommandName = "/mousecircle";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("MouseCircle");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw += DrawOverlay;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.Draw -= DrawOverlay;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private Vector2 mousePosition =  Vector2.Zero;
    public void DrawOverlay()
    {
        unsafe
        {
            var input = UIInputData.Instance();
            var flags = input->CursorInputs.MouseButtonHeldFlags;
            
            //var left = flags.HasFlag(MouseButtonFlags.LBUTTON);
            var right = flags.HasFlag(MouseButtonFlags.RBUTTON);
            //var middle = flags.HasFlag(MouseButtonFlags.MBUTTON);
            
            if (!right) mousePosition = ImGui.GetMousePos();
        }
        
        DrawMouseCircle(mousePosition, new Vector4(1f, 0f, 1f, 1f));
        DrawProgressCircle(mousePosition, GetCastProgress(), new Vector4(1f, 0f, 1f, 1f));
    }

    private static unsafe float GetCastProgress()
    {
        var actionManager = ActionManager.Instance();

        var elapsed = actionManager->CastTimeElapsed;
        var total =  actionManager->CastTimeTotal;

        var casting = total > 0f && elapsed < total;
        
        return casting ? elapsed / total : 0f;
    }

    private static void DrawProgressCircle(Vector2 center, float progress, Vector4 color, float radius = 24f, float thickness = 3f)
    {
        var drawList = ImGui.GetForegroundDrawList();

        const float startAngle = -MathF.PI / 2f;
        var endAngle = startAngle + (MathF.PI * 2f * progress);
        
        drawList.PathArcTo(center, radius, startAngle, endAngle, 64);
        drawList.PathStroke(ImGui.GetColorU32(color), ImDrawFlags.None, thickness);
    }

    private static void DrawMouseCircle(Vector2 center, Vector4 color, float radius = 16f, float thickness = 3f)
    {
        var drawList = ImGui.GetForegroundDrawList();
        
        drawList.AddCircle(
            center,
            radius,
            ImGui.GetColorU32(color),
            64,
            thickness
        );

        color.W = 0.25f;
        drawList.AddCircle(
            center,
            radius,
            ImGui.GetColorU32(color),
            64,
            thickness+3f
        );
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
