using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using RespawnTimer.API.Features;
using RespawnTimer.ApiFeatures;
using UserSettings.ServerSpecific;
using Config = RespawnTimer.Configs.Config;
using Version = System.Version;

namespace RespawnTimer;

public class RespawnTimer : Plugin<Config>
{
    public static RespawnTimer Singleton;
    private EventHandler _eventHandler;
    public string githubRepo = "MedveMarci/RespawnTimer";
    public static string RespawnTimerDirectoryPath { get; private set; }

    public override string Name => "RespawnTimer";
    public override string Description => "A customizable respawn timer for SCP:SL.";
    public override string Author => "MedveMarci";
    public override Version Version => new(1, 2, 1);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);

    public override void Enable()
    {
        Singleton = this;
        if (PluginLoader.Plugins.Keys.Any(plugin =>
                plugin != this && plugin.Name.Contains("RespawnTimer", StringComparison.OrdinalIgnoreCase)))
        {
            LogManager.Error("Another instance of RespawnTimer is already loaded!");
            return;
        }

        RespawnTimerDirectoryPath = Path.Combine(PathManager.Configs.FullName, "RespawnTimer");
        _eventHandler = new EventHandler();
        if (!Directory.Exists(RespawnTimerDirectoryPath))
        {
            LogManager.Info("RespawnTimer directory does not exist. Creating...");
            Directory.CreateDirectory(RespawnTimerDirectoryPath);
        }

        var defaultTimerDirectory = Path.Combine(RespawnTimerDirectoryPath, "DefaultTimer");
        if (!Directory.Exists(defaultTimerDirectory)) DownloadDefaultTimer(defaultTimerDirectory);
        ServerEvents.WaitingForPlayers += _eventHandler.OnWaitingForPlayers;
        ServerEvents.RoundStarted += _eventHandler.OnRoundStarted;
        PlayerEvents.ChangingRole += EventHandler.OnRoleChanging;
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += EventHandler.OnSettingValueReceived;
        PlayerEvents.Joined += EventHandler.OnJoined;
        ServerEvents.WaveRespawning += EventHandler.OnWaveRespawning;
        PlayerEvents.Left += EventHandler.OnLeft;

        ServerSpecificSettingBase[] setting =
        [
            new SSGroupHeader("RespawnTimer"),
            new SSTwoButtonsSetting(1, "Timers", "Show", "Hide", false,
                "Toggle RespawnTimer for yourself.")
        ];

        if (ServerSpecificSettingsSync.DefinedSettings == null ||
            ServerSpecificSettingsSync.DefinedSettings.Length == 0)
        {
            ServerSpecificSettingsSync.DefinedSettings = setting;
        }
        else
        {
            var newSettings = new List<ServerSpecificSettingBase>(ServerSpecificSettingsSync.DefinedSettings);
            newSettings.AddRange(setting);
            ServerSpecificSettingsSync.DefinedSettings = newSettings.ToArray();
        }

        ServerSpecificSettingsSync.SendToAll();
    }

    public override void Disable()
    {
        ServerEvents.WaitingForPlayers -= _eventHandler.OnWaitingForPlayers;
        ServerEvents.RoundStarted -= _eventHandler.OnRoundStarted;
        PlayerEvents.ChangingRole -= EventHandler.OnRoleChanging;
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= EventHandler.OnSettingValueReceived;
        PlayerEvents.Joined -= EventHandler.OnJoined;
        ServerEvents.WaveRespawning -= EventHandler.OnWaveRespawning;
        PlayerEvents.Left -= EventHandler.OnLeft;
        _eventHandler = null;
        Singleton = null;
    }

    private void DownloadDefaultTimer(string defaultTimerDirectory)
    {
        var defaultTimerZip = defaultTimerDirectory + ".zip";
        var defaultTimerTemp = defaultTimerDirectory + "_Temp";
        using WebClient client = new();
        LogManager.Info("Downloading DefaultTimer.zip...");
        var url = $"https://github.com/MedveMarci/RespawnTimer/releases/download/{Version}/DefaultTimer.zip";
        try
        {
            client.DownloadFile(url, defaultTimerZip);
        }
        catch (WebException e)
        {
            if (e.Response is HttpWebResponse response)
                LogManager.Error(
                    $"Error while downloading DefaultTimer.zip: {(int)response.StatusCode} {response.StatusCode}");
            return;
        }

        LogManager.Info("DefaultTimer.zip has been downloaded!");
        LogManager.Info("Extracting...");
        ZipFile.ExtractToDirectory(defaultTimerZip, defaultTimerTemp);
        Directory.Move(Path.Combine(defaultTimerTemp, "DefaultTimer"), defaultTimerDirectory);
        Directory.Delete(defaultTimerTemp);
        File.Delete(defaultTimerZip);
        LogManager.Info("Done!");
    }


    public void OnReloaded()
    {
        if (Config != null && Config.Timers.IsEmpty())
        {
            LogManager.Error("Timer list is empty!");
            return;
        }

        TimerView.CachedTimers.Clear();
        if (Config != null)
            foreach (var name in Config.Timers.Values)
                TimerView.AddTimer(name);
        foreach (var player in Player.ReadyList) EventHandler.RefreshHint(player, player.Role);
    }
}