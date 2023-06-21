using Amnesia.Utilities;
using System;

namespace Amnesia.Patches
{
    /// <summary>
    /// Detect level change.
    /// </summary>
    /// <remarks>Supports: local </remarks>
    //[HarmonyPatch(typeof(Progression), "AddLevelExp")]
    // TODO: enable for local support
    internal class Progression_AddLevelExp_Patches
    {
        private static readonly ModLog<Progression_AddLevelExp_Patches> _log = new ModLog<Progression_AddLevelExp_Patches>();

        public static void Prefix(EntityAlive ___parent, int ___Level, out int __state)
        {
            __state = -1; // default to ignore
            try
            {
                // TODO: do these checks in many more parts of the code to support remote having local mod (disable remote clients)
                if (!ConnectionManager.Instance.IsServer
                    || ___parent.isEntityRemote // only track local entity
                    || !(___parent is EntityPlayerLocal player)) // only track EntityPlayerLocal
                {
                    return;
                }

                __state = ___Level;
            }
            catch (Exception e)
            {
                _log.Error("Prefix", e);
            }
        }

        public static void Postfix(EntityAlive ___parent, int ___Level, int __state)
        {
            try
            {
                if (!ConnectionManager.Instance.IsServer
                    || __state == -1
                    || __state == ___Level
                    || !(___parent is EntityPlayerLocal player))
                {
                    return;
                }

                _log.Trace($"change in player level detected: {__state} -> {___Level}");
                DialogShop.UpdatePrices(player);
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
