﻿using Amnesia.Data;
using Amnesia.Utilities;
using HarmonyLib;
using System;

namespace Amnesia.Patches
{
    // TODO: try patching Setup

    /// <summary>
    /// Detect level and other stat changes.
    /// </summary>
    /// <remarks>Supports: remote</remarks>
    [HarmonyPatch(typeof(NetPackagePlayerStats), "ProcessPackage")]
    internal class NetPackagePlayerStats_ProcessPackage_Patches
    {
        private static readonly ModLog<NetPackagePlayerStats_ProcessPackage_Patches> _log = new ModLog<NetPackagePlayerStats_ProcessPackage_Patches>();

        public static void Prefix(World _world, int ___entityId, int ___level, bool ___hasProgression, out bool __state)
        {
            __state = false; // default to ignore
            try
            {
                if (!ConnectionManager.Instance.IsServer
                    || !_world.Players.dict.TryGetValue(___entityId, out var player))
                {
                    return;
                }

                // TODO: store previous level in __state, then compare and determine how many skill points to add

                if (player.Progression.Level != ___level)
                {
                    _log.Trace($"refreshing price for player {player.GetDebugName()} due to change in level: {player.Progression.Level} -> {___level}");
                    __state = true;
                }

                if (___hasProgression)
                {
                    // TODO: set skill points since they came from the client and are accurate
                    _log.Debug($"PROGRESSION PREFIX - skillPoints: {player.Progression.SkillPoints}");
                }
            }
            catch (Exception e)
            {
                _log.Error("Prefix", e);
            }
        }

        public static void Postfix(NetPackagePlayerStats __instance, World _world, int ___entityId, bool __state, bool ___hasProgression)
        {
            try
            {
                if (!ConnectionManager.Instance.IsServer
                    || !_world.Players.dict.TryGetValue(___entityId, out var player))
                {
                    return;
                }

                if (__state)
                {
                    DialogShop.UpdatePrices(player);
                }

                if (___hasProgression && PlayerRecord.Entries.TryGetValue(___entityId, out var record))
                {
                    // So... progression is sent when:
                    // - you level up with command prompt
                    // - you learn a perk/skill
                    // - you level up naturally
                    // - you learn a book
                    // - you complete a quest
                    _log.Debug($"PROGRESSION POSTFIX - skillPoints: {player.Progression.SkillPoints}");
                    // Ideas
                    // x omg I can toggle spectator mode to cause this to refresh????
                    // x adding/removing a buff seems to trigger this as well
                    // - NetPackageStatChange (from server to client) would also cause this
                    // - remove 1 xp and add 1 xp back might be a decent, clean/safe way to go about this as well
                    // - add/remove skill point???
                    record.SetUnspentSkillPoints(player.Progression.SkillPoints);
                }
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
