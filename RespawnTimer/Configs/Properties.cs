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

    [Description("CustomRole display name if UCR is available.")]
    public string CustomRole { get; private set; } = "Custom Role: {custom_role_name}";

    [Description("The display names for warhead statuses:")]
    public Dictionary<WarheadStatusType, string> WarheadStatus { get; private set; } = new()
    {
        {
            WarheadStatusType.NotArmed, "<color=green>Unarmed</color>"
        },
        {
            WarheadStatusType.Armed, "<color=orange>Armed</color>"
        },
        {
            WarheadStatusType.InProgress, "<color=red>In Progress - </color> {detonation_time} s"
        },
        {
            WarheadStatusType.Detonated, "<color=#640000>Detonated</color>"
        },
        {
            WarheadStatusType.DeadManInProgress, "<color=red>DeadMan In Progress - </color> {detonation_time} s"
        }
    };
}