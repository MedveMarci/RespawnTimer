using System;
using LabApi.Features.Wrappers;
using RespawnTimer.API;
using RespawnTimer.API.Features;
using RespawnTimer.ApiFeatures;
using UncomplicatedCustomRoles.Integrations;

namespace RespawnTimer.Integrations;

public static class UCR
{
    private const string PluginName = "UncomplicatedCustomRoles";

    private const string SummonedCustomRoleGet =
        "UncomplicatedCustomRoles.API.Features.SummonedCustomRole.Get";

    public static bool IsAvailable =>
        DynamicInvoke.GetMethod(PluginName, SummonedCustomRoleGet, true, requiredParamNames: ["player"],
            requiredParamTypes: [typeof(Player)]) is not null;

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
            return !string.IsNullOrEmpty(name)
                ? TimerView.Instance.Properties.CustomRole.Replace("{custom_role_name}", name)
                : name;
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

        var getMethod =
            DynamicInvoke.GetMethod(PluginName, SummonedCustomRoleGet, true, requiredParamNames: ["player"],
                requiredParamTypes: [typeof(Player)]);
        var roleGetter = DynamicInvoke.GetMethod(PluginName,
            "UncomplicatedCustomRoles.API.Features.SummonedCustomRole.Role_get", true);
        var nameGetter = DynamicInvoke.GetMethod(PluginName,
            "UncomplicatedCustomRoles.API.Interfaces.ICustomRole.Name_get", true);

        if (getMethod is null || roleGetter is null || nameGetter is null)
            return false;

        try
        {
            var summonedInstance = getMethod.Invoke(null, [player]);
            if (summonedInstance is null)
                return false;

            var role = roleGetter.Invoke(summonedInstance, null);
            if (role is null)
                return false;

            customRoleName = nameGetter.Invoke(role, null) as string;
            return customRoleName is not null;
        }
        catch (Exception e)
        {
            LogManager.Error($"UCR: TryGetCustomRoleName failed: {e.Message}");
            return false;
        }
    }
}