using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using Respawning;
using RespawnTimer.API.Features;
using RespawnTimer.ApiFeatures;
using UserSettings.ServerSpecific;

namespace RespawnTimer;

public class EventHandler
{
    private static readonly List<Player> Players = [];
    private CoroutineHandle _hintsCoroutine;
    private CoroutineHandle _timerCoroutine;

    internal void OnWaitingForPlayers()
    {
        if (_timerCoroutine.IsRunning) Timing.KillCoroutines(_timerCoroutine);
        if (_hintsCoroutine.IsRunning) Timing.KillCoroutines(_hintsCoroutine);
        try
        {
            ApiManager.CheckForUpdates();
        }
        catch (Exception ex)
        {
            LogManager.Error($"Version check could not be started.\n{ex}");
        }
    }

    internal void OnRoundStarted()
    {
        try
        {
            _timerCoroutine = Timing.RunCoroutine(TimerCoroutine());
            _hintsCoroutine = Timing.RunCoroutine(HintsCoroutine());
        }
        catch (Exception e)
        {
            LogManager.Error(e.ToString());
        }
    }

    internal static void OnRoleChanging(PlayerChangingRoleEventArgs ev)
    {
        RespawnTimer.Singleton.OnReloaded();
        RefreshHint(ev.Player, ev.NewRole);
    }

    internal static void RefreshHint(Player player, RoleTypeId newRole)
    {
        if (!Round.IsRoundInProgress || newRole is not (RoleTypeId.Spectator or RoleTypeId.Overwatch) ||
            ServerSpecificSettingsSync.GetSettingOfUser<SSTwoButtonsSetting>(player.ReferenceHub, 1).SyncIsB)
        {
            Players.Remove(player);
            return;
        }

        if (Players.Contains(player)) return;
        Players.Add(player);
    }

    private static IEnumerator<float> TimerCoroutine()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(1f);
            if (WaveManager.State is WaveQueueState.WaveSelected or WaveQueueState.WaveSpawning)
                switch (WaveManager._nextWave.TargetFaction)
                {
                    case Faction.SCP:
                        TimerView.ShOffset -= 1;
                        break;
                    case Faction.FoundationEnemy:
                        TimerView.CiOffset -= 1;
                        break;
                    case Faction.FoundationStaff:
                        TimerView.NtfOffset -= 1;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            if (TimerView.Instance is not null)
                foreach (var player in Players)
                    player.SendHint(TimerView.Instance.GetText(player.ReferenceHub), 1.25f);
            if (RoundSummary.singleton.IsRoundEnded) break;
        }
    }

    private static IEnumerator<float> HintsCoroutine()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(1f);
            TimerView.Instance?.IncrementHintInterval();
            if (RoundSummary.singleton.IsRoundEnded) break;
        }
    }

    internal static void OnJoined(PlayerJoinedEventArgs ev)
    {
        ServerSpecificSettingsSync.SendToPlayer(ev.Player.ReferenceHub);
    }

    internal static void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase settingBase)
    {
        if (settingBase.SettingId != 1) return;
        RefreshHint(Player.Get(hub), hub.GetRoleId());
    }

    internal static void OnLeft(PlayerLeftEventArgs ev)
    {
        if (Players.Contains(ev.Player)) Players.Remove(ev.Player);
    }

    internal static void OnWaveRespawning(WaveRespawningEventArgs ev)
    {
        TimerView.CiOffset = 14f;
        TimerView.NtfOffset = 18f;
        TimerView.ShOffset = 15f;
    }
}