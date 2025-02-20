using System.Collections.Generic;
using System.ComponentModel;

namespace RespawnTimer.Configs;

public sealed class Config
{
    [Description("Whether the plugin is enabled.")]
    public bool IsEnabled { get; set; } = true;

    [Description("Whether debug messages shoul be shown in a server console.")]
    public bool Debug { get; set; } = false;

    public Dictionary<string, string> Timers { get; private set; } = new()
    {
        {
            "default", "ExampleTimer"
        }
    };

    [Description("Whether the timer should be hidden for players in overwatch.")]
    public bool HideTimerForOverwatch { get; private set; } = true;

    [Description("The delay before the timer will be shown after player death.")]
    public float TimerDelay { get; private set; } = -1;
}