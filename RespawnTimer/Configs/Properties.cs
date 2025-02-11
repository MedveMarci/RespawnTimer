namespace RespawnTimer.Configs;

using System.Collections.Generic;
using System.ComponentModel;

public sealed class Properties
{
    [Description("Whether the leading zeros should be added in minutes and seconds if number is less than 10.")]
    public bool LeadingZeros { get; private set; } = true;

    [Description("Whether the timer should add time offset depending on MTF/CI spawn.")]
    public bool TimerOffset { get; private set; } = true;

    [Description("How often custom hints should be changed (in seconds).")]
    public int HintInterval { get; private set; } = 10;

    [Description("The Nine-Tailed Fox display name.")]
    public string Ntf { get; private set; } = "<color=blue>Nine-Tailed Fox</color>";

    [Description("The Chaos Insurgency display name.")]
    public string Ci { get; private set; } = "<color=green>Chaos Insurgency</color>";
        
    [Description("The Mini Nine-Tailed Fox display name.")]
    public string MiniNtf { get; private set; } = "<color=blue>Mini Nine-Tailed Fox</color>";

    [Description("The Mini Chaos Insurgency display name.")]
    public string MiniCi { get; private set; } = "<color=green>Mini Chaos Insurgency</color>";

    [Description("The display names for warhead statuses:")]
    public Dictionary<string, string> WarheadStatus { get; private set; } = new()
    {
        {
            "NotArmed", "<color=green>Unarmed</color>"
        },
        {
            "Armed", "<color=orange>Armed</color>"
        },
        {
            "InProgress", "<color=red>In Progress - </color> {detonation_time} s"
        },
        {
            "Detonated", "<color=#640000>Detonated</color>"
        },
    };
}