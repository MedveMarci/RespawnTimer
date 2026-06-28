using System;

namespace RespawnTimer.API.Features;

/// <summary>
/// Represents a custom spawn wave registered through <see cref="RespawnTimer.API.TimerAPI"/>,
/// letting the timer display its team name, countdown and during-spawn animation offset.
/// </summary>
public sealed class RegisteredWave
{
    internal RegisteredWave(Type waveType, Func<string> displayNameProvider, string placeholder, float spawnDuration)
    {
        WaveType = waveType;
        DisplayNameProvider = displayNameProvider;
        Placeholder = placeholder;
        SpawnDuration = spawnDuration;
        Offset = spawnDuration;
    }

    /// <summary>The <c>TimeBasedWave</c> type this entry describes.</summary>
    public Type WaveType { get; }

    /// <summary>Provides the value used for the <c>{team}</c> placeholder when this wave is selected.</summary>
    public Func<string> DisplayNameProvider { get; }

    /// <summary>
    /// Optional placeholder prefix for this wave's countdown, e.g. <c>"x"</c> enables
    /// <c>{xminutes}</c> and <c>{xseconds}</c>. <see langword="null"/> to disable.
    /// </summary>
    public string Placeholder { get; }

    /// <summary>How long (seconds) the spawn animation lasts, used for the <c>{s...}</c> countdown.</summary>
    public float SpawnDuration { get; }

    /// <summary>Current value of the during-spawn countdown, reset to <see cref="SpawnDuration"/> each wave.</summary>
    public float Offset { get; internal set; }
}
