using System.Collections.Generic;
using System.ComponentModel;
using RespawnTimer.Enums;

namespace RespawnTimer.Configs;

public sealed class Properties
{
    [Description("Whether the leading zeros should be added in minutes and seconds if number is less than 10.")]
    public bool LeadingZeros { get; private set; } = true;

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

    [Description("The Serpent's Hand display name.")]
    public string Sh { get; private set; } = "<color=#FF0090>Serpent's Hand</color>";

    [Description("The display names for warhead statuses:")]
    public Dictionary<WarheadStatuss, string> WarheadStatus { get; private set; } = new()
    {
        {
            WarheadStatuss.NotArmed, "<color=green>Unarmed</color>"
        },
        {
            WarheadStatuss.Armed, "<color=orange>Armed</color>"
        },
        {
            WarheadStatuss.InProgress, "<color=red>In Progress - </color> {detonation_time} s"
        },
        {
            WarheadStatuss.Detonated, "<color=#640000>Detonated</color>"
        }
    };
}