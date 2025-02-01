#if EXILED
namespace RespawnTimer.API.Features.ExternalTeams
{
    using System.Reflection;

    public class SerpentsHandTeam : ExternalTeamChecker
    {
        public override void Init(Assembly assembly)
        {
            PluginEnabled = true;
            
            var mainClass = assembly.GetType(RespawnTimer.Singleton.Config.SHMainClass);
            Instance = mainClass.GetField(RespawnTimer.Singleton.Config.SHInstance).GetValue(null);
            FieldInfo = mainClass.GetField(RespawnTimer.Singleton.Config.SHFieldInfo);
        }
    }
}
#endif