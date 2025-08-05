using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using RespawnTimer.API.Features;
using UserSettings.ServerSpecific;
using Config = RespawnTimer.Configs.Config;
using Version = System.Version;

namespace RespawnTimer;

public class RespawnTimer : Plugin<Config>
{
    public static RespawnTimer Singleton;
    private EventHandler _eventHandler;
    public static string RespawnTimerDirectoryPath { get; private set; }

    public override string Name => "RespawnTimer";
    public override string Description => "Respawn epic Timer";
    public override string Author => "MedveMarci";
    public override Version Version => new(2025, 8, 4, 1);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);

    public override void Enable()
    {
        Singleton = this;
        RespawnTimerDirectoryPath = Path.Combine(PathManager.Configs.FullName, "RespawnTimer");
        _eventHandler = new EventHandler();
        if (!Directory.Exists(RespawnTimerDirectoryPath))
        {
            Logger.Info("RespawnTimer directory does not exist. Creating...");
            Directory.CreateDirectory(RespawnTimerDirectoryPath);
        }
        var defaultTimerDirectory = Path.Combine(RespawnTimerDirectoryPath, "DefaultTimer");
        if (!Directory.Exists(defaultTimerDirectory)) DownloadDefaultTimer(defaultTimerDirectory);
        ServerEvents.MapGenerated += _eventHandler.OnMapGenerated;
        ServerEvents.RoundStarted += _eventHandler.OnRoundStarted;
        PlayerEvents.ChangingRole += EventHandler.OnRoleChanging;
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += EventHandler.OnSettingValueReceived;
        PlayerEvents.Joined += EventHandler.OnJoined;
        ServerEvents.WaveRespawning += EventHandler.OnWaveRespawning;

        ServerSpecificSettingBase[] setting =
        [
            new SSGroupHeader("RespawnTimer"),
            new SSTwoButtonsSetting(1, "Timers", "Show", "Hide", false,
                "Toggle RespawnTimer for yourself."),
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
        ServerEvents.MapGenerated -= _eventHandler.OnMapGenerated;
        ServerEvents.RoundStarted -= _eventHandler.OnRoundStarted;
        PlayerEvents.ChangingRole -= EventHandler.OnRoleChanging;
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= EventHandler.OnSettingValueReceived;
        PlayerEvents.Joined -= EventHandler.OnJoined;
        ServerEvents.WaveRespawning -= EventHandler.OnWaveRespawning;
        _eventHandler = null;
        Singleton = null;
    }

    private void DownloadDefaultTimer(string defaultTimerDirectory)
    {
        var defaultTimerZip = defaultTimerDirectory + ".zip";
        var defaultTimerTemp = defaultTimerDirectory + "_Temp";
        using WebClient client = new();
        Logger.Info("Downloading DefaultTimer.zip...");
        var url = $"https://github.com/MedveMarci/RespawnTimer/releases/download/v{Version}/DefaultTimer.zip";
        try
        {
            client.DownloadFile(url, defaultTimerZip);
        }
        catch (WebException e)
        {
            if (e.Response is HttpWebResponse response)
                Logger.Error(
                    $"Error while downloading DefaultTimer.zip: {(int)response.StatusCode} {response.StatusCode}");
            return;
        }

        Logger.Info("DefaultTimer.zip has been downloaded!");
        Logger.Info("Extracting...");
        ZipFile.ExtractToDirectory(defaultTimerZip, defaultTimerTemp);
        Directory.Move(Path.Combine(defaultTimerTemp, "DefaultTimer"), defaultTimerDirectory);
        Directory.Delete(defaultTimerTemp);
        File.Delete(defaultTimerZip);
        Logger.Info("Done!");
    }

    
    public void OnReloaded()
    {
        if (Config != null && Config.Timers.IsEmpty())
        {
            Logger.Error("Timer list is empty!");
            return;
        }

        TimerView.CachedTimers.Clear();
        if (Config != null)
            foreach (var name in Config.Timers.Values)
                TimerView.AddTimer(name);
        foreach (var player in Player.ReadyList) EventHandler.RefreshHint(player, player.Role);
    }
}