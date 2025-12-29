using System.Collections.Generic;
using System.ComponentModel;

namespace RespawnTimer.Configs;

public sealed class Config
{
    [Description("Whether to enable debug messages in the console.")]
    public bool Debug { get; private set; } = false;
    public Dictionary<string, string> Timers { get; private set; } = new()
    {
        {
            "default", "DefaultTimer"
        }
    };
}