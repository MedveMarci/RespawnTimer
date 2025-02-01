#if EXILED
namespace RespawnTimerRuei.API.Features.ExternalTeams
{
    using System.Reflection;

    public class UiuTeam : ExternalTeamChecker
    {
        public override void Init(Assembly assembly)
        {
            PluginEnabled = true;

            var mainClass = assembly.GetType(RespawnTimerRuei.Singleton.Config.UiuMainClass);
            Instance = mainClass.GetField(RespawnTimerRuei.Singleton.Config.UiuInstance).GetValue(null);
            FieldInfo = mainClass.GetField(RespawnTimerRuei.Singleton.Config.UiuFieldInfo);
        }
    }
}
#endif