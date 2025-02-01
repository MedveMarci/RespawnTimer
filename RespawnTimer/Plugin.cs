using UserSettings.ServerSpecific;

namespace RespawnTimer
{
    using System.IO;
    using System.IO.Compression;
    using System.Net;
#if EXILED
    using API.Features;
    using System;
    using System.Collections.Generic;
    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.Loader;
    using Exiled.API.Features.Core.UserSettings;
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
        private EventHandler _eventHandler;
#endif


#if EXILED
        public override void OnEnabled()
#else
        [PluginPriority(LoadPriority.Lowest)]
        [PluginEntryPoint("RespawnTimer", "4.3.0", "RespawnTimer", "MedveMarci")]
        private void LoadPlugin()
#endif
        {
#if !EXILED
            if (!Config.IsEnabled)
                return;
#endif

            Singleton = this;
#if EXILED
            RespawnTimerDirectoryPath = Path.Combine(Paths.Configs, "RespawnTimer");
            _eventHandler = new EventHandler();
#else
            RespawnTimerDirectoryPath = PluginHandler.Get(this).PluginDirectoryPath;
            EventManager.RegisterEvents<EventHandler>(this);
#endif

            if (!Directory.Exists(RespawnTimerDirectoryPath))
            {
                Log.Info("RespawnTimer directory does not exist. Creating...");
                Directory.CreateDirectory(RespawnTimerDirectoryPath);
            }

            var exampleTimerDirectory = Path.Combine(RespawnTimerDirectoryPath, "ExampleTimer");
            if (!Directory.Exists(exampleTimerDirectory))
                DownloadExampleTimer(exampleTimerDirectory);

#if EXILED
            Exiled.Events.Handlers.Map.Generated += _eventHandler.OnGenerated;
            Exiled.Events.Handlers.Server.RoundStarted += _eventHandler.OnRoundStart;
            Exiled.Events.Handlers.Player.Dying += _eventHandler.OnDying;
            Exiled.Events.Handlers.Server.ReloadedConfigs += OnReloaded;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += EventHandler.OnSettingValueReceived;
            Exiled.Events.Handlers.Player.Verified += EventHandler.OnVerified;

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
                header,
                new TwoButtonsSetting(1, "Visibility", "Show", "Hide", false, "Hide/Show the Timer"),
            };

            SettingBase.Register(settingBases);
            SettingBase.SendToAll();
            if (!Config.ReloadTimerEachRound)
                OnReloaded();

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
            
            Log.Info("Downloading ExampleTimer.zip...");
#if EXILED
            var url = $"https://github.com/MedveMarci/RespawnTimer/releases/download/v{Version}/ExampleTimer.zip";
#else
            string url = $"https://github.com/MedveMarci/RespawnTimer/releases/download/v{PluginHandler.Get(this).PluginVersion}/ExampleTimer.zip";
#endif
            try
            {
                client.DownloadFile(url, exampleTimerZip);
            }
            catch (WebException e)
            {
                if (e.Response is HttpWebResponse response)
                    Log.Error($"Error while downloading ExampleTimer.zip: {(int)response.StatusCode} {response.StatusCode}");
                
                return;
            }

            Log.Info("ExampleTimer.zip has been downloaded!");
            
            Log.Info("Extracting...");
            ZipFile.ExtractToDirectory(exampleTimerZip, exampleTimerTemp);
            Directory.Move(Path.Combine(exampleTimerTemp, "ExampleTimer"), exampleTimerDirectory);

            Directory.Delete(exampleTimerTemp);
            File.Delete(exampleTimerZip);

            Log.Info("Done!");
        }

#if EXILED
        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Map.Generated -= _eventHandler.OnGenerated;
            Exiled.Events.Handlers.Server.RoundStarted -= _eventHandler.OnRoundStart;
            Exiled.Events.Handlers.Player.Dying -= _eventHandler.OnDying;
            Exiled.Events.Handlers.Server.ReloadedConfigs -= OnReloaded;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= EventHandler.OnSettingValueReceived;
            Exiled.Events.Handlers.Player.Verified -= EventHandler.OnVerified;

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

            foreach (var name in Config.Timers.Values)
                TimerView.AddTimer(name);
        }

        public override string Name => "RespawnTimer";
        public override string Author => "MedveMarci";
        public override Version Version => new(4, 3, 0);
        public override Version RequiredExiledVersion => new(9, 5,0);
        public override PluginPriority Priority => PluginPriority.Last;
#endif
    }
}