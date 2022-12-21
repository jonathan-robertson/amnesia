﻿using Amnesia.Data;
using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers {
    internal class PlayerSpawnedInWorld {
        private static readonly ModLog log = new ModLog(typeof(PlayerSpawnedInWorld));

        /**
         * <summary>Handle player spawning into world.</summary>
         * <param name="clientInfo">The client currently spawning in.</param>
         * <param name="respawnType">The type of respawn.</param>
         * <param name="pos">The position this player is respawning to.</param>
         * <remarks>This mod supports being dropped into an existing game, thanks to how we handle this process.</remarks>
         */
        public static void Handle(ClientInfo clientInfo, RespawnType respawnType, Vector3i pos) {
            if (!Config.Loaded) { return; }
            try {
                if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player) || !player.IsAlive()) {
                    return; // exit early if player cannot be found in active world or is dead
                }
                switch (respawnType) {
                    case RespawnType.EnterMultiplayer: // first-time login for new player
                        if (Config.EnablePositiveOutlookOnMemoryLoss && respawnType == RespawnType.EnterMultiplayer) {
                            _ = player.Buffs.AddBuff(Values.PositiveOutlookBuff);
                        }
                        HandleStandardRespawnSteps(player);
                        break;
                    case RespawnType.JoinMultiplayer: // existing player rejoining
                        // grace period should continue only so long as you don't disconnect
                        player.Buffs.RemoveBuff(Values.PostBloodmoonLifeProtectionBuff);
                        HandleStandardRespawnSteps(player);
                        break;
                    case RespawnType.Died: // existing player returned from death
                        HandleStandardRespawnSteps(player);
                        break;
                }
            } catch (Exception e) {
                log.Error("Failed to handle PlayerSpawnedInWorld event.", e);
            }
        }

        /**
         * <summary>Process steps common to enter/join/death.</summary>
         * <param name="player">Player to process steps for.</param>
         * <param name="respawnType">The type of respawn (enter/join/death)</param>
         * <remarks>This method also handles cleanup when player was already dead on Enter/Join (happens if player logged out while dead).</remarks>
         */
        private static void HandleStandardRespawnSteps(EntityPlayer player) {

            // Refresh rule for when to warn
            player.SetCVar(Values.WarnAtLifeCVar, Config.WarnAtLife);

            // Remove Positive Outlook if admin disabled it since player's last login
            if (!Config.EnablePositiveOutlookOnMemoryLoss) {
                player.Buffs.RemoveBuff(Values.PositiveOutlookBuff);
            }

            // Apply/Remove memory protection based on configuration
            if (Config.ProtectMemoryDuringBloodmoon) {
                // add or remove protection based on whether BM is active
                if (GameManager.Instance.World.aiDirector.BloodMoonComponent.BloodMoonActive) {
                    _ = player.Buffs.AddBuff(Values.BloodmoonLifeProtectionBuff);
                } else {
                    player.Buffs.RemoveBuff(Values.BloodmoonLifeProtectionBuff);
                }
            } else {
                // remove/clean up since protection is inactive
                player.Buffs.RemoveBuff(Values.BloodmoonLifeProtectionBuff);
                player.Buffs.RemoveBuff(Values.PostBloodmoonLifeProtectionBuff);
            }

            // Update player's max/remaining lives to fit new MaxLives changes if changed since last login
            Config.AdjustToMaxOrRemainingLivesChange(player);
        }
    }
}
