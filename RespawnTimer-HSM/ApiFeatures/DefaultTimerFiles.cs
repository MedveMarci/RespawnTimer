using System.Collections.Generic;

namespace RespawnTimer.ApiFeatures;

internal static class DefaultTimerFiles
{
    private static readonly string TimerBeforeSpawn = "<size=50%>Round time: {round_minutes}:{round_seconds} | TPS: {tps}/{tickrate}</size>" + new string('\n', 34) + "<size=75%><color={RANDOM_COLOR}>You will respawn in: </color>\n" + "<color=blue>NTF Spawn time:</color> {nminutes} min. {nseconds} s\n" + "<color=#002DB3>Mini NTF Spawn time:</color> {mnminutes} min. {mnseconds} s {mntoken} Token\n" + "<color=green>CI Spawn time:</color> {cminutes} min. {cseconds} s\n" + "<color=#32CD32>Mini CI Spawn time:</color> {mcminutes} min. {mcseconds} s {mctoken} Token\n" + "<align=left>Warhead Status: {warhead_status}\n" + "Generators: {generator_engaged}/{generator_count}</align>\n" + "<align=right>\n" + "<color=#808080>Spectators:</color> {spectators_num}\n" + "</align>\n" + "{hint}" + new string('\n', 13);

    private static readonly string TimerDuringSpawn = "<size=50%>Round time: {round_minutes}:{round_seconds} | TPS: {tps}/{tickrate}</size>" + new string('\n', 34) + "<size=110%><color={RANDOM_COLOR}>You will spawn as:</color> {team} {sseconds} s\n" + "<size=75%><color=blue>NTF Spawn time:</color> {nminutes} min. {nseconds} s\n" + "<color=#002DB3>Mini NTF Spawn time:</color> {mnminutes} min. {mnseconds} s {mntoken} Token\n" + "<color=green>CI Spawn time:</color> {cminutes} min. {cseconds} s\n" + "<color=#32CD32>Mini CI Spawn time:</color> {mcminutes} min. {mcseconds} s {mctoken} Token\n" + "<align=left>Warhead Status: {warhead_status}\n" + "Generators: {generator_engaged}/{generator_count}</align>\n" + "<align=right>\n" + "<color=#808080>Spectators:</color> {spectators_num}\n" + "</align>\n" + "{hint}" + new string('\n', 11);

    private const string Hints = "You <b>will</b> die in this game many times.\n" + "Don't throw grenades into elevators. It's not funny at all.";

    internal static readonly IReadOnlyDictionary<string, string> Contents = new Dictionary<string, string>
    {
        ["TimerBeforeSpawn.txt"] = Crlf(TimerBeforeSpawn),
        ["TimerDuringSpawn.txt"] = Crlf(TimerDuringSpawn),
        ["Hints.txt"] = Crlf(Hints)
    };

    private static string Crlf(string text)
    {
        return text.Replace("\n", "\r\n");
    }
}