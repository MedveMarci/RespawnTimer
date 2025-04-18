﻿namespace RespawnTimerRuei.Configs
{
    using System.Collections.Generic;
    using System.ComponentModel;

#if EXILED
    public sealed class Config : Exiled.API.Interfaces.IConfig
#else
    public sealed class Config
#endif
    {
        [Description("Whether the plugin is enabled.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Whether debug messages shoul be shown in a server console.")]
        public bool Debug { get; set; } = false;

        public Dictionary<string, string> Timers { get; private set; } = new()
        {
            {
                "default", "ExampleTimerRuei"
            },
        };

        [Description("Whether the timer should be reloaded each round. Useful if you have many different timers designed.")]
        public bool ReloadTimerEachRound { get; private set; } = true;

        [Description("Whether the timer should be hidden for players in overwatch.")]
        public bool HideTimerForOverwatch { get; private set; } = true;

        [Description("The delay before the timer will be shown after player death.")]
        public float TimerDelay { get; private set; } = -1;
        
        [Description("The name of the category.")]
        public string SettingHeaderLabel { get; set; } = "RespawnTimer";
        
        [Description("Serpent's Hand configuration.")]
        public string SHMainClass { get; set; } = "SerpentsHand.Plugin";
        public string SHInstance { get; set; } = "Instance";
        public string SHFieldInfo { get; set; } = "IsSpawnable";
        
        [Description("UIU configuration.")]
        public string UiuMainClass { get; set; } = "UIURescueSquad.UIURescueSquad";
        public string UiuInstance { get; set; } = "Instance";
        public string UiuFieldInfo { get; set; } = "IsSpawnable";
    }
}