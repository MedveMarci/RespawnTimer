namespace RespawnTimer.API.Features
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Configs;
    using Respawning;
    using Serialization;
    using UnityEngine;
#if EXILED
    using Exiled.API.Features;
#else
    using PluginAPI.Core;
#endif

    public partial class TimerView
    {
        public static readonly Dictionary<string, TimerView> CachedTimers = new();

        private int HintIndex { get; set; }

        private int HintInterval { get; set; }

        public static void AddTimer(string name)
        {
            if (CachedTimers.ContainsKey(name))
                return;

            var directoryPath = Path.Combine(RespawnTimer.RespawnTimerDirectoryPath, name);
            if (!Directory.Exists(directoryPath))
            {
                Log.Error($"{name} directory does not exist!");
                return;
            }

            var timerBeforePath = Path.Combine(directoryPath, "TimerBeforeSpawn.txt");
            if (!File.Exists(timerBeforePath))
            {
                Log.Error($"{Path.GetFileName(timerBeforePath)} file does not exist!");
                return;
            }

            var timerDuringPath = Path.Combine(directoryPath, "TimerDuringSpawn.txt");
            if (!File.Exists(timerDuringPath))
            {
                Log.Error($"{Path.GetFileName(timerDuringPath)} file does not exist!");
                return;
            }

            var propertiesPath = Path.Combine(directoryPath, "Properties.yml");
            if (!File.Exists(propertiesPath))
            {
                Log.Error($"{Path.GetFileName(propertiesPath)} file does not exist! Creating...");
                File.WriteAllText(propertiesPath, YamlParser.Serializer.Serialize(new Properties()));
            }

            var hintsPath = Path.Combine(directoryPath, "Hints.txt");
            List<string> hints = new();
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
            var groupName = !ServerStatic.PermissionsHandler._members.TryGetValue(player.UserId, out var str) ? null : str;

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

        public string GetText(int? spectatorCount = null)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(
                WaveManager.State is not (WaveQueueState.WaveSelected or WaveQueueState.WaveSpawning)
                    ? BeforeRespawnString
                    : DuringRespawnString);
            SetAllProperties(spectatorCount);
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

        private TimerView(string beforeRespawnString, string duringRespawnString, Properties properties, List<string> hints)
        {
            BeforeRespawnString = beforeRespawnString;
            DuringRespawnString = duringRespawnString;
            Properties = properties;
            Hints = hints;
        }

        private string BeforeRespawnString { get; }

        private string DuringRespawnString { get; }

        private Properties Properties { get; }

        private List<string> Hints { get; }

        private readonly StringBuilder _stringBuilder = new(1024);
    }
}