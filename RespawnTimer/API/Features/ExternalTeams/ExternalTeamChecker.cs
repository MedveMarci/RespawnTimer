using System.Reflection;

#if EXILED
namespace RespawnTimer.API.Features.ExternalTeams
{
    public abstract class ExternalTeamChecker
    {
        public bool IsSpawnable
        {
            get
            {
                if (!PluginEnabled)
                    return false;

                return (bool)FieldInfo.GetValue(Instance);
            }
        }

        protected bool PluginEnabled { get; set; }
        protected FieldInfo FieldInfo { get; set; }
        protected object Instance { get; set; }
        public abstract void Init(Assembly assembly);
    }
}
#endif