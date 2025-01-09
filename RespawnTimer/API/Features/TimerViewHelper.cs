using LightContainmentZoneDecontamination;
using Respawning.Waves;

namespace RespawnTimer.API.Features;

using System;
using System.Globalization;
using System.Linq;
using GameCore;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using Respawning;
using UnityEngine;
#if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
#else
using PluginAPI.Core;
#endif

public partial class TimerView
{
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
        int minutes = RoundStart.RoundLength.Minutes;
        StringBuilder.Replace("{round_minutes}",
            $"{(Properties.LeadingZeros && minutes < 10 ? "0" : string.Empty)}{minutes}");
        int seconds = RoundStart.RoundLength.Seconds;
        StringBuilder.Replace("{round_seconds}",
            $"{(Properties.LeadingZeros && seconds < 10 ? "0" : string.Empty)}{seconds}");
    }

    private void SetMinutesAndSeconds()
    {
        var waves = WaveManager.Waves.OfType<TimeBasedWave>().ToList();
        var ntf = waves.FirstOrDefault(wave => wave is NtfSpawnWave && wave.ReceiveObjectiveRewards);
        var ci = waves.FirstOrDefault(wave => wave is ChaosSpawnWave && wave.ReceiveObjectiveRewards);
        TimeSpan ciTime = TimeSpan.FromSeconds(ci?.Timer.TimeLeft ?? 0);
        TimeSpan ntfTime = TimeSpan.FromSeconds(ntf?.Timer.TimeLeft ?? 0);
        TimeSpan miniCiTime =
            TimeSpan.FromSeconds(waves.FirstOrDefault(wave => wave is ChaosMiniWave)?.Timer.TimeLeft ?? 0);
        TimeSpan miniNtfTime =
            TimeSpan.FromSeconds(waves.FirstOrDefault(wave => wave is NtfMiniWave)?.Timer.TimeLeft ?? 0);

        void ReplaceTime(string placeholder, TimeSpan time)
        {
            int minutes = (int)time.TotalSeconds / 60;
            int seconds = (int)Math.Round(time.TotalSeconds % 60);
            StringBuilder.Replace($"{{{placeholder}minutes}}",
                $"{(Properties.LeadingZeros && minutes < 10 ? "0" : string.Empty)}{minutes}");
            StringBuilder.Replace($"{{{placeholder}seconds}}",
                $"{(Properties.LeadingZeros && seconds < 10 ? "0" : string.Empty)}{seconds}");
        }

        if (WaveManager.State is WaveQueueState.WaveSelected or WaveQueueState.WaveSpawning)
        {
#if EXILED
            int offset = (WaveManager.State is WaveQueueState.WaveSelected or WaveQueueState.WaveSpawning)
                ? (Respawn.NextKnownSpawnableFaction is SpawnableFaction.ChaosWave or SpawnableFaction.ChaosMiniWave ? 13 : 18)
                : 0;
#else
            int offset = (WaveManager.State is WaveQueueState.WaveSelected or WaveQueueState.WaveSpawning)
                ? (WaveManager._nextWave is ChaosSpawnWave or ChaosMiniWave ? 13 : 18)
                : 0;
#endif
#if EXILED
            switch (Respawn.NextKnownSpawnableFaction)
            {
                case SpawnableFaction.ChaosWave:
                    ReplaceTime("s", ciTime + TimeSpan.FromSeconds(offset));
                    break;
                case SpawnableFaction.NtfWave:
                    ReplaceTime("s", ntfTime + TimeSpan.FromSeconds(offset));
                    break;
                case SpawnableFaction.ChaosMiniWave:
                    ReplaceTime("s", miniCiTime + TimeSpan.FromSeconds(offset));
                    break;
                case SpawnableFaction.NtfMiniWave:
                    ReplaceTime("s", miniNtfTime + TimeSpan.FromSeconds(offset));
                    break;
#else
            switch (WaveManager._nextWave)
            {
                case ChaosSpawnWave:
                    ReplaceTime("s", ciTime + TimeSpan.FromSeconds(offset));
                    break;
                case NtfSpawnWave:
                    ReplaceTime("s", ntfTime + TimeSpan.FromSeconds(offset));
                    break;
                case ChaosMiniWave:
                    ReplaceTime("s", miniCiTime + TimeSpan.FromSeconds(offset));
                    break;
                case NtfMiniWave:
                    ReplaceTime("s", miniNtfTime + TimeSpan.FromSeconds(offset));
                    break;
#endif
            }
        }

        if (ciTime >= TimeSpan.Zero)
            ReplaceTime("c", ciTime);
        else
            StringBuilder.Replace("{cminutes}", "00").Replace("{cseconds}", "00");
        if (ntfTime >= TimeSpan.Zero)
            ReplaceTime("n", ntfTime);
        else
            StringBuilder.Replace("{nminutes}", "00").Replace("{nseconds}", "00");
        if (miniCiTime >= TimeSpan.Zero)
            ReplaceTime("mc", miniCiTime);
        else
            StringBuilder.Replace("{mcminutes}", "00").Replace("{mcseconds}", "00");
        if (miniNtfTime >= TimeSpan.Zero)
            ReplaceTime("mn", miniNtfTime);
        else
            StringBuilder.Replace("{mnminutes}", "00").Replace("{mnseconds}", "00");
        int miniNtfToken = waves.OfType<NtfMiniWave>().Sum(wave => wave.RespawnTokens);
        int miniCiToken = waves.OfType<ChaosMiniWave>().Sum(wave => wave.RespawnTokens);
        StringBuilder.Replace("{mntoken}", $"{miniNtfToken}");
        StringBuilder.Replace("{mctoken}", $"{miniCiToken}");
    }

    private void SetSpawnableTeam()
    {
#if EXILED
        switch (Respawn.NextKnownSpawnableFaction)
#else
        switch (WaveManager._nextWave)
#endif
        {
            default:
                return;
#if EXILED
            case SpawnableFaction.NtfWave:
                StringBuilder.Replace("{team}", !API.UiuSpawnable ? Properties.Ntf : Properties.Uiu);
                break;
            case SpawnableFaction.ChaosWave:
                StringBuilder.Replace("{team}", !API.SerpentsHandSpawnable ? Properties.Ci : Properties.Sh);
                break;
            case SpawnableFaction.NtfMiniWave:
                StringBuilder.Replace("{team}", !API.UiuSpawnable ? Properties.MiniNtf : Properties.Uiu);
                break;
            case SpawnableFaction.ChaosMiniWave:
                StringBuilder.Replace("{team}", !API.SerpentsHandSpawnable ? Properties.MiniCi : Properties.Sh);
                break;
#else
            case NtfSpawnWave:
                StringBuilder.Replace("{team}", Properties.Ntf);
                break;
            case ChaosSpawnWave:
                StringBuilder.Replace("{team}", Properties.Ci);
                break;
            case NtfMiniWave:
                StringBuilder.Replace("{team}", Properties.MiniNtf);
                break;
            case ChaosMiniWave:
                StringBuilder.Replace("{team}", Properties.MiniCi);
                break;
#endif
        }
    }

    private void SetSpectatorCountAndSpawnChance(int? spectatorCount = null)
    {
#if EXILED
        StringBuilder.Replace("{spectators_num}",
            spectatorCount?.ToString() ??
            Player.List.Count(x => x.Role.Team == Team.Dead && !x.IsOverwatchEnabled).ToString());
#else
        StringBuilder.Replace("{spectators_num}",
            spectatorCount?.ToString() ?? Player.GetPlayers()
                .Count(x => x.Role == RoleTypeId.Spectator && !x.IsOverwatchEnabled).ToString());
#endif
    }

    private void SetWarheadStatus()
    {
#if EXILED
        WarheadStatus warheadStatus = Warhead.Status;
        StringBuilder.Replace("{warhead_status}", Properties.WarheadStatus[warheadStatus]);
                    StringBuilder.Replace("{detonation_time}",
            warheadStatus == WarheadStatus.InProgress
                ? Mathf.Round(Warhead.DetonationTimer).ToString(CultureInfo.InvariantCulture)
                : string.Empty);
#else
        string warheadStatus = Warhead.IsDetonationInProgress ? Warhead.IsDetonated ? "Detonated" : "InProgress" :
            Warhead.LeverStatus ? "Armed" : "NotArmed";
        StringBuilder.Replace("{warhead_status}", Properties.WarheadStatus[warheadStatus]);
        StringBuilder.Replace("{detonation_time}",
            warheadStatus == "InProgress"
                ? Mathf.Round(Warhead.DetonationTime).ToString(CultureInfo.InvariantCulture)
                : string.Empty);
#endif
    }

    private void SetGeneratorCount()
    {
        /*
        int generatorEngaged = 0;
        int generatorCount = 0;

        foreach (Generator generator in Generator.List)
        {
            if (generator.State.HasFlag(GeneratorState.Engaged))
                generatorEngaged++;

            generatorCount++;
        }

        StringBuilder.Replace("{generator_engaged}", generatorEngaged.ToString());
        StringBuilder.Replace("{generator_count}", generatorCount.ToString());
        */
        StringBuilder.Replace("{generator_engaged}", Scp079Recontainer.AllGenerators.Count(x => x.Engaged).ToString());
        StringBuilder.Replace("{generator_count}", "3");
    }

    private void SetTpsAndTickrate()
    {
        StringBuilder.Replace("{tps}", Math.Round(1.0 / Time.smoothDeltaTime).ToString(CultureInfo.InvariantCulture));
        StringBuilder.Replace("{tickrate}", Application.targetFrameRate.ToString());
    }

    private void SetHint()
    {
        if (!Hints.Any()) return;
        StringBuilder.Replace("{hint}", Hints[HintIndex]);
    }
}