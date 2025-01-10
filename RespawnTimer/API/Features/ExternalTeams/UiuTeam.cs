using System.Reflection;

#if EXILED
namespace RespawnTimer.API.Features.ExternalTeams
{
    public class UiuTeam : ExternalTeamChecker
    {
        public override void Init(Assembly assembly)
        {
            PluginEnabled = true;

            var mainClass = assembly.GetType("UIURescueSquad.UIURescueSquad");
            Instance = mainClass.GetField("Instance").GetValue(null);
            FieldInfo = mainClass.GetField("IsSpawnable");
        }
    }
}
#endif