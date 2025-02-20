using System.IO;
using System.IO.Compression;
using System.Net;
using LabApi.Events.Handlers;
using LabApi.Features.Console;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using RespawnTimerRuei.Configs;
using Version = System.Version;

namespace RespawnTimerRuei;

public class RespawnTimerRuei : Plugin<Config>
{
    public static RespawnTimerRuei Singleton;
    private EventHandler _eventHandler;
    public static string RespawnTimerRueiDirectoryPath { get; private set; }
    public override string Name => "RespawnTimerRuei";
    public override string Author => "MedveMarci";
    public override string Description { get; } = "A simple Respawn Timer plugin for RueI framework.";
    public override Version Version { get; } = new(4, 4, 0);
    public override Version RequiredApiVersion { get; } = new(0, 4, 0);

    public override void Enable()
    {
        LoadConfigs();
        Singleton = this;
        if (Singleton.Config == null)
        {
            Logger.Error("There is no config file!");
            return;
        }

        RespawnTimerRueiDirectoryPath = Path.Combine(PathManager.Configs.FullName, "RespawnTimerRuei");
        _eventHandler = new EventHandler();
        if (!Directory.Exists(RespawnTimerRueiDirectoryPath))
        {
            Logger.Info("RespawnTimerRuei directory does not exist. Creating...");
            Directory.CreateDirectory(RespawnTimerRueiDirectoryPath);
        }

        var exampleTimerDirectory = Path.Combine(RespawnTimerRueiDirectoryPath, "ExampleTimerRuei");
        if (!Directory.Exists(exampleTimerDirectory)) DownloadExampleTimer(exampleTimerDirectory);
        ServerEvents.MapGenerated += _eventHandler.OnGenerated;
        PlayerEvents.Dying += _eventHandler.OnDying;
        //ServerSpecificSettingsSync.ServerOnSettingValueReceived += EventHandler.OnSettingValueReceived;
        ServerEvents.WaveRespawned += EventHandler.OnRespawnedTeam;

        /*var header = new HeaderSetting(Config.SettingHeaderLabel);
        IEnumerable<SettingBase> settingBases = new SettingBase[]
        {
            header, new TwoButtonsSetting(1, "Visibility", "Show", "Hide", false, "Hide/Show the Timer"),
        };
        SettingBase.Register(settingBases);
        SettingBase.SendToAll();*/
        RueiHelper.Init();
    }

    private void DownloadExampleTimer(string exampleTimerDirectory)
    {
        var exampleTimerZip = exampleTimerDirectory + ".zip";
        var exampleTimerTemp = exampleTimerDirectory + "_Temp";
        using WebClient client = new();
        Logger.Info("Downloading ExampleTimerRuei.zip...");
        var url = $"https://github.com/MedveMarci/RespawnTimerRuei/releases/download/v{Version}/ExampleTimerRuei.zip";
        try
        {
            client.DownloadFile(url, exampleTimerZip);
        }
        catch (WebException e)
        {
            if (e.Response is HttpWebResponse response)
                Logger.Error(
                    $"Error while downloading ExampleTimerRuei.zip: {(int)response.StatusCode} {response.StatusCode}");
            return;
        }

        Logger.Info("ExampleTimerRuei.zip has been downloaded!");
        Logger.Info("Extracting...");
        ZipFile.ExtractToDirectory(exampleTimerZip, exampleTimerTemp);
        Directory.Move(Path.Combine(exampleTimerTemp, "ExampleTimerRuei"), exampleTimerDirectory);
        Directory.Delete(exampleTimerTemp);
        File.Delete(exampleTimerZip);
        Logger.Info("Done!");
    }

    public override void Disable()
    {
        ServerEvents.MapGenerated -= _eventHandler.OnGenerated;
        PlayerEvents.Dying -= _eventHandler.OnDying;
        //ServerSpecificSettingsSync.ServerOnSettingValueReceived -= EventHandler.OnSettingValueReceived;
        ServerEvents.WaveRespawned -= EventHandler.OnRespawnedTeam;
        _eventHandler = null;
        Singleton = null;
    }
}