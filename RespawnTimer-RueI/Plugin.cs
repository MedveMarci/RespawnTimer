using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public override string Name => "RespawnTimer-RueI";
    public override string Description => "A customizable respawn timer for SCP:SL.";
    public override string Author => "MedveMarci";
    public override Version Version => new(1, 5, 0);
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

    private void MigrateFromLegacy()
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

    private static void EnsureTimerFiles()
    {
        var missingFiles = RequiredFiles
            .Where(f => !File.Exists(Path.Combine(RespawnTimerDirectoryPath, f)))
            .ToList();

        if (missingFiles.Count == 0) return;

        GenerateTimerFiles(missingFiles);
    }

    private static void GenerateTimerFiles(List<string> missingFiles)
    {
        LogManager.Warn("==============================================");

        if (missingFiles.Count == RequiredFiles.Length)
        {
            LogManager.Info("[RespawnTimer] Timer files are missing. Generating the defaults...");
        }
        else
        {
            LogManager.Warn("[RespawnTimer] The following timer files are missing:");
            foreach (var fileName in missingFiles)
                LogManager.Warn($"[RespawnTimer]   - {fileName}");
            LogManager.Info("[RespawnTimer] Generating them with their default contents...");
        }

        foreach (var fileName in missingFiles)
        {
            if (!DefaultTimerFiles.Contents.TryGetValue(fileName, out var content))
            {
                LogManager.Error($"[RespawnTimer] No default content is known for '{fileName}'!");
                continue;
            }

            try
            {
                File.WriteAllText(Path.Combine(RespawnTimerDirectoryPath, fileName), content);
                LogManager.Info($"[RespawnTimer] Generated: {fileName}");
            }
            catch (Exception e)
            {
                LogManager.Error($"[RespawnTimer] Failed to generate '{fileName}': {e.Message}");
            }
        }

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