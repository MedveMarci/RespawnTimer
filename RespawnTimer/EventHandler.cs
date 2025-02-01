namespace RespawnTimer;

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
using Hints;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
#endif

public class EventHandler
{
    private CoroutineHandle _timerCoroutine;
    private CoroutineHandle _hintsCoroutine;
#if NWAPI
    [PluginEvent(ServerEventType.MapGenerated)]
#endif
    internal void OnGenerated()
    {
#if EXILED
        if (RespawnTimer.Singleton.Config.ReloadTimerEachRound) RespawnTimer.Singleton.OnReloaded();
#else
        if (RespawnTimer.Singleton.Config.Timers.IsEmpty())
        {
            Log.Error("Timer list is empty!");
            return;
        }

        TimerView.CachedTimers.Clear();
        foreach (var name in RespawnTimer.Singleton.Config.Timers.Values) TimerView.AddTimer(name);
#endif
        if (_timerCoroutine.IsRunning) Timing.KillCoroutines(_timerCoroutine);
        if (_hintsCoroutine.IsRunning) Timing.KillCoroutines(_hintsCoroutine);
    }

#if NWAPI
    [PluginEvent(ServerEventType.RoundStart)]
#endif
    internal void OnRoundStart()
    {
        try
        {
            _timerCoroutine = Timing.RunCoroutine(TimerCoroutine());
            _hintsCoroutine = Timing.RunCoroutine(HintsCoroutine());
#if NWAPI
            WaveManager.OnWaveSpawned += OnWaveSpawned;
#endif
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }

#if EXILED
        Log.Debug("RespawnTimer coroutine started successfully!");
#else
        Log.Debug("RespawnTimer coroutine started successfully!", RespawnTimer.Singleton.Config.Debug);
#endif
    }

#if NWAPI
    [PluginEvent(ServerEventType.RoundEnd)]
    internal void OnRoundEnd(RoundSummary.LeadingTeam _)
    {
        WaveManager.OnWaveSpawned -= OnWaveSpawned;
    }
#endif
#if NWAPI
    [PluginEvent(ServerEventType.PlayerDeath)]
    internal void OnDying(Player victim, Player _, DamageHandlerBase __)
#else
    internal void OnDying(DyingEventArgs ev)
#endif
    {
        if (RespawnTimer.Singleton.Config.TimerDelay < 0) return;
#if EXILED
        if (_playerDeathDictionary.ContainsKey(ev.Player))
        {
            Timing.KillCoroutines(_playerDeathDictionary[ev.Player]);
            _playerDeathDictionary.Remove(ev.Player);
        }

        _playerDeathDictionary.Add(ev.Player,
            Timing.CallDelayed(RespawnTimer.Singleton.Config.TimerDelay,
                () => _playerDeathDictionary.Remove(ev.Player)));
#else
        if (_playerDeathDictionary.ContainsKey(victim))
        {
            Timing.KillCoroutines(_playerDeathDictionary[victim]);
            _playerDeathDictionary.Remove(victim);
        }

        _playerDeathDictionary.Add(victim,
            Timing.CallDelayed(RespawnTimer.Singleton.Config.TimerDelay, () => _playerDeathDictionary.Remove(victim)));
#endif
    }

    private IEnumerator<float> TimerCoroutine()
    {
        yield return Timing.WaitForSeconds(1f);
        while (true)
        {
            yield return Timing.WaitForSeconds(1f);
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
#if EXILED
            var specNum = Player.List.Count(x => !x.IsAlive || x.SessionVariables.ContainsKey("IsGhost"));
            foreach (var player in Player.List)
#else
            var specNum = Player.GetPlayers().Count(x => !x.IsAlive);
            foreach (var player in Player.GetPlayers())
#endif
            {
                try
                {
#if EXILED
                    if (player == null || (player.IsAlive && !player.SessionVariables.ContainsKey("IsGhost"))) continue;
#else
                    if (player == null || player.IsAlive) continue;
#endif
                    if (player.IsOverwatchEnabled && RespawnTimer.Singleton.Config.HideTimerForOverwatch) continue;
                    if (API.API.TimerHidden.Contains(player.UserId)) continue;
                    if (_playerDeathDictionary.ContainsKey(player)) continue;
                    if (!TimerView.TryGetTimerForPlayer(player, out var timerView)) continue;
                    if (timerView == null)
                    {
#if EXILED
                        Log.Warn(
                            "TimerView is null! Check if the Timers config is correct and the directory exists. If not delete the RespawnTimer folder and restart the server.");
#else
                        Log.Warning(
                            "TimerView is null! Check if the Timers config is correct and the directory exists. If not delete the RespawnTimer folder and restart the server.");
#endif
                        continue;
                    }

                    var text = timerView.GetText(specNum);
#if EXILED
                    player.ShowHint(text, 1.25f);
#else
                    ShowHint(player, text, 1.25f);
#endif
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }

            if (RoundSummary.singleton._roundEnded) break;
        }
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

#if NWAPI
    private static void ShowHint(Player player, string message, float duration = 3f)
    {
        HintParameter[] parameters = { new StringHintParameter(message) };
        player.ReferenceHub.networkIdentity.connectionToClient.Send(
            new HintMessage(new TextHint(message, parameters, durationScalar: duration)));
    }
#endif
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