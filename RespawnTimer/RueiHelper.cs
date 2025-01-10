using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if EXILED
using Exiled.API.Features;
using Exiled.Loader;
#else
using PluginAPI.Core;
#endif
using MEC;
using RespawnTimer.API.Features;
using RueI.Displays;
using RueI.Elements;

namespace RespawnTimer;

public class RueiHelper
{
    public readonly Dictionary<Player, CoroutineHandle> PlayerDeathDictionary = new(50);
    public static bool IsActive { get; set; }
    public AutoElement RespawnTimerElement { set; get; }

    public void Init()
    {
        IEnumerable<Assembly> assemblies =
#if EXILED
            Loader.Dependencies;
#else
                        PluginAPI.Loader.AssemblyLoader.Dependencies;
#endif
        var assembly = assemblies.FirstOrDefault(x => x.GetName().Name == "RueI");
        if (assembly != null)
        {
            var init = assembly.GetType("RueI.RueIMain")?.GetMethod("EnsureInit");
            if (init != null)
            {
                init.Invoke(null, new object[] { });
                IsActive = true;
            }
        }
    }

    public void RegisterElement()
    {
        if (!RespawnTimer.Singleton.Config.HideTimerForOverwatch)
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

    public string GetTimers(DisplayCore core)
    {
        var player = Player.Get(core.Hub);
#if EXILED
        var specNum = Player.List.Count(x => !x.IsAlive || x.SessionVariables.ContainsKey("IsGhost"));
#else
        int specNum = Player.GetPlayers().Count(x => !x.IsAlive);
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
        if (player.IsOverwatchEnabled && RespawnTimer.Singleton.Config.HideTimerForOverwatch) return "";

        if (API.API.TimerHidden.Contains(player.UserId)) return "";

        if (PlayerDeathDictionary.ContainsKey(player)) return "";

        if (!TimerView.TryGetTimerForPlayer(player, out var timerView)) return "";

        var text = timerView.GetText(specNum);
        return text;
    }
}