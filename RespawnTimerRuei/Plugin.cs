namespace RespawnTimerRuei;

using System.IO;
using System.IO.Compression;
using System.Net;
#endif
#if EXILED
public class RespawnTimerRuei : Plugin<Configs.Config>
#else
public class RespawnTimerRuei
#endif
{
    public static RespawnTimerRuei Singleton;
    public static string RespawnTimerDirectoryPath { get; private set; }
#if NWAPI
    [PluginConfig] public Configs.Config Config;
#else
    private EventHandler _eventHandler;
#endif
#if EXILED
    public override void OnEnabled()
#else
    [PluginPriority(LoadPriority.Lowest)]
    [PluginEntryPoint("RespawnTimerRuei", "4.3.0", "RespawnTimerRuei", "MedveMarci")]
    private void LoadPlugin()
#endif
    {
#if NWAPI
        if (!Config.IsEnabled) return;
#endif
        Singleton = this;
#if EXILED
        RespawnTimerDirectoryPath = Path.Combine(Paths.Configs, "RespawnTimerRuei");
        _eventHandler = new EventHandler();
#else
        RespawnTimerDirectoryPath = PluginHandler.Get(this).PluginDirectoryPath;
        EventManager.RegisterEvents<EventHandler>(this);
#endif
        if (!Directory.Exists(RespawnTimerDirectoryPath))
        {
            Log.Info("RespawnTimerRuei directory does not exist. Creating...");
            Directory.CreateDirectory(RespawnTimerDirectoryPath);
        }

        var exampleTimerDirectory = Path.Combine(RespawnTimerDirectoryPath, "ExampleTimerRuei");
        if (!Directory.Exists(exampleTimerDirectory)) DownloadExampleTimer(exampleTimerDirectory);
        RueiHelper.Init();
#if EXILED
        Exiled.Events.Handlers.Map.Generated += _eventHandler.OnGenerated;
        Exiled.Events.Handlers.Player.Dying += _eventHandler.OnDying;
        Exiled.Events.Handlers.Server.ReloadedConfigs += OnReloaded;
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += EventHandler.OnSettingValueReceived;
        Exiled.Events.Handlers.Player.Verified += EventHandler.OnVerified;
        Exiled.Events.Handlers.Server.RespawnedTeam += EventHandler.OnRespawnedTeam;
        foreach (var plugin in Loader.Plugins)
        {
            switch (plugin.Name)
            {
                case "Serpents Hand" when plugin.Config.IsEnabled:
                    API.API.SerpentsHandTeam.Init(plugin.Assembly);
                    Log.Debug("Serpents Hand plugin detected!");
                    break;
                case "UIURescueSquad" when plugin.Config.IsEnabled:
                    API.API.UiuTeam.Init(plugin.Assembly);
                    Log.Debug("UIURescueSquad plugin detected!");
                    break;
            }
        }

        var header = new HeaderSetting(Config.SettingHeaderLabel);
        IEnumerable<SettingBase> settingBases = new SettingBase[]
        {
            header, new TwoButtonsSetting(1, "Visibility", "Show", "Hide", false, "Hide/Show the Timer"),
        };
        SettingBase.Register(settingBases);
        SettingBase.SendToAll();
        if (!Config.ReloadTimerEachRound) OnReloaded();
        base.OnEnabled();
#else
        ServerSpecificSettingsSync.DefinedSettings = new ServerSpecificSettingBase[]
        {
            new SSGroupHeader(Singleton.Config.SettingHeaderLabel),
            new SSTwoButtonsSetting(1, "Visibility", "Show", "Hide", false, "Hide/Show the Timer")
        };
        ServerSpecificSettingsSync.SendToAll();
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += EventHandler.OnSettingValueReceived;
#endif
    }

    private void DownloadExampleTimer(string exampleTimerDirectory)
    {
        var exampleTimerZip = exampleTimerDirectory + ".zip";
        var exampleTimerTemp = exampleTimerDirectory + "_Temp";
        using WebClient client = new();
        Log.Info("Downloading ExampleTimerRuei.zip...");
#if EXILED
        var url = $"https://github.com/MedveMarci/RespawnTimer/releases/download/v{Version}/ExampleTimerRuei.zip";
#else
        var url =
            $"https://github.com/MedveMarci/RespawnTimer/releases/download/v{PluginHandler.Get(this).PluginVersion}/ExampleTimerRuei.zip";
#endif
        try
        {
            client.DownloadFile(url, exampleTimerZip);
        }
        catch (WebException e)
        {
            if (e.Response is HttpWebResponse response)
                Log.Error(
                    $"Error while downloading ExampleTimerRuei.zip: {(int)response.StatusCode} {response.StatusCode}");
            return;
        }

        Log.Info("ExampleTimerRuei.zip has been downloaded!");
        Log.Info("Extracting...");
        ZipFile.ExtractToDirectory(exampleTimerZip, exampleTimerTemp);
        Directory.Move(Path.Combine(exampleTimerTemp, "ExampleTimerRuei"), exampleTimerDirectory);
        Directory.Delete(exampleTimerTemp);
        File.Delete(exampleTimerZip);
        Log.Info("Done!");
    }

#if EXILED
    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Map.Generated -= _eventHandler.OnGenerated;
        Exiled.Events.Handlers.Player.Dying -= _eventHandler.OnDying;
        Exiled.Events.Handlers.Server.ReloadedConfigs -= OnReloaded;
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= EventHandler.OnSettingValueReceived;
        Exiled.Events.Handlers.Player.Verified -= EventHandler.OnVerified;
        Exiled.Events.Handlers.Server.RespawnedTeam -= EventHandler.OnRespawnedTeam;
        _eventHandler = null;
        Singleton = null;
        base.OnDisabled();
    }

    public override void OnReloaded()
    {
        if (Config.Timers.IsEmpty())
        {
            Log.Error("Timer list is empty!");
            return;
        }

        TimerView.CachedTimers.Clear();
        foreach (var name in Config.Timers.Values) TimerView.AddTimer(name);
    }

    public override string Name => "RespawnTimerRuei";
    public override string Author => "MedveMarci";
    public override Version Version => new(4, 3, 0);
    public override Version RequiredExiledVersion => new(9, 5, 0);
    public override PluginPriority Priority => PluginPriority.Last;
#endif
}