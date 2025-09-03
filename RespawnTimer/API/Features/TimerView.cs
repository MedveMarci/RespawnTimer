#if HSM
using HintServiceMeow.Core.Models.Arguments;
#endif
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Respawning;
using RespawnTimer.Configs;
using Serialization;
using Random = UnityEngine.Random;

namespace RespawnTimer.API.Features;

public partial class TimerView
{
    public static readonly Dictionary<string, TimerView> CachedTimers = new();

    private readonly StringBuilder _stringBuilder = new(1024);

    private TimerView(string beforeRespawnString, string duringRespawnString, Properties properties, List<string> hints)
    {
        BeforeRespawnString = beforeRespawnString;
        DuringRespawnString = duringRespawnString;
        Properties = properties;
        Hints = hints;
    }

    private int HintIndex { get; set; }

    private int HintInterval { get; set; }

    private string BeforeRespawnString { get; }

    private string DuringRespawnString { get; }

    private Properties Properties { get; }

    private List<string> Hints { get; }

    public static void AddTimer(string name)
    {
        if (CachedTimers.ContainsKey(name))
            return;

        var directoryPath = Path.Combine(RespawnTimer.RespawnTimerDirectoryPath, name);
        if (!Directory.Exists(directoryPath))
        {
            LogManager.Error($"{name} directory does not exist!");
            return;
        }

        var timerBeforePath = Path.Combine(directoryPath, "TimerBeforeSpawn.txt");
        if (!File.Exists(timerBeforePath))
        {
            LogManager.Error($"{Path.GetFileName(timerBeforePath)} file does not exist!");
            return;
        }

        var timerDuringPath = Path.Combine(directoryPath, "TimerDuringSpawn.txt");
        if (!File.Exists(timerDuringPath))
        {
            LogManager.Error($"{Path.GetFileName(timerDuringPath)} file does not exist!");
            return;
        }

        var propertiesPath = Path.Combine(directoryPath, "Properties.yml");
        if (!File.Exists(propertiesPath))
        {
            LogManager.Error($"{Path.GetFileName(propertiesPath)} file does not exist! Creating...");
            File.WriteAllText(propertiesPath, YamlParser.Serializer.Serialize(new Properties()));
        }

        var hintsPath = Path.Combine(directoryPath, "Hints.txt");
        List<string> hints = [];
        if (File.Exists(hintsPath))
            hints.AddRange(File.ReadAllLines(hintsPath, Encoding.UTF8));

        TimerView timerView = new(
            File.ReadAllText(timerBeforePath, Encoding.UTF8),
            File.ReadAllText(timerDuringPath, Encoding.UTF8),
            YamlParser.Deserializer.Deserialize<Properties>(File.ReadAllText(propertiesPath, Encoding.UTF8)),
            hints);

        CachedTimers.Add(name, timerView);
    }

    public static bool TryGetTimerForPlayer(Player player, out TimerView timerView)
    {
        var groupName = !ServerStatic.PermissionsHandler.Members.TryGetValue(player.UserId, out var str) ? null : str;

        // Check by group name
        if (groupName is not null && RespawnTimer.Singleton.Config.Timers.TryGetValue(groupName, out var timerName))
        {
            timerView = CachedTimers[timerName];
            return true;
        }

        // Check by user id
        if (RespawnTimer.Singleton.Config.Timers.TryGetValue(player.UserId, out timerName))
        {
            timerView = CachedTimers[timerName];
            return true;
        }

        // Use fallback default timer
        if (RespawnTimer.Singleton.Config.Timers.TryGetValue("default", out timerName))
        {
            timerView = CachedTimers[timerName];
            return true;
        }

        // Default fallback does not exist
        timerView = null!;
        return false;
    }

#if HSM
    public string GetText(AutoContentUpdateArg arg)
    {
        arg.NextUpdateDelay = TimeSpan.FromSeconds(1);
        _stringBuilder.Clear();
        _stringBuilder.Append(
            WaveManager.State is not (WaveQueueState.WaveSelected or WaveQueueState.WaveSpawning)
                ? BeforeRespawnString
                : DuringRespawnString);
        SetAllProperties(Player.ReadyList.Count(p => p.Role is RoleTypeId.Spectator));
        _stringBuilder.Replace("{RANDOM_COLOR}", $"#{Random.Range(0x0, 0xFFFFFF):X6}");
        _stringBuilder.Replace('{', '[').Replace('}', ']');
        return _stringBuilder.ToString();
    }
#else
    public string GetText()
    {
        _stringBuilder.Clear();
        _stringBuilder.Append(
            WaveManager.State is not (WaveQueueState.WaveSelected or WaveQueueState.WaveSpawning)
                ? BeforeRespawnString
                : DuringRespawnString);
        SetAllProperties(Player.ReadyList.Count(p => p.Role is RoleTypeId.Spectator));
        _stringBuilder.Replace("{RANDOM_COLOR}", $"#{Random.Range(0x0, 0xFFFFFF):X6}");
        _stringBuilder.Replace('{', '[').Replace('}', ']');
        return _stringBuilder.ToString();
    }
#endif

    internal void IncrementHintInterval()
    {
        HintInterval++;
        if (HintInterval != Properties.HintInterval)
            return;

        HintInterval = 0;
        IncrementHintIndex();
    }

    private void IncrementHintIndex()
    {
        HintIndex++;
        if (Hints.Count == HintIndex)
            HintIndex = 0;
    }
}