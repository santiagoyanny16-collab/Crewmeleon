using Crewmeleon.Components;
using Crewmeleon.GameMode;
using FungleAPI.GameModes;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crewmeleon.Patches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
    internal static class PlayerControlPatch
    {
        public static void Postfix(PlayerControl __instance, ref bool __result)
        {
            if (!__instance.AmOwner) return;

            if (GameModeManager.GetCurrentGameMode().GameModeId == GameMode<ChameleonGameMode>.Instance.GameModeId || AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
            {
                if (HudManager.Instance.PlayerCam.Target != __instance)
                {
                    __result = false;
                    return;
                }
                if (__instance.Data != null && __instance.Data.Role.TeamType == RoleTeamTypes.Impostor)
                {
                    __result = __result && GameMode<ChameleonGameMode>.Instance.HideTime <= 0;
                }
            }
        }
    }
}
