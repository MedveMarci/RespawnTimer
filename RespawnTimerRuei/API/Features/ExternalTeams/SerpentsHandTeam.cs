#if EXILED
namespace RespawnTimerRuei.API.Features.ExternalTeams
{
    using System.Reflection;

    public class SerpentsHandTeam : ExternalTeamChecker
    {
        public override void Init(Assembly assembly)
        {
            PluginEnabled = true;
            
            var mainClass = assembly.GetType(RespawnTimerRuei.Singleton.Config.SHMainClass);
            Instance = mainClass.GetField(RespawnTimerRuei.Singleton.Config.SHInstance).GetValue(null);
            FieldInfo = mainClass.GetField(RespawnTimerRuei.Singleton.Config.SHFieldInfo);
        }
    }
}
#endif