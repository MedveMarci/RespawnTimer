using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Respawning;
using RespawnTimer.ApiFeatures;
using RespawnTimer.Configs;
using Serialization;
using Random = UnityEngine.Random;

namespace RespawnTimer.API.Features;

public partial class TimerView
{
    private readonly StringBuilder _stringBuilder = new(1024);

    internal TimerView(string beforeRespawnString, string duringRespawnString, Properties properties,
        List<string> hints)
    {
        BeforeRespawnString = beforeRespawnString;
        DuringRespawnString = duringRespawnString;
        Properties = properties;
        Hints = hints;
    }

    public static TimerView Instance { get; private set; }

    private int HintIndex { get; set; }
    private int HintInterval { get; set; }
    internal string BeforeRespawnString { get; }
    internal string DuringRespawnString { get; }
    internal Properties Properties { get; }
    private List<string> Hints { get; }

    public static void Load()
    {
        var directoryPath = RespawnTimer.RespawnTimerDirectoryPath;

        var timerBeforePath = Path.Combine(directoryPath, "TimerBeforeSpawn.txt");
        if (!File.Exists(timerBeforePath))
        {
            LogManager.Error("TimerBeforeSpawn.txt does not exist!");
            return;
        }

        var timerDuringPath = Path.Combine(directoryPath, "TimerDuringSpawn.txt");
        if (!File.Exists(timerDuringPath))
        {
            LogManager.Error("TimerDuringSpawn.txt does not exist!");
            return;
        }

        var propertiesPath = Path.Combine(directoryPath, "Properties.yml");
        if (!File.Exists(propertiesPath))
        {
            LogManager.Warn("Properties.yml does not exist! Creating...");
            File.WriteAllText(propertiesPath, YamlParser.Serializer.Serialize(new Properties()));
        }

        var propertiesText = File.ReadAllText(propertiesPath, Encoding.UTF8);
        var properties = YamlParser.Deserializer.Deserialize<Properties>(propertiesText);
        if (EnsureProperties(properties, propertiesText))
        {
            LogManager.Warn("Properties.yml was missing some entries. Adding missing defaults...");
            File.WriteAllText(propertiesPath, YamlParser.Serializer.Serialize(properties));
        }

        var hintsPath = Path.Combine(directoryPath, "Hints.txt");
        List<string> hints = [];
        if (File.Exists(hintsPath))
            hints.AddRange(File.ReadAllLines(hintsPath, Encoding.UTF8));

        Instance = new TimerView(
            File.ReadAllText(timerBeforePath, Encoding.UTF8),
            File.ReadAllText(timerDuringPath, Encoding.UTF8),
            properties,
            hints);
    }

    private static bool EnsureProperties(Properties properties, string propertiesText)
    {
        var changed = false;
        var raw = YamlParser.Deserializer.Deserialize<Dictionary<object, object>>(propertiesText) ?? new();
        var rawKeys = new HashSet<string>(raw.Keys.Select(key => NormalizeKey(key.ToString())), StringComparer.OrdinalIgnoreCase);

        foreach (var property in typeof(Properties).GetProperties())
            if (!rawKeys.Contains(NormalizeKey(property.Name)))
                changed = true;

        foreach (var entry in new Properties().WarheadStatus)
        {
            if (properties.WarheadStatus.ContainsKey(entry.Key)) continue;
            properties.WarheadStatus[entry.Key] = entry.Value;
            changed = true;
        }

        return changed;
    }

    // Properties.yml is serialized with an underscored naming convention (e.g. "leading_zeros"),
    // so strip underscores before comparing against the PascalCase property names.
    private static string NormalizeKey(string key) => key?.Replace("_", string.Empty) ?? string.Empty;

    public static void Unload()
    {
        Instance = null;
    }

    public string GetText(ReferenceHub hub)
    {
        _stringBuilder.Clear();
        _stringBuilder.Append(
            WaveManager.State is not (WaveQueueState.WaveSelected or WaveQueueState.WaveSpawning)
                ? BeforeRespawnString
                : DuringRespawnString);
        SetAllProperties(hub, Player.ReadyList.Count(p => p.Role is RoleTypeId.Spectator));
        _stringBuilder.Replace("{RANDOM_COLOR}", $"#{Random.Range(0x0, 0xFFFFFF):X6}");
        _stringBuilder.Replace('{', '[').Replace('}', ']');
        return _stringBuilder.ToString();
    }

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