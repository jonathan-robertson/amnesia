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

                        // Remove Positive Outlook if admin disabled it since player's last login
                        if (!Config.EnablePositiveOutlook) {
                            player.Buffs.RemoveBuff("buffAmnesiaPositiveOutlook");
                        }

                        // Grant xp boost on first login if enabled
                        if (Config.EnablePositiveOutlook && respawnType == RespawnType.EnterMultiplayer) {
                            // give first time login buff (for first life)
                            player.Buffs.AddBuff("buffAmnesiaPositiveOutlook");
                        }

                        // Update player's max/remaining lives to fit new MaxLives changes if necessary
                        Config.AdjustToMaxOrRemainingLivesChange(player);
                        break;
                }
            } catch (Exception e) {
                log.Error("Failed to handle PlayerSpawnedInWorld event.", e);
            }
        }
    }
}
