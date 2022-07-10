using Amnesia.Data;
using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers {
    internal class PlayerSpawnedInWorld {
        private static readonly ModLog log = new ModLog(typeof(PlayerSpawnedInWorld));

        private static readonly string factionResetKickReason = "This server is configured to erase some settings from your player file when you die for the final time. Please feel free to reconnect whenever you're ready.";

        /**
         * <summary>Handle player spawning into world.</summary>
         * <param name="clientInfo">The client currently spawning in.</param>
         * <param name="respawnType">The type of respawn.</param>
         * <param name="pos">The position this player is respawning to.</param>
         * <remarks>This mod supports being dropped into an existing game, thanks to how we handle this process.</remarks>
         */
        public static void Handle(ClientInfo clientInfo, RespawnType respawnType, Vector3i pos) {
            try {
                switch (respawnType) {
                    case RespawnType.EnterMultiplayer:
                    case RespawnType.JoinMultiplayer:
                        // Fetch player if possible
                        if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out EntityPlayer player)) {
                            log.Warn("JoinMultiplayer/EnterMultiplayer event sent from a non-player client... may want to investigate");
                            return; // exit early if player cannot be found in active world
                        }

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
                        API.AdjustToMaxOrRemainingLivesChange(player);
                        break;
                    case RespawnType.Died:
                        // TODO: KICKING DOES NOT TRIGGER OnPlayerDisconnected, it seems... but does give the client a message :-/
                        //GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default, factionResetKickReason));
                        //clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerDisconnect>().Setup(player));
                        if (API.ResetAfterDisconnectMap.ContainsKey(clientInfo.entityId)) {
                            ConnectionManager.Instance.DisconnectClient(clientInfo);
                        }
                        break;
                }
            } catch (Exception e) {
                log.Error("Failed to handle PlayerSpawnedInWorld event.", e);
            }
        }
    }
}
