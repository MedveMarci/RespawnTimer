using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using MEC;
using Respawning;
using Respawning.Waves;
using RespawnTimer.API.Features;

namespace RespawnTimer;

public class EventHandler
{
    private readonly Dictionary<Player, CoroutineHandle> _playerDeathDictionary = new(25);
    private CoroutineHandle _hintsCoroutine;
    private CoroutineHandle _timerCoroutine;

    internal void OnGenerated(MapGeneratedEventArgs ev)
    {
        if (RespawnTimer.Singleton.Config!.Timers.IsEmpty())
        {
            Logger.Error("Timer list is empty!");
            return;
        }

        TimerView.CachedTimers.Clear();
        foreach (var name in RespawnTimer.Singleton.Config.Timers.Values) TimerView.AddTimer(name);
        if (_timerCoroutine.IsRunning) Timing.KillCoroutines(_timerCoroutine);
        if (_hintsCoroutine.IsRunning) Timing.KillCoroutines(_hintsCoroutine);
    }

    internal void OnRoundStart()
    {
        try
        {
            _timerCoroutine = Timing.RunCoroutine(TimerCoroutine());
            _hintsCoroutine = Timing.RunCoroutine(HintsCoroutine());
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }

        Logger.Debug("RespawnTimer coroutine started successfully!");
    }

    internal void OnDying(PlayerDyingEventArgs ev)
    {
        if (RespawnTimer.Singleton.Config!.TimerDelay < 0) return;
        if (_playerDeathDictionary.ContainsKey(ev.Player))
        {
            Timing.KillCoroutines(_playerDeathDictionary[ev.Player]);
            _playerDeathDictionary.Remove(ev.Player);
        }

        _playerDeathDictionary.Add(ev.Player,
            Timing.CallDelayed(RespawnTimer.Singleton.Config.TimerDelay,
                () => _playerDeathDictionary.Remove(ev.Player)));
    }

    private IEnumerator<float> TimerCoroutine()
    {
        yield return Timing.WaitForSeconds(1f);
        while (true)
        {
            yield return Timing.WaitForSeconds(1f);
            if (WaveManager.State is WaveQueueState.WaveSpawning or WaveQueueState.WaveSelected)
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

            var specNum = Player.List.Count(x => !x.IsAlive);
            foreach (var player in Player.List)
                try
                {
                    if (player == null || player.IsAlive || player.IsServer) continue;
                    if (player.IsOverwatchEnabled && RespawnTimer.Singleton.Config.HideTimerForOverwatch) continue;
                    if (API.API.TimerHidden.Contains(player.UserId)) continue;
                    if (_playerDeathDictionary.ContainsKey(player)) continue;
                    if (!TimerView.TryGetTimerForPlayer(player, out var timerView)) continue;
                    if (timerView == null)
                    {
                        Logger.Warn(
                            "TimerView is null! Check if the Timers config is correct and the directory exists. If not delete the RespawnTimer folder and restart the server.");
                        continue;
                    }

                    var text = timerView.GetText(specNum);
                    player.SendHint(text, 1.25f);
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
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

/*if EXILED
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
    }*/
    internal static void OnRespawnedTeam(WaveRespawnedEventArgs ev)
    {
        TimerView.CiOffset = 14;
        TimerView.NtfOffset = 18;
    }
}