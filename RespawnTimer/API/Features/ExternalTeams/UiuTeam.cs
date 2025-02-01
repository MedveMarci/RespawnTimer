#if EXILED
namespace RespawnTimer.API.Features.ExternalTeams
{
    using System.Reflection;

    public class UiuTeam : ExternalTeamChecker
    {
        public override void Init(Assembly assembly)
        {
            PluginEnabled = true;

            var mainClass = assembly.GetType(RespawnTimer.Singleton.Config.UiuMainClass);
            Instance = mainClass.GetField(RespawnTimer.Singleton.Config.UiuInstance).GetValue(null);
            FieldInfo = mainClass.GetField(RespawnTimer.Singleton.Config.UiuFieldInfo);
        }
    }
}
#endif