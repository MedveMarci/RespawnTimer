namespace RespawnTimerRuei;

using UserSettings.ServerSpecific;
using System;
using System.Collections.Generic;
using MEC;
using API.Features;
#if EXILED
using Respawning;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Server;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
#else
using Respawning;
using Respawning.Waves;
using Utils.NonAllocLINQ;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
#endif

public class EventHandler
{
    private readonly RueiHelper _rueiHelper = new();
#if NWAPI
    [PluginEvent(ServerEventType.MapGenerated)]
#endif
    internal void OnGenerated()
    {
        if (_rueiHelper.RespawnTimerElement == null)
        {
            Timing.RunCoroutine(HintsCoroutine());
            _rueiHelper.RegisterElement();
        }
#if EXILED
        if (RespawnTimerRuei.Singleton.Config.ReloadTimerEachRound) RespawnTimerRuei.Singleton.OnReloaded();
#else
        if (RespawnTimerRuei.Singleton.Config.Timers.IsEmpty())
        {
            Log.Error("Timer list is empty!");
            return;
        }

        TimerView.CachedTimers.Clear();
        foreach (string name in RespawnTimerRuei.Singleton.Config.Timers.Values) TimerView.AddTimer(name);
#endif
    }

#if NWAPI
    [PluginEvent(ServerEventType.RoundStart)]
    internal void OnRoundStart()
    {
            WaveManager.OnWaveSpawned += OnWaveSpawned;
    }

    [PluginEvent(ServerEventType.RoundEnd)]
    internal void OnRoundEnd(RoundSummary.LeadingTeam _)
    {
        WaveManager.OnWaveSpawned -= OnWaveSpawned;
    }

    [PluginEvent(ServerEventType.PlayerDeath)]
    internal void OnDying(Player victim, Player _, DamageHandlerBase __)
#else
    internal void OnDying(DyingEventArgs ev)
#endif
    {
        if (RespawnTimerRuei.Singleton.Config.TimerDelay < 0) return;
#if EXILED
        if (_rueiHelper.PlayerDeathDictionary.ContainsKey(ev.Player))
        {
            _rueiHelper.PlayerDeathDictionary.Remove(ev.Player);
        }

        _rueiHelper.PlayerDeathDictionary.Add(ev.Player,
            Timing.CallDelayed(RespawnTimerRuei.Singleton.Config.TimerDelay,
                () => _rueiHelper.PlayerDeathDictionary.Remove(ev.Player)));
#else
        if (_rueiHelper.PlayerDeathDictionary.ContainsKey(victim))
        {
            _rueiHelper.PlayerDeathDictionary.Remove(victim);
        }

        _rueiHelper.PlayerDeathDictionary.Add(victim,
            Timing.CallDelayed(RespawnTimerRuei.Singleton.Config.TimerDelay,
                () => _rueiHelper.PlayerDeathDictionary.Remove(victim)));
#endif
    }

    private static IEnumerator<float> HintsCoroutine()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(1f);
            foreach (var timerView in TimerView.CachedTimers.Values) timerView.IncrementHintInterval();
            if (RoundSummary.singleton._roundEnded) break;
        }
    }
    
    private readonly Dictionary<Player, CoroutineHandle> _playerDeathDictionary = new(25);
#if EXILED
    internal static void OnVerified(VerifiedEventArgs ev)
    {
        ServerSpecificSettingsSync.SendToPlayer(ev.Player.ReferenceHub);
    }
#endif
    internal static void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase settingBase)
    {
        var userId = Player.Get(hub).UserId;
        if (settingBase.SettingId != 1) return;
        if (ServerSpecificSettingsSync.GetSettingOfUser<SSTwoButtonsSetting>(hub, 1).SyncIsA)
        {
            API.API.TimerHidden.Remove(userId);
        }

        if (!ServerSpecificSettingsSync.GetSettingOfUser<SSTwoButtonsSetting>(hub, 1).SyncIsB) return;
        if (API.API.TimerHidden.Contains(userId)) return;
        API.API.TimerHidden.Add(userId);
    }
#if NWAPI
    private static void OnWaveSpawned(SpawnableWaveBase _, List<ReferenceHub> __)
#else
    internal static void OnRespawnedTeam(RespawnedTeamEventArgs ev)
#endif
    {
        TimerView.CiOffset = 14;
        TimerView.NtfOffset = 18;
    }
}