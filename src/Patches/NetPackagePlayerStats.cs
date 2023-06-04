using Amnesia.Utilities;
using HarmonyLib;
using System;

namespace Amnesia.Patches
{
    /// <summary>
    /// Detect level and other stat changes.
    /// </summary>
    /// <remarks>Supports: remote </remarks>
    [HarmonyPatch(typeof(NetPackagePlayerStats), "ProcessPackage")]
    internal class NetPackagePlayerStats_ProcessPackage_Patches
    {
        private static readonly ModLog<NetPackagePlayerStats_ProcessPackage_Patches> _log = new ModLog<NetPackagePlayerStats_ProcessPackage_Patches>();

        public static void Prefix(World _world, int ___entityId, int ___level, out bool __state)
        {
            __state = false; // default to ignore
            try
            {
                if (!ConnectionManager.Instance.IsServer
                    || !_world.Players.dict.TryGetValue(___entityId, out var player))
                {
                    return;
                }

                if (player.Progression.Level != ___level)
                {
                    _log.Trace($"refreshing price for player {player.GetDebugName()} due to change in level: {player.Progression.Level} -> {___level}");
                    __state = true;
                }
            }
            catch (Exception e)
            {
                _log.Error("Prefix", e);
            }
        }

        public static void Postfix(World _world, int ___entityId, bool __state)
        {
            try
            {
                if (!ConnectionManager.Instance.IsServer
                    || !__state
                    || !_world.Players.dict.TryGetValue(___entityId, out var player))
                {
                    return;
                }

                DialogShop.UpdatePrices(player);
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
