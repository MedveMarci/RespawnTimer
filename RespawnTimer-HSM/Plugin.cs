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
using RespawnTimer.Integrations;
using UserSettings.ServerSpecific;
using Config = RespawnTimer.Configs.Config;
using Version = System.Version;

namespace RespawnTimer;

public class RespawnTimer : Plugin<Config>
{
    public static RespawnTimer Singleton;

    private static readonly string[] RequiredFiles =
        ["TimerBeforeSpawn.txt", "TimerDuringSpawn.txt", "Hints.txt"];

    private EventHandler _eventHandler;
    public static string RespawnTimerDirectoryPath { get; private set; }

    public override string Name => "RespawnTimer-HSM";
    public override string Description => "A customizable respawn timer for SCP:SL.";
    public override string Author => "MedveMarci";
    public override Version Version => new(1, 4, 1);
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

        MigrateFromLegacy();
        EnsureTimerFiles();
        TimerView.Load();

        ServerEvents.WaitingForPlayers += _eventHandler.OnWaitingForPlayers;
        ServerEvents.RoundStarted += _eventHandler.OnRoundStarted;
        PlayerEvents.ChangingRole += EventHandler.OnRoleChanging;
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += EventHandler.OnSettingValueReceived;
        PlayerEvents.Joined += EventHandler.OnJoined;
        ServerEvents.WaveRespawning += EventHandler.OnWaveRespawning;

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
        UCR.Enable();
    }

    public override void Disable()
    {
        ServerEvents.WaitingForPlayers -= _eventHandler.OnWaitingForPlayers;
        ServerEvents.RoundStarted -= _eventHandler.OnRoundStarted;
        PlayerEvents.ChangingRole -= EventHandler.OnRoleChanging;
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= EventHandler.OnSettingValueReceived;
        PlayerEvents.Joined -= EventHandler.OnJoined;
        ServerEvents.WaveRespawning -= EventHandler.OnWaveRespawning;
        UCR.Disable();
        _eventHandler = null;
        Singleton = null;
    }

    private static void MigrateFromLegacy()
    {
        var oldDir = Path.Combine(RespawnTimerDirectoryPath, "DefaultTimer");
        if (!Directory.Exists(oldDir)) return;

        LogManager.Warn("==============================================");
        LogManager.Warn("[RespawnTimer] Legacy 'DefaultTimer' folder detected!");
        LogManager.Warn("[RespawnTimer] Migrating files to the new location...");

        var migrated = false;
        foreach (var file in Directory.GetFiles(oldDir))
        {
            var dest = Path.Combine(RespawnTimerDirectoryPath, Path.GetFileName(file));
            if (File.Exists(dest)) continue;
            File.Move(file, dest);
            LogManager.Info($"[RespawnTimer] Migrated: {Path.GetFileName(file)}");
            migrated = true;
        }

        if (!migrated)
            LogManager.Warn("[RespawnTimer] No new files to migrate (all already exist at target).");

        if (!Directory.GetFiles(oldDir).Any() && !Directory.GetDirectories(oldDir).Any())
        {
            Directory.Delete(oldDir);
            LogManager.Info("[RespawnTimer] Old DefaultTimer directory removed.");
        }

        LogManager.Warn("==============================================");
    }

    private void EnsureTimerFiles()
    {
        var missingFiles = RequiredFiles
            .Where(f => !File.Exists(Path.Combine(RespawnTimerDirectoryPath, f)))
            .ToList();

        if (missingFiles.Count == 0) return;

        DownloadTimerFiles(missingFiles);
    }

    private void DownloadTimerFiles(List<string> missingFiles)
    {
        var zipName = $"{Name}.zip";
        var zipPath = Path.Combine(RespawnTimerDirectoryPath, zipName);
        var url = $"https://github.com/MedveMarci/RespawnTimer/releases/download/{Version}/{zipName}";

        LogManager.Warn("==============================================");

        if (missingFiles.Count == RequiredFiles.Length)
        {
            LogManager.Info("[RespawnTimer] Timer files are missing. Downloading from GitHub...");
        }
        else
        {
            LogManager.Warn("[RespawnTimer] The following timer files are missing:");
            foreach (var f in missingFiles)
                LogManager.Warn($"[RespawnTimer]   - {f}");
            LogManager.Info("[RespawnTimer] Downloading missing files from GitHub...");
        }

        LogManager.Info($"[RespawnTimer] URL: {url}");

        using WebClient client = new();
        try
        {
            client.DownloadFile(url, zipPath);
        }
        catch (WebException e)
        {
            if (e.Response is HttpWebResponse response)
                LogManager.Error(
                    $"[RespawnTimer] Download failed: {(int)response.StatusCode} {response.StatusCode}");
            else
                LogManager.Error($"[RespawnTimer] Download failed: {e.Message}");
            LogManager.Warn("==============================================");
            return;
        }

        LogManager.Info($"[RespawnTimer] {zipName} downloaded! Extracting...");

        using (var archive = ZipFile.OpenRead(zipPath))
        {
            foreach (var fileName in missingFiles)
            {
                var entry = archive.GetEntry(fileName);
                if (entry == null)
                {
                    LogManager.Warn($"[RespawnTimer] '{fileName}' was not found in the archive!");
                    continue;
                }

                entry.ExtractToFile(Path.Combine(RespawnTimerDirectoryPath, fileName), false);
                LogManager.Info($"[RespawnTimer] Extracted: {fileName}");
            }
        }

        File.Delete(zipPath);
        LogManager.Info("[RespawnTimer] Done!");
        LogManager.Warn("==============================================");
    }

    public static void OnReloaded()
    {
        TimerView.Unload();
        TimerView.Load();
        foreach (var player in Player.ReadyList) EventHandler.RefreshHint(player, player.Role);
    }
}