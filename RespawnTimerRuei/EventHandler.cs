namespace RespawnTimerRuei;

using System.Collections.Generic;
using MEC;
using API.Features;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;

public class EventHandler
{
    private readonly RueiHelper _rueiHelper = new();
    internal void OnGenerated(MapGeneratedEventArgs ev)
    {
        if (_rueiHelper.RespawnTimerElement == null)
        {
            Timing.RunCoroutine(HintsCoroutine());
            _rueiHelper.RegisterElement();
        }
        if (RespawnTimerRuei.Singleton.Config!.Timers.IsEmpty())
        {
            Logger.Error("Timer list is empty!");
            return;
        }

        TimerView.CachedTimers.Clear();
        foreach (var name in RespawnTimerRuei.Singleton.Config.Timers.Values) TimerView.AddTimer(name);
    }

    internal void OnDying(PlayerDyingEventArgs ev)
    {
        if (RespawnTimerRuei.Singleton.Config!.TimerDelay < 0) return;
        if (_playerDeathDictionary.ContainsKey(ev.Player))
        {
            Timing.KillCoroutines(_playerDeathDictionary[ev.Player]);
            _playerDeathDictionary.Remove(ev.Player);
        }

        _playerDeathDictionary.Add(ev.Player,
            Timing.CallDelayed(RespawnTimerRuei.Singleton.Config.TimerDelay,
                () => _playerDeathDictionary.Remove(ev.Player)));
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
/*#if EXILED
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