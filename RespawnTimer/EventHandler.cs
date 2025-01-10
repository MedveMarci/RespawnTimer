using System.Collections.Generic;
using MEC;
using RespawnTimer.API.Features;
using UserSettings.ServerSpecific;

namespace RespawnTimer;

#if EXILED
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;

#else
    using Utils.NonAllocLINQ;
    using Hints;
    using PlayerStatsSystem;
    using PluginAPI.Core;
    using PluginAPI.Core.Attributes;
    using PluginAPI.Enums;
#endif

public class EventHandler
{
    private readonly RueiHelper rueiHelper = new();
#if NWAPI
        [PluginEvent(ServerEventType.MapGenerated)]
#endif
    internal void OnGenerated()
    {
        if (rueiHelper.RespawnTimerElement == null)
        {
            Timing.RunCoroutine(HintsCoroutine());
            rueiHelper.RegisterElement();
        }
#if EXILED
        if (RespawnTimer.Singleton.Config.ReloadTimerEachRound) RespawnTimer.Singleton.OnReloaded();
#else
            if (RespawnTimer.Singleton.Config.Timers.IsEmpty())
            {
                Log.Error("Timer list is empty!");
                return;
            }

            TimerView.CachedTimers.Clear();

            foreach (string name in RespawnTimer.Singleton.Config.Timers.Values)
                TimerView.AddTimer(name);
#endif
    }

#if NWAPI
        [PluginEvent(ServerEventType.PlayerDeath)]
        internal void OnDying(Player victim, Player _, DamageHandlerBase __)
#else
    internal void OnDying(DyingEventArgs ev)
#endif
    {
        if (RespawnTimer.Singleton.Config.TimerDelay < 0) return;
#if EXILED
        if (rueiHelper.PlayerDeathDictionary.ContainsKey(ev.Player))
        {
            rueiHelper.PlayerDeathDictionary.Remove(ev.Player);
        }

        rueiHelper.PlayerDeathDictionary.Add(ev.Player,
            Timing.CallDelayed(RespawnTimer.Singleton.Config.TimerDelay,
                () => rueiHelper.PlayerDeathDictionary.Remove(ev.Player)));
#else
            if (rueiHelper.PlayerDeathDictionary.ContainsKey(victim))
            {
                rueiHelper.PlayerDeathDictionary.Remove(victim);
            }

            rueiHelper.PlayerDeathDictionary.Add(victim, Timing.CallDelayed(RespawnTimer.Singleton.Config.TimerDelay, () => rueiHelper.PlayerDeathDictionary.Remove(victim)));
#endif
    }

    private IEnumerator<float> HintsCoroutine()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(1f);
            foreach (var timerView in TimerView.CachedTimers.Values) timerView.IncrementHintInterval();
            if (RoundSummary.singleton._roundEnded) break;
        }
    }

#if NWAPI
        private void ShowHint(Player player, string message, float duration = 3f)
        {
            HintParameter[] parameters =
            {
                new StringHintParameter(message)
            };

            player.ReferenceHub.networkIdentity.connectionToClient.Send(new HintMessage(new TextHint(message, parameters, durationScalar: duration)));
        }
#endif

#if EXILED
    public void OnVerified(VerifiedEventArgs ev)
    {
        ServerSpecificSettingsSync.SendToPlayer(ev.Player.ReferenceHub);
    }

    public void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase settingBase)
    {
        var userId = Player.Get(hub).UserId;
        if (settingBase.SettingId == 1)
        {
            if (ServerSpecificSettingsSync.GetSettingOfUser<SSTwoButtonsSetting>(hub, 1).SyncIsA)
                API.API.TimerHidden.Remove(userId);

            if (ServerSpecificSettingsSync.GetSettingOfUser<SSTwoButtonsSetting>(hub, 1).SyncIsB)
            {
                if (API.API.TimerHidden.Contains(userId)) return;
                API.API.TimerHidden.Add(userId);
            }
        }
    }
#else
		public void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
		{
            string userId = Player.Get(hub).UserId;
            if (setting.SettingId == 1)
            {
                if (ServerSpecificSettingsSync.GetSettingOfUser<SSTwoButtonsSetting>(hub, 1).SyncIsA)
                {
                    API.API.TimerHidden.Remove(userId);
                }
                if (ServerSpecificSettingsSync.GetSettingOfUser<SSTwoButtonsSetting>(hub, 1).SyncIsB)
                {
                    if (API.API.TimerHidden.Contains(userId)) return;
                    API.API.TimerHidden.Add(userId);
                }
			}
		}
#endif
}