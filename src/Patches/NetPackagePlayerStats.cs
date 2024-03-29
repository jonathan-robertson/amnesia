﻿using Amnesia.Utilities;
using HarmonyLib;
using System;

namespace Amnesia.Patches
{
    /// <summary>
    /// Detect level and other stat changes.
    /// </summary>
    /// <remarks>Supports: remote</remarks>
    [HarmonyPatch(typeof(NetPackagePlayerStats), "ProcessPackage")]
    internal class NetPackagePlayerStats_ProcessPackage_Patches
    {
        private static readonly ModLog<NetPackagePlayerStats_ProcessPackage_Patches> _log = new ModLog<NetPackagePlayerStats_ProcessPackage_Patches>();

        public static void Prefix(World _world, int ___entityId, out int __state)
        {
            __state = -1; // default to ignore
            try
            {
                if (!ConnectionManager.Instance.IsServer
                    || !_world.Players.dict.TryGetValue(___entityId, out var player))
                {
                    return;
                }

                __state = player.Progression.Level;
            }
            catch (Exception e)
            {
                _log.Error("Prefix", e);
            }
        }

        public static void Postfix(World _world, int ___entityId, int __state, int ___level)
        {
            try
            {
                if (!ConnectionManager.Instance.IsServer
                    || !_world.Players.dict.TryGetValue(___entityId, out var player))
                {
                    return;
                }

                if (__state != ___level)
                {
                    DialogShop.UpdatePrices(player);
                }
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
