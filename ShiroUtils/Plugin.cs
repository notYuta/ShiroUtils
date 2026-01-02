using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ShiroUtils.Windows;
using ShiroUtils.Modules;
using ShiroUtils.Modules.MobHunt;
using ShiroUtils.Modules.GatherMap;
using ShiroUtils.Modules.QuickTryOn;

namespace ShiroUtils;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;
    // [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    // [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;

    public static Configuration Configuration { get; private set; } = null!;
    public readonly WindowSystem WindowSystem = new("ShiroUtils");
    private readonly ConfigWindow configWindow;

    // Modules
    private readonly MobHuntModule mobHuntModule;
    private readonly GatherMapModule gatherMapModule;
    private readonly QuickTryOnModule quickTryOnModule;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        configWindow = new ConfigWindow(Configuration);
        WindowSystem.AddWindow(configWindow);

        // Modules initialization
        mobHuntModule = new MobHuntModule(PluginInterface, ClientState, DataManager, Log, AddonLifecycle, Configuration);
        gatherMapModule = new GatherMapModule(ClientState, DataManager, Log, AddonLifecycle, ObjectTable, Configuration);
        quickTryOnModule = new QuickTryOnModule(GameGui, KeyState, DataManager, Log, Configuration);
        
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += () => configWindow.IsOpen = true;
        // PluginInterface.UiBuilder.OpenMainUi += () => configWindow.IsOpen = true;
        
        Log.Information("ShiroUtils loaded successfully.");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        WindowSystem.RemoveAllWindows();
        
        configWindow.Dispose();
        
        // Modules disposal
        mobHuntModule.Dispose();
        gatherMapModule.Dispose();
        quickTryOnModule.Dispose();
    }
}
