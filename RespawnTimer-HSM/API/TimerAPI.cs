using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using Respawning.Waves;
using RespawnTimer.API.Features;

namespace RespawnTimer.API;

public static class TimerAPI
{
    private static readonly Dictionary<string, Func<Player, string>> _properties = new();
    private static readonly Dictionary<Type, RegisteredWave> _waves = new();

    internal static IReadOnlyDictionary<string, Func<Player, string>> Properties => _properties;

    internal static IReadOnlyDictionary<Type, RegisteredWave> Waves => _waves;

    public static void RegisterProperty(string placeholder, Func<Player, string> valueProvider)
    {
        _properties[placeholder] = valueProvider;
    }

    public static void UnregisterProperty(string placeholder)
    {
        _properties.Remove(placeholder);
    }

    /// <summary>
    /// Registers a custom <see cref="TimeBasedWave"/> so the timer can display it instead of
    /// throwing on an unknown wave type.
    /// </summary>
    /// <typeparam name="T">The wave type to register.</typeparam>
    /// <param name="displayNameProvider">Value used for the <c>{team}</c> placeholder while this wave is selected.</param>
    /// <param name="placeholder">
    /// Optional placeholder prefix for this wave's countdown, e.g. <c>"x"</c> enables
    /// <c>{xminutes}</c> and <c>{xseconds}</c>.
    /// </param>
    /// <param name="spawnDuration">Length (seconds) of the spawn animation, used for the <c>{s...}</c> countdown.</param>
    public static void RegisterWave<T>(Func<string> displayNameProvider, string placeholder = null,
        float spawnDuration = 18f) where T : TimeBasedWave
    {
        RegisterWave(typeof(T), displayNameProvider, placeholder, spawnDuration);
    }

    /// <inheritdoc cref="RegisterWave{T}(System.Func{string},string,float)"/>
    public static void RegisterWave<T>(string displayName, string placeholder = null, float spawnDuration = 18f)
        where T : TimeBasedWave
    {
        RegisterWave(typeof(T), () => displayName, placeholder, spawnDuration);
    }

    /// <inheritdoc cref="RegisterWave{T}(System.Func{string},string,float)"/>
    public static void RegisterWave(Type waveType, Func<string> displayNameProvider, string placeholder = null,
        float spawnDuration = 18f)
    {
        if (waveType is null) throw new ArgumentNullException(nameof(waveType));
        if (displayNameProvider is null) throw new ArgumentNullException(nameof(displayNameProvider));
        _waves[waveType] = new RegisteredWave(waveType, displayNameProvider, placeholder, spawnDuration);
    }

    /// <inheritdoc cref="RegisterWave{T}(System.Func{string},string,float)"/>
    public static void RegisterWave(Type waveType, string displayName, string placeholder = null,
        float spawnDuration = 18f)
    {
        RegisterWave(waveType, () => displayName, placeholder, spawnDuration);
    }

    public static void UnregisterWave<T>() where T : TimeBasedWave
    {
        UnregisterWave(typeof(T));
    }

    public static void UnregisterWave(Type waveType)
    {
        _waves.Remove(waveType);
    }

    internal static RegisteredWave GetWave(object wave)
    {
        if (wave is null) return null;
        if (_waves.TryGetValue(wave.GetType(), out var registered)) return registered;
        foreach (var entry in _waves.Values)
            if (entry.WaveType.IsInstanceOfType(wave))
                return entry;
        return null;
    }
}
