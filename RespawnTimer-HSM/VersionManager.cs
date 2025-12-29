using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RespawnTimer;

namespace RespawnTimer;

public static class VersionManager
{
    private const bool PreRelease = false;
    
    internal static async Task CheckForUpdatesAsync(Version currentVersion)
    {
        try
        {
            var name = RespawnTimer.Singleton.Name;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"{name}/{currentVersion}");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var repo = $"MedveMarci/{name}";
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
                foreach (var rel in allReleasesDoc.RootElement.EnumerateArray().Where(rel => rel.ValueKind == JsonValueKind.Object))
                {
                    var draft = rel.TryGetProperty("draft", out var draftProp) &&
                                draftProp.ValueKind == JsonValueKind.True;
                    if (draft) continue;

                    var prerelease = rel.TryGetProperty("prerelease", out var preProp) &&
                                     preProp.ValueKind == JsonValueKind.True;
                    if (!prerelease) continue;

                    DateTime? publishedAt = null;
                    if (rel.TryGetProperty("published_at", out var pubProp))
                    {
                        var s = pubProp.GetString();
                        if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, out var dt))
                            publishedAt = dt;
                    }

                    if (latestPre != null && (!publishedAt.HasValue ||
                                              (bestPublishedAt.HasValue && publishedAt.Value <= bestPublishedAt.Value)))
                        continue;
                    latestPre = rel;
                    bestPublishedAt = publishedAt;
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

            Version latestRemoteVersion = null;
            string latestRemoteTag = null;

            if (!PreRelease)
            {
                if (stableVer != null)
                {
                    latestRemoteVersion = stableVer;
                    latestRemoteTag = stableTag;
                }
            }
            else
            {
                if (stableVer != null && preVer != null)
                {
                    if (preVer > stableVer)
                    {
                        latestRemoteVersion = preVer;
                        latestRemoteTag = preTag;
                    }
                    else
                    {
                        latestRemoteVersion = stableVer;
                        latestRemoteTag = stableTag;
                    }
                }
                else if (preVer != null)
                {
                    latestRemoteVersion = preVer;
                    latestRemoteTag = preTag;
                }
                else if (stableVer != null)
                {
                    latestRemoteVersion = stableVer;
                    latestRemoteTag = stableTag;
                }
            }

            var currentIsNewerThanRemote = latestRemoteVersion != null && currentVersion > latestRemoteVersion;

            if (outdatedStable)
                LogManager.Info(
                    $"A new {name} version is available: {stableTag} (current {currentVersion}). Download: https://github.com/MedveMarci/{name}/releases/latest",
                    ConsoleColor.DarkRed);
            else if (prereleaseNewer)
                LogManager.Info(
                    $"A newer pre-release is available: {preTag} (current {currentVersion}). Download: https://github.com/MedveMarci/{name}/releases/tag/{preTag}",
                    ConsoleColor.DarkYellow);
            else
                LogManager.Info(
                    $"Thanks for using {name} v{currentVersion}. To get support and latest news, join to my Discord Server: https://discord.gg/KmpA8cfaSA",
                    ConsoleColor.Blue);

            if (currentIsNewerThanRemote)
            {
                var remoteLabel = string.IsNullOrWhiteSpace(latestRemoteTag) ? "the latest GitHub release" : latestRemoteTag;
                LogManager.Info(
                    $"You are running a newer version ({currentVersion}) than {remoteLabel} on GitHub. This is a development build and 100% that it contains errors or bugs.",
                    ConsoleColor.DarkMagenta);
            }

            if (PreRelease)
                LogManager.Info(
                    "This is a pre-release version. There might be bugs, if you find one, please report it on GitHub or Discord.",
                    ConsoleColor.DarkYellow);
        }
        catch (Exception e)
        {
            LogManager.Error($"Version check failed.\n{e}");
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

            var cut = t.IndexOfAny(['-', '+']);
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