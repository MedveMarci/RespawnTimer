using System;
using System.Globalization;
using System.Linq;
using GameCore;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;
using RespawnTimer.Enums;
using UnityEngine;

namespace RespawnTimer.API.Features;

public partial class TimerView
{
    public static float CiOffset { get; set; } = 14f;
    public static float NtfOffset { get; set; } = 18f;
    public static float ShOffset { get; set; } = 15f;

    private void SetAllProperties(ReferenceHub hub, int? spectatorCount = null)
    {
        SetRoundTime();
        SetMinutesAndSeconds();
        SetSpawnableTeam();
        SetNextPossibleTeam();
        SetSpectatorCountAndSpawnChance(spectatorCount);
        SetWarheadStatus();
        SetGeneratorCount();
        SetTpsAndTickrate();
        SetHint();
        SetExternalProperties(hub);
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
        {
            var registeredWave = TimerAPI.GetWave(WaveManager._nextWave);
            if (registeredWave is not null)
                ReplaceTime("s", TimeSpan.FromSeconds(registeredWave.Offset));
            else
                switch (WaveManager._nextWave.TargetFaction)
                {
                    case Faction.FoundationEnemy:
                        ReplaceTime("s", TimeSpan.FromSeconds(CiOffset));
                        break;
                    case Faction.FoundationStaff:
                        ReplaceTime("s", TimeSpan.FromSeconds(NtfOffset));
                        break;
                }
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

        foreach (var registeredWave in TimerAPI.Waves.Values)
        {
            if (string.IsNullOrEmpty(registeredWave.Placeholder)) continue;
            var instance = waves.FirstOrDefault(wave => registeredWave.WaveType.IsInstanceOfType(wave));
            var tokenValue = 0;
            if (instance is not null)
            {
                var prop = registeredWave.WaveType.GetProperty("RespawnTokens");
                if (prop != null)
                {
                    var val = prop.GetValue(instance);
                    if (val is int iv) tokenValue = iv;
                    else if (val != null && int.TryParse(val.ToString(), out var parsed)) tokenValue = parsed;
                }
            }
            _stringBuilder.Replace($"{{{registeredWave.Placeholder}token}}", $"{tokenValue}");
            var time = TimeSpan.FromSeconds(instance?.Timer.TimeLeft ?? 0);
            if (time >= TimeSpan.Zero)
                ReplaceTime(registeredWave.Placeholder, time);
            else
                _stringBuilder
                    .Replace($"{{{registeredWave.Placeholder}minutes}}", "00")
                    .Replace($"{{{registeredWave.Placeholder}seconds}}", "00")
                    .Replace($"{{{registeredWave.Placeholder}token}}", "0");
        }

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
        var displayName = GetWaveDisplayName(WaveManager._nextWave);
        if (displayName is not null)
            _stringBuilder.Replace("{team}", displayName);
    }

    private void SetNextPossibleTeam()
    {
        // While a wave is selected or spawning it is the current team, so the next possible
        // one is whichever of the remaining waves has the least time left on its timer.
        var currentWave = WaveManager.State is WaveQueueState.WaveSelected or WaveQueueState.WaveSpawning
            ? WaveManager._nextWave
            : null;

        var nextWave = WaveManager.Waves
            .OfType<TimeBasedWave>()
            .Where(wave => !ReferenceEquals(wave, currentWave) && CanSpawnNext(wave))
            .OrderBy(wave => wave.Timer.TimeLeft)
            .FirstOrDefault();

        _stringBuilder.Replace("{next_team}",
            (nextWave is null ? null : GetWaveDisplayName(nextWave)) ?? Properties.NoNextTeam);
    }

    // Mirrors the eligibility check WaveManager runs before initiating a respawn, minus the
    // timer having elapsed - a wave that cannot spawn at all is not a possible next spawn.
    private static bool CanSpawnNext(TimeBasedWave wave)
    {
        return wave.Configuration.IsEnabled
               && !wave.Timer.IsPaused
               && !wave.Timer.IsForcefullyPaused
               && wave is not ILimitedWave { RespawnTokens: <= 0 };
    }

    /// <summary>Returns the configured display name of a wave, or <see langword="null"/> if it is unknown.</summary>
    private string GetWaveDisplayName(SpawnableWaveBase wave)
    {
        return wave switch
        {
            NtfMiniWave => Properties.MiniNtf,
            ChaosMiniWave => Properties.MiniCi,
            NtfSpawnWave => Properties.Ntf,
            ChaosSpawnWave => Properties.Ci,
            _ => TimerAPI.GetWave(wave) is { } registeredWave
                ? registeredWave.DisplayNameProvider() ?? string.Empty
                : null
        };
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

    private static WarheadStatusType GetWarheadStatus()
    {
        return Warhead.IsDetonationInProgress
            ? Warhead.IsDetonated ? WarheadStatusType.Detonated :
            Warhead.ScenarioType == WarheadScenarioType.DeadmanSwitch ? WarheadStatusType.DeadManInProgress :
            WarheadStatusType.InProgress
            : Warhead.LeverStatus
                ? WarheadStatusType.Armed
                : WarheadStatusType.NotArmed;
    }

    private void SetGeneratorCount()
    {
        _stringBuilder.Replace("{generator_engaged}", Scp079Recontainer.AllGenerators.Count(x => x.Engaged).ToString());
        _stringBuilder.Replace("{generator_count}", "3");
    }

    private void SetTpsAndTickrate()
    {
        _stringBuilder.Replace("{tps}", Server.Tps.ToString(CultureInfo.InvariantCulture));
        _stringBuilder.Replace("{tickrate}", Server.MaxTps.ToString(CultureInfo.InvariantCulture));
    }

    private void SetHint()
    {
        if (!Hints.Any()) return;
        _stringBuilder.Replace("{hint}", Hints[HintIndex]);
    }

    private void SetExternalProperties(ReferenceHub hub)
    {
        var spectated = Player.Get(hub).CurrentlySpectating;

        foreach (var kvp in TimerAPI.Properties)
        {
            var placeholder = kvp.Key;
            var valueProvider = kvp.Value;
            var value = spectated is not null ? valueProvider(spectated) : null;
            _stringBuilder.Replace($"{{{placeholder}}}", value ?? string.Empty);
        }
    }
}