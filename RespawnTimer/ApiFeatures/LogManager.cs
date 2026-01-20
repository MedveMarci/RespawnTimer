using System;
using System.Collections.Generic;
using LabApi.Features.Console;
using LabApi.Loader.Features.Yaml;
using NorthwoodLib.Pools;
using RespawnTimer.API.Features;

namespace RespawnTimer.ApiFeatures;

internal static class LogManager
{
    private static readonly List<LogEntry> History = [];
    private static bool DebugEnabled => RespawnTimer.Singleton.Config?.Debug ?? false;

    public static void Debug(string message)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Debug", message));
        if (!DebugEnabled)
            return;

        Logger.Raw($"[DEBUG] [{RespawnTimer.Singleton.Name}] {message}", ConsoleColor.Green);
    }

    public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Info", message));
        Logger.Raw($"[INFO] [{RespawnTimer.Singleton.Name}] {message}", color);
    }

    public static void Warn(string message)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Warn", message));
        Logger.Warn(message);
    }

    public static void Error(string message, ConsoleColor color = ConsoleColor.Red)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Error", message));
        Logger.Raw($"[ERROR] [{RespawnTimer.Singleton.Name}] {message}", color);
    }

    public static (string logResult, bool success) GetLogHistory()
    {
        var stringBuilder = StringBuilderPool.Shared.Rent();
        foreach (var log in History)
            stringBuilder.AppendLine(
                $"[{DateTimeOffset.FromUnixTimeMilliseconds(log.Timestamp):yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}");

        if (RespawnTimer.Singleton.Config != null)
        {
            stringBuilder.AppendLine("\n--- RespawnTimer Config ---\n");
            stringBuilder.Append($"{YamlConfigParser.Serializer.Serialize(RespawnTimer.Singleton.Config)}");
        }

        stringBuilder.AppendLine("\n--- RespawnTimer Timers ---\n");
        foreach (var timerView in TimerView.CachedTimers)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(
                $"{timerView.Key}:\nBeforeRespawn:\n{timerView.Value.BeforeRespawnString}\nDuringRespawn:\n{timerView.Value.DuringRespawnString}\n");
        }

        var logId = ApiManager.SendLogsAsync(StringBuilderPool.Shared.ToStringReturn(stringBuilder));
        return logId == null
            ? ("Failed to send LogHistory.", false)
            : ($"Log history sent, received id: {logId}", true);
    }

    private class LogEntry(long timestamp, string level, string message)
    {
        public long Timestamp { get; } = timestamp;
        public string Level { get; } = level;
        public string Message { get; } = message;
    }
}