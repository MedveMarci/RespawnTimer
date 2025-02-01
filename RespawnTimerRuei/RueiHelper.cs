namespace RespawnTimerRuei;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MEC;
using API.Features;
using RueI.Displays;
using RueI.Elements;
using Respawning;
#if EXILED
using Exiled.API.Features;
using Exiled.Loader;
using Exiled.API.Enums;
#else
using PluginAPI.Core;
using Respawning.Waves;
#endif

public class RueiHelper
{
    public readonly Dictionary<Player, CoroutineHandle> PlayerDeathDictionary = new(50);
    public static bool IsActive { get; set; }
    public AutoElement RespawnTimerElement { set; get; }

    public static void Init()
    {
        IEnumerable<Assembly> assemblies =
#if EXILED
            Loader.Dependencies;
#else
            PluginAPI.Loader.AssemblyLoader.Dependencies;
#endif
        var assembly = assemblies.FirstOrDefault(x => x.GetName().Name == "RueI");
        if (assembly == null) return;
        var init = assembly.GetType("RueI.RueIMain")?.GetMethod("EnsureInit");
        if (init == null) return;
        init.Invoke(null, new object[] { });
        IsActive = true;
    }

    public void RegisterElement()
    {
        if (!RespawnTimerRuei.Singleton.Config.HideTimerForOverwatch)
            RespawnTimerElement = new AutoElement(Roles.Spectator | Roles.Overwatch, new DynamicElement(GetTimers, 980))
            {
                UpdateEvery = new AutoElement.PeriodicUpdate(TimeSpan.FromSeconds(1))
            };
        else
            RespawnTimerElement = new AutoElement(Roles.Spectator, new DynamicElement(GetTimers, 980))
            {
                UpdateEvery = new AutoElement.PeriodicUpdate(TimeSpan.FromSeconds(1))
            };
    }

    private string GetTimers(DisplayCore core)
    {
        if (WaveManager.State is WaveQueueState.WaveSelected or WaveQueueState.WaveSpawning)
        {
#if EXILED
            switch (Respawn.NextKnownSpawnableFaction)
            {
                case SpawnableFaction.ChaosWave:
                    TimerView.CiOffset -= 1;
                    break;
                case SpawnableFaction.NtfWave:
                    TimerView.NtfOffset -= 1;
                    break;
                case SpawnableFaction.ChaosMiniWave:
                    TimerView.CiOffset -= 1;
                    break;
                case SpawnableFaction.NtfMiniWave:
                    TimerView.NtfOffset -= 1;
                    break;
                case SpawnableFaction.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
#else
            switch (WaveManager._nextWave)
            {
                case ChaosSpawnWave:
                    TimerView.CiOffset -= 1;
                    break;
                case NtfSpawnWave:
                    TimerView.NtfOffset -= 1;
                    break;
                case ChaosMiniWave:
                    TimerView.CiOffset -= 1;
                    break;
                case NtfMiniWave:
                    TimerView.NtfOffset -= 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
#endif
        }

        var player = Player.Get(core.Hub);
#if EXILED
        var specNum = Player.List.Count(x => !x.IsAlive || x.SessionVariables.ContainsKey("IsGhost"));
#else
        var specNum = Player.GetPlayers().Count(x => !x.IsAlive);
#endif
#if EXILED
        if (player.IsAlive && !player.SessionVariables.ContainsKey("IsGhost")) return "";
#else
        if (player.IsAlive)
        {
            Log.Debug("Player is alive");
            return "";
        }
#endif
        if (player.IsOverwatchEnabled && RespawnTimerRuei.Singleton.Config.HideTimerForOverwatch) return "";
        if (API.API.TimerHidden.Contains(player.UserId)) return "";
        if (PlayerDeathDictionary.ContainsKey(player)) return "";
        if (!TimerView.TryGetTimerForPlayer(player, out var timerView)) return "";
        if (timerView == null)
        {
#if EXILED
            Log.Warn(
                "TimerView is null! Check if the Timers config is correct and the directory exists. If not delete the RespawnTimer folder and restart the server.");
#else
            Log.Warning(
                "TimerView is null! Check if the Timers config is correct and the directory exists. If not delete the RespawnTimer folder and restart the server.");
#endif
            return "";
        }

        var text = timerView.GetText(specNum);
        return text;
    }
}