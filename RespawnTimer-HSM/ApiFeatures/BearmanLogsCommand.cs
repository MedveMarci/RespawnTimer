using System;
using CommandSystem;

namespace RespawnTimer.ApiFeatures;

[CommandHandler(typeof(GameConsoleCommandHandler))]
public class BearmanLogsRt : ICommand
{
    public string Command => "bearmanlogsRT";

    public string[] Aliases { get; } = ["bmlogsRT"];

    public string Description => "Sends collected plugin logs to the log server and returns the log id.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var getLogHistory = LogManager.GetLogHistory();
        response = getLogHistory.logResult;
        return getLogHistory.success;
    }
}