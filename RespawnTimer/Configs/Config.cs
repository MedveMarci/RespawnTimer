using System.Collections.Generic;
using System.ComponentModel;

namespace RespawnTimer.Configs;

public sealed class Config
{
    public Dictionary<string, string> Timers { get; private set; } = new()
    {
        {
            "default", "DefaultTimer"
        }
    };

    [Description("Whether the timer should be reloaded each round. Useful if you have many different timers designed.")]
    public bool ReloadTimerEachRound { get; private set; } = true;
}