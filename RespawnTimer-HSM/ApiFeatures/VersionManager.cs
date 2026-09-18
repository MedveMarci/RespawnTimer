using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace RespawnTimer.ApiFeatures;

internal static class VersionManager
{
    private const string ApiBase = "https://bearmanapi.hu";
    private const string SupportUrl = "https://discord.gg/KmpA8cfaSA";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);

    internal static void CheckForUpdates()
    {
        Task.Run(async () =>
        {
            const string name = "RespawnTimer";
            Version current = RespawnTimer.Singleton.Version;

            try
            {
                (Version Version, string DownloadUrl)? latest = await FetchLatestVersion(name);
                if (latest is null)
                    return;

                if (await IsCurrentVersionRecalled(name, current, latest.Value.Version))
                    return;

                ReportVersionStatus(name, current, latest.Value.Version, latest.Value.DownloadUrl);
            }
            catch (TimeoutException)
            {
                LogManager.Error("Version check timed out.");
            }
            catch (Exception ex)
            {
                LogManager.Error("Version check failed.");
                LogManager.Debug($"Version check exception:\n{ex}");
            }
        });
    }

    private static async Task<(Version Version, string DownloadUrl)?> FetchLatestVersion(string name)
    {
        string resp = await WithTimeout(HttpQuery.GetAsync($"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/latest"));

        (HttpStatusCode code, _) = ParseResponse(resp);
        if (code != HttpStatusCode.OK)
        {
            LogManager.Error($"Version check failed: {code}");
            return null;
        }

        JsonElement root = JsonDocument.Parse(resp).RootElement;
        if (!root.TryGetProperty("version", out JsonElement vProp) || vProp.ValueKind != JsonValueKind.String || !Version.TryParse(vProp.GetString() ?? string.Empty, out Version latest))
        {
            LogManager.Error("Version check: invalid response format.");
            return null;
        }

        return (latest, GetDownloadUrl(root));
    }

    private static async Task<bool> IsCurrentVersionRecalled(string name, Version current, Version latest)
    {
        string resp = await WithTimeout(HttpQuery.GetAsync($"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/version/{Uri.EscapeDataString(current.ToString())}"));

        JsonElement root = JsonDocument.Parse(resp).RootElement;
        if (!root.TryGetProperty("is_recalled", out JsonElement recalled) || recalled.ValueKind != JsonValueKind.True)
            return false;

        string reason = root.TryGetProperty("recall_reason", out JsonElement r) && r.ValueKind == JsonValueKind.String ? r.GetString() : "No reason provided.";

        LogManager.Error($"This version of {name} has been recalled! Update to {latest} ASAP.\nReason: {reason}", ConsoleColor.DarkRed);
        return true;
    }

    private static void ReportVersionStatus(string name, Version current, Version latest, string downloadUrl)
    {
        if (latest > current)
            LogManager.Info($"New version of {name} available: {latest} (you have {current}). {downloadUrl}".TrimEnd(), ConsoleColor.DarkRed);
        else if (current > latest)
            LogManager.Info($"You are running a newer version of {name} ({current}) than {latest}. " + "This is a development/pre-release build and it can contain errors or bugs.", ConsoleColor.DarkMagenta);
        else
            LogManager.Info($"Thank you for using {name} v{current}. Support: {SupportUrl}", ConsoleColor.Blue);
    }

    private static async Task<string> WithTimeout(Task<string> task)
    {
        Task completed = await Task.WhenAny(task, Task.Delay(RequestTimeout));
        if (completed != task)
            throw new TimeoutException();
        return await task;
    }

    private static (HttpStatusCode code, string message) ParseResponse(string json)
    {
        try
        {
            JsonElement root = JsonDocument.Parse(json).RootElement;
            HttpStatusCode code = root.TryGetProperty("status", out JsonElement s) && s.ValueKind == JsonValueKind.Number ? (HttpStatusCode)s.GetInt32() : HttpStatusCode.InternalServerError;
            string msg = root.TryGetProperty("message", out JsonElement m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
            return (code, msg);
        }
        catch
        {
            return (HttpStatusCode.InternalServerError, null);
        }
    }

    private static string GetDownloadUrl(JsonElement root)
    {
        return root.TryGetProperty("download_url", out JsonElement d) && d.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(d.GetString()) ? $"Download: {d.GetString()}" : string.Empty;
    }
}