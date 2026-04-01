using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace RespawnTimer.API;

public static class TimerAPI
{
    private static readonly Dictionary<string, Func<Player, string>> _properties = new();
    
    public static void RegisterProperty(string placeholder, Func<Player, string> valueProvider)
    {
        _properties[placeholder] = valueProvider;
    }

    public static void UnregisterProperty(string placeholder)
    {
        _properties.Remove(placeholder);
    }

    internal static IReadOnlyDictionary<string, Func<Player, string>> Properties => _properties;
}
