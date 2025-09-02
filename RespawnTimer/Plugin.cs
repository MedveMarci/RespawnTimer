using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using System.Text.Json;
using RespawnTimer.API.Features;
using UserSettings.ServerSpecific;
using Config = RespawnTimer.Configs.Config;
using Version = System.Version;

namespace RespawnTimer;

public class RespawnTimer : Plugin<Config>
{
    private const bool PreRelease = false;
    public static RespawnTimer Singleton;
    private EventHandler _eventHandler;
    public static string RespawnTimerDirectoryPath { get; private set; }

    public override string Name => "RespawnTimer";
    public override string Description => "Respawn epic Timer";
    public override string Author => "MedveMarci";
    public override Version Version => new(1, 0, 0);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);

    public override void Enable()
    {
        Singleton = this;
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
        ServerEvents.WaitingForPlayers -= _eventHandler.OnWaitingForPlayers;
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

    internal static async Task CheckForUpdatesAsync(Version currentVersion)
    {
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"{Singleton.Name}/{currentVersion}");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var repo = $"MedveMarci/{Singleton.Name}";
            var latestStableJson = await client.GetStringAsync($"https://api.github.com/repos/{repo}/releases/latest")
                .ConfigureAwait(false);
            var allReleasesJson = await client
                .GetStringAsync($"https://api.github.com/repos/{repo}/releases?per_page=20").ConfigureAwait(false);

            using var latestStableDoc = JsonDocument.Parse(latestStableJson);
            using var allReleasesDoc = JsonDocument.Parse(allReleasesJson);

            var latestStableRoot = latestStableDoc.RootElement;
            string stableTag = null;
            if (latestStableRoot.TryGetProperty("tag_name", out var tagProp))
                stableTag = tagProp.GetString();
            var stableVer = ParseVersion(stableTag);

            JsonElement? latestPre = null;
            Version preVer = null;
            string preTag = null;

            if (allReleasesDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                DateTime? bestPublishedAt = null;
                foreach (var rel in allReleasesDoc.RootElement.EnumerateArray())
                {
                    if (rel.ValueKind != JsonValueKind.Object) continue;

                    bool draft = rel.TryGetProperty("draft", out var draftProp) && draftProp.ValueKind == JsonValueKind.True;
                    if (draft) continue;

                    bool prerelease = rel.TryGetProperty("prerelease", out var preProp) && preProp.ValueKind == JsonValueKind.True;
                    if (!prerelease) continue;

                    DateTime? publishedAt = null;
                    if (rel.TryGetProperty("published_at", out var pubProp))
                    {
                        var s = pubProp.GetString();
                        if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, out var dt))
                            publishedAt = dt;
                    }

                    if (latestPre == null)
                    {
                        latestPre = rel;
                        bestPublishedAt = publishedAt;
                    }
                    else
                    {
                        if (publishedAt.HasValue && (!bestPublishedAt.HasValue || publishedAt.Value > bestPublishedAt.Value))
                        {
                            latestPre = rel;
                            bestPublishedAt = publishedAt;
                        }
                    }
                }
            }

            if (latestPre.HasValue)
            {
                if (latestPre.Value.TryGetProperty("tag_name", out var preTagProp))
                    preTag = preTagProp.GetString();
                preVer = ParseVersion(preTag);
            }

            var outdatedStable = stableVer != null && stableVer > currentVersion;
            var prereleaseNewer = preVer != null && preVer > currentVersion && !outdatedStable;

            if (outdatedStable)
                LogManager.Info(
                    $"A new {Singleton.Name} version is available: {stableTag} (current {currentVersion}). Download: https://github.com/MedveMarci/{Singleton.Name}/releases/latest",
                    ConsoleColor.DarkRed);
            else if (prereleaseNewer)
                LogManager.Info(
                    $"A newer pre-release is available: {preTag} (current {currentVersion}). Download: https://github.com/MedveMarci/{Singleton.Name}/releases/tag/{preTag}",
                    ConsoleColor.DarkYellow);
            else
                LogManager.Info($"Thanks for using {Singleton.Name} v{currentVersion}. To get support and latest news, join to my Discord Server: https://discord.gg/KmpA8cfaSA", ConsoleColor.Blue);
            if (PreRelease)
                LogManager.Info(
                    "This is a pre-release version. There might be bugs, if you find one, please report it on GitHub or Discord.",
                    ConsoleColor.DarkYellow);
        }
        catch (Exception e)
        {
            LogManager.Debug($"Version check failed.\n{e}");
        }
    }

    private static Version ParseVersion(string tag)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tag)) return null;
                var t = tag.Trim();
                if (t.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    t = t.Substring(1);

                var cut = t.IndexOfAny(new[] { '-', '+' });
                if (cut >= 0)
                    t = t.Substring(0, cut);

                return Version.TryParse(t, out var v) ? v : null;
            }
            catch
            {
                return null;
            }
        }
}