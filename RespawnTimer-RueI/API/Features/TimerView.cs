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
    public static TimerView Instance { get; private set; }

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
    internal string BeforeRespawnString { get; }
    internal string DuringRespawnString { get; }
    private Properties Properties { get; }
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

        var hintsPath = Path.Combine(directoryPath, "Hints.txt");
        List<string> hints = [];
        if (File.Exists(hintsPath))
            hints.AddRange(File.ReadAllLines(hintsPath, Encoding.UTF8));

        Instance = new TimerView(
            File.ReadAllText(timerBeforePath, Encoding.UTF8),
            File.ReadAllText(timerDuringPath, Encoding.UTF8),
            YamlParser.Deserializer.Deserialize<Properties>(File.ReadAllText(propertiesPath, Encoding.UTF8)),
            hints);
    }

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
