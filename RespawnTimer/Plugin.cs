using System.IO;
using System.IO.Compression;
using System.Net;
using LabApi.Features.Console;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using Version = System.Version;

namespace RespawnTimer;
public class RespawnTimer : Plugin<Configs.Config>
{
    public static RespawnTimer Singleton;
    public static string RespawnTimerDirectoryPath { get; private set; }
    private EventHandler _eventHandler;
    public override void Enable()
    {
        LoadConfigs();
        Singleton = this;
        if (Singleton.Config == null)
        {
            Logger.Error("There is no config file!");
            return;
        }
        RespawnTimerDirectoryPath = Path.Combine(PathManager.Configs.FullName, "RespawnTimer");
        _eventHandler = new EventHandler();
        if (!Directory.Exists(RespawnTimerDirectoryPath))
        {
            Logger.Info("RespawnTimer directory does not exist. Creating...");
            Directory.CreateDirectory(RespawnTimerDirectoryPath);
        }

        var exampleTimerDirectory = Path.Combine(RespawnTimerDirectoryPath, "ExampleTimer");
        if (!Directory.Exists(exampleTimerDirectory)) DownloadExampleTimer(exampleTimerDirectory);
        LabApi.Events.Handlers.ServerEvents.MapGenerated += _eventHandler.OnGenerated;
        LabApi.Events.Handlers.ServerEvents.RoundStarted += _eventHandler.OnRoundStart;
        LabApi.Events.Handlers.PlayerEvents.Dying += _eventHandler.OnDying;
        //ServerSpecificSettingsSync.ServerOnSettingValueReceived += EventHandler.OnSettingValueReceived;
        LabApi.Events.Handlers.ServerEvents.WaveRespawned += EventHandler.OnRespawnedTeam;

        /*var header = new HeaderSetting(Config.SettingHeaderLabel);
        IEnumerable<SettingBase> settingBases = new SettingBase[]
        {
            header, new TwoButtonsSetting(1, "Visibility", "Show", "Hide", false, "Hide/Show the Timer"),
        };
        SettingBase.Register(settingBases);
        SettingBase.SendToAll();*/
    }

    private void DownloadExampleTimer(string exampleTimerDirectory)
    {
        var exampleTimerZip = exampleTimerDirectory + ".zip";
        var exampleTimerTemp = exampleTimerDirectory + "_Temp";
        using WebClient client = new();
        Logger.Info("Downloading ExampleTimer.zip...");
        var url = $"https://github.com/MedveMarci/RespawnTimer/releases/download/v{Version}/ExampleTimer.zip";
        try
        {
            client.DownloadFile(url, exampleTimerZip);
        }
        catch (WebException e)
        {
            if (e.Response is HttpWebResponse response)
                Logger.Error(
                    $"Error while downloading ExampleTimer.zip: {(int)response.StatusCode} {response.StatusCode}");
            return;
        }

        Logger.Info("ExampleTimer.zip has been downloaded!");
        Logger.Info("Extracting...");
        ZipFile.ExtractToDirectory(exampleTimerZip, exampleTimerTemp);
        Directory.Move(Path.Combine(exampleTimerTemp, "ExampleTimer"), exampleTimerDirectory);
        Directory.Delete(exampleTimerTemp);
        File.Delete(exampleTimerZip);
        Logger.Info("Done!");
    }

    public override void Disable()
    {
        LabApi.Events.Handlers.ServerEvents.MapGenerated -= _eventHandler.OnGenerated;
        LabApi.Events.Handlers.ServerEvents.RoundStarted -= _eventHandler.OnRoundStart;
        LabApi.Events.Handlers.PlayerEvents.Dying -= _eventHandler.OnDying;
        //ServerSpecificSettingsSync.ServerOnSettingValueReceived -= EventHandler.OnSettingValueReceived;
        LabApi.Events.Handlers.ServerEvents.WaveRespawned -= EventHandler.OnRespawnedTeam;
        _eventHandler = null;
        Singleton = null;
    }
    public override string Name => "RespawnTimer";
    public override string Author => "MedveMarci";
    public override string Description { get; } = "A simple Respawn Timer plugin.";
    public override Version Version { get; } = new Version(4, 4, 0);
    public override Version RequiredApiVersion { get; } = new(0, 4, 0);
}