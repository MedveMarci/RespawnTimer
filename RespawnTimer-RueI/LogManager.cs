using System;
using LabApi.Features.Console;

namespace RespawnTimer;

internal abstract class LogManager
{
    private static bool DebugEnabled => RespawnTimer.Singleton.Config.Debug;

    public static void Debug(string message)
    {
        if (!DebugEnabled)
            return;

        Logger.Raw($"[DEBUG] [{RespawnTimer.Singleton.Name}] {message}", ConsoleColor.Green);
    }

    public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        Logger.Raw($"[INFO] [{RespawnTimer.Singleton.Name}] {message}", color);
    }

    public static void Warn(string message)
    {
        Logger.Warn(message);
    }

    public static void Error(string message)
    {
        var singleton = RespawnTimer.Singleton;
        var name = singleton?.Name ?? "Unknown";
        var version = singleton?.Version.ToString() ?? "Unknown";
        Logger.Raw($"[ERROR] [{name}] Details:\nVersion: {version}\n{message}", ConsoleColor.Red);
    }
}