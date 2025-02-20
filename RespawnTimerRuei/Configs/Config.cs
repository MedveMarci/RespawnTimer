using System.Collections.Generic;
using System.ComponentModel;

namespace RespawnTimerRuei.Configs;

public sealed class Config
{
    public Dictionary<string, string> Timers { get; private set; } = new()
    {
        {
            "default", "ExampleTimerRuei"
        }
    };

    [Description("Whether the timer should be hidden for players in overwatch.")]
    public bool HideTimerForOverwatch { get; private set; } = true;

    [Description("The delay before the timer will be shown after player death.")]
    public float TimerDelay { get; private set; } = -1;
}