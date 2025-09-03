using System;
using System.Globalization;
using System.Linq;
using GameCore;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using Respawning;
using Respawning.Waves;
using RespawnTimer.Enums;
using UnityEngine;

namespace RespawnTimer.API.Features;

public partial class TimerView
{
    public static float CiOffset { get; set; } = 14f;
    public static float NtfOffset { get; set; } = 18f;
    public static float ShOffset { get; set; } = 15f;

    private void SetAllProperties(int? spectatorCount = null)
    {
        SetRoundTime();
        SetMinutesAndSeconds();
        SetSpawnableTeam();
        SetSpectatorCountAndSpawnChance(spectatorCount);
        SetWarheadStatus();
        SetGeneratorCount();
        SetTpsAndTickrate();
        SetHint();
    }

    private void SetRoundTime()
    {
        var hours = RoundStart.RoundLength.Hours;
        _stringBuilder.Replace("{round_hours}",
            $"{(Properties.LeadingZeros && hours < 10 ? "0" : string.Empty)}{hours}");
        var minutes = RoundStart.RoundLength.Minutes;
        _stringBuilder.Replace("{round_minutes}",
            $"{(Properties.LeadingZeros && minutes < 10 ? "0" : string.Empty)}{minutes}");
        var seconds = RoundStart.RoundLength.Seconds;
        _stringBuilder.Replace("{round_seconds}",
            $"{(Properties.LeadingZeros && seconds < 10 ? "0" : string.Empty)}{seconds}");
    }

    private void SetMinutesAndSeconds()
    {
        var waves = WaveManager.Waves.OfType<TimeBasedWave>().ToList();
        var ntf = waves.FirstOrDefault(wave => wave is NtfSpawnWave);
        var ci = waves.FirstOrDefault(wave => wave is ChaosSpawnWave);
        var miniNtf = waves.FirstOrDefault(wave => wave is NtfMiniWave);
        var miniCi = waves.FirstOrDefault(wave => wave is ChaosMiniWave);
        var ciTime = TimeSpan.FromSeconds(ci?.Timer.TimeLeft ?? 0);
        var ntfTime = TimeSpan.FromSeconds(ntf?.Timer.TimeLeft ?? 0);
        var miniCiTime = TimeSpan.FromSeconds(miniCi?.Timer.TimeLeft ?? 0);
        var miniNtfTime = TimeSpan.FromSeconds(miniNtf?.Timer.TimeLeft ?? 0);
        if (WaveManager.State is WaveQueueState.WaveSelected or WaveQueueState.WaveSpawning)
            switch (WaveManager._nextWave.TargetFaction)
            {
                case Faction.SCP:
                    ReplaceTime("s", TimeSpan.FromSeconds(ShOffset));
                    break;
                case Faction.FoundationEnemy:
                    ReplaceTime("s", TimeSpan.FromSeconds(CiOffset));
                    break;
                case Faction.FoundationStaff:
                    ReplaceTime("s", TimeSpan.FromSeconds(NtfOffset));
                    break;
            }

        if (ciTime >= TimeSpan.Zero)
            ReplaceTime("c", ciTime);
        else
            _stringBuilder.Replace("{cminutes}", "00").Replace("{cseconds}", "00");
        if (ntfTime >= TimeSpan.Zero)
            ReplaceTime("n", ntfTime);
        else
            _stringBuilder.Replace("{nminutes}", "00").Replace("{nseconds}", "00");
        if (miniCiTime >= TimeSpan.Zero)
            ReplaceTime("mc", miniCiTime);
        else
            _stringBuilder.Replace("{mcminutes}", "00").Replace("{mcseconds}", "00");
        if (miniNtfTime >= TimeSpan.Zero)
            ReplaceTime("mn", miniNtfTime);
        else
            _stringBuilder.Replace("{mnminutes}", "00").Replace("{mnseconds}", "00");
        var miniNtfToken = waves.OfType<NtfMiniWave>().Sum(wave => wave.RespawnTokens);
        var miniCiToken = waves.OfType<ChaosMiniWave>().Sum(wave => wave.RespawnTokens);

        _stringBuilder.Replace("{mntoken}", $"{miniNtfToken}");
        _stringBuilder.Replace("{mctoken}", $"{miniCiToken}");

        return;

        void ReplaceTime(string placeholder, TimeSpan? time)
        {
            if (time == null) return;
            var totalSeconds = Math.Max(0, (int)time.Value.TotalSeconds);
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            _stringBuilder.Replace($"{{{placeholder}minutes}}",
                $"{(Properties.LeadingZeros && minutes < 10 ? "0" : string.Empty)}{minutes}");
            _stringBuilder.Replace($"{{{placeholder}seconds}}",
                $"{(Properties.LeadingZeros && seconds < 10 ? "0" : string.Empty)}{seconds}");
        }
    }

    private void SetSpawnableTeam()
    {
        if (WaveManager._nextWave is null) return;
        switch (WaveManager._nextWave)
        {
            case NtfSpawnWave:
                _stringBuilder.Replace("{team}", Properties.Ntf);
                break;
            case NtfMiniWave:
                _stringBuilder.Replace("{team}", Properties.MiniNtf);
                break;
            case ChaosSpawnWave:
                _stringBuilder.Replace("{team}", Properties.Ci);
                break;
            case ChaosMiniWave:
                _stringBuilder.Replace("{team}", Properties.MiniCi);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SetSpectatorCountAndSpawnChance(int? spectatorCount = null)
    {
        _stringBuilder.Replace("{spectators_num}",
            spectatorCount?.ToString() ??
            Player.ReadyList.Count(x => x.RoleBase.Team == Team.Dead && !x.IsOverwatchEnabled).ToString());
    }

    private void SetWarheadStatus()
    {
        var warheadStatus = GetWarheadStatus();
        _stringBuilder.Replace("{warhead_status}", Properties.WarheadStatus[warheadStatus]);
        _stringBuilder.Replace("{detonation_time}",
            Warhead.IsDetonationInProgress
                ? Mathf.Round(Warhead.DetonationTime).ToString(CultureInfo.InvariantCulture)
                : string.Empty);
    }

    private static WarheadStatuss GetWarheadStatus()
    {
        return Warhead.IsDetonationInProgress
            ? Warhead.IsDetonated ? WarheadStatuss.Detonated : WarheadStatuss.InProgress
            : Warhead.LeverStatus
                ? WarheadStatuss.Armed
                : WarheadStatuss.NotArmed;
    }

    private void SetGeneratorCount()
    {
        _stringBuilder.Replace("{generator_engaged}", Scp079Recontainer.AllGenerators.Count(x => x.Engaged).ToString());
        _stringBuilder.Replace("{generator_count}", "3");
    }

    private void SetTpsAndTickrate()
    {
        _stringBuilder.Replace("{tps}", Math.Round(1.0 / Time.smoothDeltaTime).ToString(CultureInfo.InvariantCulture));
        _stringBuilder.Replace("{tickrate}", Application.targetFrameRate.ToString());
    }

    private void SetHint()
    {
        if (!Hints.Any()) return;
        _stringBuilder.Replace("{hint}", Hints[HintIndex]);
    }
}