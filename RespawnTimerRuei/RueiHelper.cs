using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using Respawning;
using Respawning.Waves;
using RespawnTimerRuei.API.Features;
using RueI.Displays;
using RueI.Elements;

namespace RespawnTimerRuei;

public class RueiHelper
{
    public static bool IsActive { get; set; }
    public AutoElement RespawnTimerElement { set; get; }

    public static void Init()
    {
        IEnumerable<Assembly> assemblies =
            PluginLoader.Dependencies;
        var assembly = assemblies.FirstOrDefault(x => x.GetName().Name == "RueI");
        if (assembly == null) return;
        var init = assembly.GetType("RueI.RueIMain")?.GetMethod("EnsureInit");
        if (init == null) return;
        init.Invoke(null, new object[] { });
        IsActive = true;
    }

    public void RegisterElement()
    {
        if (!RespawnTimerRuei.Singleton.Config!.HideTimerForOverwatch)
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

        var player = Player.Get(core.Hub);
        var specNum = Player.List.Count(x => !x.IsAlive);
        if (player.IsAlive)
        {
            Logger.Debug("Player is alive");
            return "";
        }

        if (player.IsOverwatchEnabled && RespawnTimerRuei.Singleton.Config!.HideTimerForOverwatch) return "";
        if (API.API.TimerHidden.Contains(player.UserId)) return "";
        if (EventHandler._playerDeathDictionary.ContainsKey(player)) return "";
        if (!TimerView.TryGetTimerForPlayer(player, out var timerView)) return "";
        if (timerView == null)
        {
            Logger.Warn(
                "TimerView is null! Check if the Timers config is correct and the directory exists. If not delete the RespawnTimer folder and restart the server.");
            return "";
        }

        var text = timerView.GetText(specNum);
        return text;
    }
}