using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using RespawnTimer.API.Features;
using UserSettings.ServerSpecific;

namespace RespawnTimer;

#if EXILED
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Loader;

#else
    using PluginAPI.Core;
    using PluginAPI.Core.Attributes;
    using PluginAPI.Enums;
    using PluginAPI.Events;
#endif

#if EXILED
public class RespawnTimer : Plugin<Configs.Config>
#else
    public class RespawnTimer
#endif
{
    public static RespawnTimer Singleton;
    public static string RespawnTimerDirectoryPath { get; private set; }

#if !EXILED
        [PluginConfig]
        public Configs.Config Config;
#else
    public EventHandler EventHandler;
#endif


#if EXILED
    public override void OnEnabled()
#else
        [PluginAPI.Core.Attributes.PluginPriority(LoadPriority.Medium)]
        [PluginEntryPoint("RespawnTimer", "4.2.0", "RespawnTimer", "MedveMarci")]
        private void LoadPlugin()
#endif
    {
#if !EXILED
            if (!Config.IsEnabled)
                return;
#endif

        Singleton = this;
        var rueiHelper = new RueiHelper();
#if EXILED
        RespawnTimerDirectoryPath = Path.Combine(Paths.Configs, "RespawnTimerRuei");
        EventHandler = new EventHandler();
#else
            RespawnTimerDirectoryPath = PluginHandler.Get(this).PluginDirectoryPath;
            EventManager.RegisterEvents<EventHandler>(this);
#endif

        if (!Directory.Exists(RespawnTimerDirectoryPath))
        {
            // Log.Warn("RespawnTimerRuei directory does not exist. Creating...");
            Log.Info("RespawnTimerRuei directory does not exist. Creating...");
            Directory.CreateDirectory(RespawnTimerDirectoryPath);
        }

        var exampleTimerDirectory = Path.Combine(RespawnTimerDirectoryPath, "ExampleTimerRuei");
        if (!Directory.Exists(exampleTimerDirectory))
            DownloadExampleTimer(exampleTimerDirectory);
        rueiHelper.Init();


#if EXILED
        Exiled.Events.Handlers.Player.Dying += EventHandler.OnDying;
        Exiled.Events.Handlers.Server.ReloadedConfigs += OnReloaded;
        Exiled.Events.Handlers.Player.Verified += EventHandler.OnVerified;
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += EventHandler.OnSettingValueReceived;
        Exiled.Events.Handlers.Map.Generated += EventHandler.OnGenerated;

        foreach (var plugin in Loader.Plugins)
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

        var header = new HeaderSetting(Config.SettingHeaderLabel);
        IEnumerable<SettingBase> settingBases = new SettingBase[]
        {
            header,
            new TwoButtonsSetting(1, "Visibility", "Show", "Hide", false, "Hide/Show the Timer")
        };

        SettingBase.Register(settingBases);
        SettingBase.SendToAll();
        if (!Config.ReloadTimerEachRound)
            OnReloaded();

        base.OnEnabled();
#else
            ServerSpecificSettingsSync.DefinedSettings = new ServerSpecificSettingBase[]
            {
                new SSGroupHeader(RespawnTimer.Singleton.Config.SettingHeaderLabel),
                new SSTwoButtonsSetting(1, "Visibility", "Show", "Hide", false, "Hide/Show the Timer")
            };

            ServerSpecificSettingsSync.SendToAll();
            EventHandler eventHandlerInstance = new EventHandler();
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += eventHandlerInstance.OnSettingValueReceived;
#endif
    }

    private void DownloadExampleTimer(string exampleTimerDirectory)
    {
        var exampleTimerZip = exampleTimerDirectory + ".zip";
        var exampleTimerTemp = exampleTimerDirectory + "_Temp";

        using WebClient client = new();

        // Log.Warn("Downloading ExampleTimerRuei.zip...");
        Log.Info("Downloading ExampleTimerRuei.zip...");
#if EXILED
        var url = $"https://github.com/MedveMarci/RespawnTimer/releases/download/v{Version}/ExampleTimerRuei.zip";
#else
            string url =
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

        // Log.Warn("Extracting...");
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
        Exiled.Events.Handlers.Map.Generated -= EventHandler.OnGenerated;
        Exiled.Events.Handlers.Player.Dying -= EventHandler.OnDying;
        Exiled.Events.Handlers.Server.ReloadedConfigs -= OnReloaded;
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= EventHandler.OnSettingValueReceived;
        Exiled.Events.Handlers.Player.Verified -= EventHandler.OnVerified;

        EventHandler = null;
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

        foreach (var name in Config.Timers.Values)
            TimerView.AddTimer(name);
    }

    public override string Name => "RespawnTimer";
    public override string Author => "MedveMarci";
    public override Version Version => new(4, 2, 0);
    public override Version RequiredExiledVersion => new(9, 2, 2);
    public override PluginPriority Priority => PluginPriority.Last;
#endif
}