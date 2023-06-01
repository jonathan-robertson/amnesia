﻿using Amnesia.Utilities;
using HarmonyLib;
using System;

namespace Amnesia.Patches
{
    /// <summary>
    /// Detect level change and update weight/gear values.
    /// </summary>
    /// <remarks>local players</remarks>
    [HarmonyPatch(typeof(Progression), "AddLevelExp")]
    internal class Progression_AddLevelExp_Patches
    {
        private static readonly ModLog<Progression_AddLevelExp_Patches> _log = new ModLog<Progression_AddLevelExp_Patches>();

        public static void Prefix(Progression __instance, int ___Level, out int __state)
        {
            __state = -1; // default to ignore
            try
            {
                if (!ConnectionManager.Instance.IsServer // TODO: do these checks in many more parts of the code to support remote having local mod (disable remote clients)
                    || __instance.parent.isEntityRemote // only track local entity
                    || !(__instance.parent is EntityPlayerLocal)) // only track EntityPlayerLocal
                {
                    return;
                }

                __state = ___Level;
                // TODO: ?
            }
            catch (Exception e)
            {
                _log.Error("Prefix", e);
            }
        }

        public static void Postfix(Progression __instance, int ___Level, int __state)
        {
            try
            {
                if (!ConnectionManager.Instance.IsServer
                    || __state == -1
                    || __state == ___Level
                    || !(__instance.parent is EntityPlayerLocal player))
                {
                    return;
                }

                _log.Trace($"change in player level detected: {__state} -> {___Level}");
                //WeightManager.UpdatePlayerWeight(player, player.inventory.GetSlots(), player.bag.GetSlots(), player.equipment);
                // TODO: add behavior

                // TOOD: calculate and push amnesia treatment and therapy costs as levels change...? Or perhaps as skill points change?

                // TODO: (in another hook) as each skill point is applied, record that added skill point in permanent memory and write to drive
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
