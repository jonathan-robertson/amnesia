using Amnesia.Data;
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
                log.Trace($"PlayerSpawnedInWorld called for player {clientInfo.entityId}");

                // Fetch player if possible
                if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out EntityPlayer player)) {
                    return; // exit early if player cannot be found in active world
                }
                switch (respawnType) {
                    case RespawnType.EnterMultiplayer:
                    case RespawnType.JoinMultiplayer:
                        if (!player.IsAlive()) {
                            break; // buffs don't work when a player is dead
                        }

                        // Refresh rule for when to warn
                        player.SetCVar(Values.WarnAtLifeCVar, Config.WarnAtLife);

                        // Manage Positive Outlook if admin disabled it since player's last login
                        if (!Config.EnablePositiveOutlook) {
                            player.Buffs.RemoveBuff(Values.PositiveOutlookBuff);
                        }
                        if (Config.EnablePositiveOutlook && respawnType == RespawnType.EnterMultiplayer) {
                            player.Buffs.AddBuff(Values.PositiveOutlookBuff); // give first time login buff (for first life)
                        }

                        // Manage Bloodmoon Life Protection if admin disabled it since player's last login
                        if (!Config.ProtectMemoryDuringBloodmoon || !GameManager.Instance.World.aiDirector.BloodMoonComponent.BloodMoonActive) {
                            player.Buffs.RemoveBuff(Values.BloodmoonLifeProtectionBuff);
                        }
                        if (Config.ProtectMemoryDuringBloodmoon && GameManager.Instance.World.aiDirector.BloodMoonComponent.BloodMoonActive) {
                            player.Buffs.AddBuff(Values.BloodmoonLifeProtectionBuff);
                        }

                        // Update player's max/remaining lives to fit new MaxLives changes if necessary
                        Config.AdjustToMaxOrRemainingLivesChange(player);
                        break;
                    case RespawnType.Died:

                        // Refresh rule for when to warn
                        player.SetCVar(Values.WarnAtLifeCVar, Config.WarnAtLife);

                        // Remove Positive Outlook if admin disabled it since player's last login
                        if (!Config.EnablePositiveOutlook) {
                            player.Buffs.RemoveBuff(Values.PositiveOutlookBuff);
                        }

                        // Remove BloodmoonLifeProtectionBuff if BM has ended
                        if (!Config.ProtectMemoryDuringBloodmoon || !GameManager.Instance.World.aiDirector.BloodMoonComponent.BloodMoonActive) {
                            player.Buffs.RemoveBuff(Values.BloodmoonLifeProtectionBuff);
                        }
                        break;
                }
            } catch (Exception e) {
                log.Error("Failed to handle PlayerSpawnedInWorld event.", e);
            }
        }
    }
}
