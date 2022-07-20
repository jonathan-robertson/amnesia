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
                        API.AdjustToMaxOrRemainingLivesChange(player);
                        break;
                    case RespawnType.Died:
                        log.Trace($"RespawnType.Died");



                        // TODO: KICKING DOES NOT TRIGGER OnPlayerDisconnected, it seems... but does give the client a message :-/
                        //GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default, factionResetKickReason));
                        //clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerDisconnect>().Setup(player));


                        // TODO: if we're going to kick, we have to update death state to not dead first (or after?) and make sure the game knows it
                        //if (API.ResetAfterDisconnectMap.ContainsKey(clientInfo.entityId)) {
                        //    ConnectionManager.Instance.DisconnectClient(clientInfo);
                        //}

                        // Try without controlled delay
                        //if (API.ResetAfterDisconnectMap.TryGetValue(clientInfo.entityId, out var value) && value) {
                        /*if (API.ResetAfterDisconnectMap.ContainsKey(clientInfo.entityId)) {
                            //API.ResetAfterDisconnectMap[clientInfo.entityId] = false; // try disconnecting, but only once
                            GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default(DateTime), API.QuestResetKickReason));
                        }*/

                        // Try with controlled delay
                        /*
                        clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerDenied>().Setup(kickData));
                        string str = clientInfo.ToString();
                        string str2 = "Kicking player (";
                        GameUtils.KickPlayerData kickPlayerData = kickData;
                        Log.Out(str2 + kickPlayerData.ToString() + "): " + str);
                        ThreadManager.StartCoroutine(disconnectLater(0.5f, clientInfo));
                        */
                        break;
                }
            } catch (Exception e) {
                log.Error("Failed to handle PlayerSpawnedInWorld event.", e);
            }
        }

        /*
        protected static IEnumerator disconnectLater(float _delayInSec, ClientInfo _clientInfo) {
            // TODO: whisper to this player explaining upcoming disconnection?
            yield return new WaitForSecondsRealtime(_delayInSec);
            SingletonMonoBehaviour<ConnectionManager>.Instance.DisconnectClient(_clientInfo, false, false);
            yield break;
        }
        */
    }
}
