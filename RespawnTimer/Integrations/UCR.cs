using LabApi.Features.Wrappers;
using RespawnTimer.API;
using RespawnTimer.ApiFeatures;

namespace RespawnTimer.Integrations;

public static class UCR
{
    private const string PluginName = "UncomplicatedCustomRoles";

    private const string SummonedCustomRoleGet =
        "UncomplicatedCustomRoles.API.Features.SummonedCustomRole.Get";

    public static bool IsAvailable =>
        DynamicInvoke.GetMethod(PluginName, SummonedCustomRoleGet, isLabapi: true, requiredParamNames: ["player"]) is not null;

    internal static void Enable()
    {
        if (!IsAvailable)
        {
            LogManager.Debug("UCR: Plugin not found, skipping integration.");
            return;
        }

        TimerAPI.RegisterProperty("custom_role", player =>
        {
            TryGetCustomRoleName(player, out var name);
            return name;
        });

        LogManager.Debug("UCR: Integration enabled, {custom_role} placeholder registered.");
    }

    internal static void Disable()
    {
        TimerAPI.UnregisterProperty("custom_role");
    }

    private static bool TryGetCustomRoleName(Player player, out string customRoleName)
    {
        customRoleName = null;

        var getMethod = DynamicInvoke.GetMethod(PluginName, SummonedCustomRoleGet, isLabapi: true, requiredParamNames: ["player"]);
        var roleGetter = DynamicInvoke.GetMethod(PluginName, "UncomplicatedCustomRoles.API.Features.SummonedCustomRole.Role_get", isLabapi: true);
        var nicknameGetter = DynamicInvoke.GetMethod(PluginName, "UncomplicatedCustomRoles.API.Interfaces.ICustomRole.Nickname_get", isLabapi: true);

        if (getMethod is null || roleGetter is null || nicknameGetter is null)
            return false;

        try
        {
            object summonedInstance = getMethod.Invoke(null, [player]);
            if (summonedInstance is null)
                return false;

            object role = roleGetter.Invoke(summonedInstance, null);
            if (role is null)
                return false;

            customRoleName = nicknameGetter.Invoke(role, null) as string;
            return customRoleName is not null;
        }
        catch (System.Exception e)
        {
            LogManager.Error($"UCR: TryGetCustomRoleName failed: {e.Message}");
            return false;
        }
    }
}
