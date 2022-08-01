using Amnesia.Data;
using Amnesia.Utilities;
using System;
using System.Collections;
using UnityEngine;

namespace Amnesia.Handlers {
    internal class SavePlayerData {
        private static readonly ModLog log = new ModLog(typeof(SavePlayerData));

        public static void Handle(ClientInfo clientInfo, PlayerDataFile playerDataFile) {
            if (!Config.Loaded) { return; }
            try {
                log.Trace($"SavePlayerData called for player {clientInfo.entityId}");

                if (!API.Obituary.ContainsKey(clientInfo.entityId)) {
                    log.Trace($"Player {clientInfo.entityId} not queued for reset; skipping");
                    return;
                }
                API.Obituary.Remove(clientInfo.entityId);

                if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player)) {
                    log.Warn("EntityWasKilled event sent from a non-player client... may want to investigate");
                    return; // exit early, do not interrupt other mods from processing event
                }

                /*
                 * TODO: add mechanic to handle final death differently for kill by zombie (or natural death) vs kill by player
                 * Perhaps "Total Bag/Equipment Deletion if not killed by player or Total Bag/Equipment drop if killed by player"
                 */

                var livesRemaining = player.GetCVar(Values.RemainingLivesCVar);
                log.Debug($"{player.GetDebugName()} livesRemaining: {livesRemaining}");

                // cap lives to maximum(sanity check)
                if (livesRemaining > Config.MaxLives) {
                    // "shouldn't" have to do this since we auto-push changes as they're made and on login... but just in case:
                    player.SetCVar(Values.MaxLivesCVar, Config.MaxLives);
                    livesRemaining = Config.MaxLives;
                }

                // Calculate and apply remaining lives
                if (livesRemaining > 0) {
                    log.Trace($"more lives remaining for {player.GetDebugName()}");
                    player.SetCVar(Values.RemainingLivesCVar, livesRemaining - 1);
                    return;
                }

                if ((Config.ForgetActiveQuests || Config.ForgetInactiveQuests) && QuestHelper.ResetQuests(player)) {
                    // TODO: fix NRE that client experiences after getting kicked
                    // TODO: delay for a bit?


                    // Safe disconnection that allows the client to affirm the disconnection? - still experiences NRE on disconnection :P
                    //clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerDenied>()
                    //    .Setup(new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default(DateTime), API.QuestResetKickReason)));

                    //ConnectionManager.Instance.DisconnectClient(clientInfo);

                    GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default(DateTime), Config.QuestResetKickReason));
                    ThreadManager.StartCoroutine(SaveLater(2.0f, clientInfo, player));
                    return;
                }

                // TODO: update cvar, but... is this sanity check really necessary since we *should* be doing this on login and admin change? (confirm this)
                player.SetCVar(Values.WarnAtLifeCVar, Config.WarnAtLife);

                // Reset Player
                log.Trace($"lives have expired for {player.GetDebugName()}");
                PlayerHelper.ResetPlayer(player);
                log.Trace($"returning lives to {Config.MaxLives} for {player.GetDebugName()}");
                player.SetCVar(Values.RemainingLivesCVar, Config.MaxLives);

                player.Buffs.AddBuff("buffAmnesiaMemoryLoss");
                if (Config.EnablePositiveOutlook) {
                    log.Trace($"triggered: apply positive outlook for {player.GetDebugName()}");
                    player.Buffs.AddBuff(Values.PositiveOutlookBuff);
                } else {
                    log.Trace($"skipped: apply positive outlook for {player.GetDebugName()}");
                }
            } catch (Exception e) {
                log.Error("Failed to handle OnSavePlayerData", e);
            }
        }

        protected static IEnumerator SaveLater(float _delayInSec, ClientInfo clientInfo, EntityPlayer player) {
            yield return new WaitForSecondsRealtime(_delayInSec);
            WritePlayerData(clientInfo, player);
            yield break;
        }

        private static void WritePlayerData(ClientInfo clientInfo, EntityPlayer player, bool saveMap = false) {

            var pdf = new PlayerDataFile();
            pdf.FromPlayer(player);
            pdf.bModifiedSinceLastSave = true;
            pdf.Save(GameIO.GetPlayerDataDir(), clientInfo.InternalId.CombinedString);

            //clientInfo.latestPlayerData = pdf;
            //clientInfo.latestPlayerData.Save(GameIO.GetPlayerDataDir(), clientInfo.InternalId.CombinedString);

            if (saveMap && player.ChunkObserver.mapDatabase != null) {
                ThreadManager.AddSingleTask(
                    new ThreadManager.TaskFunctionDelegate(player.ChunkObserver.mapDatabase.SaveAsync),
                    new MapChunkDatabase.DirectoryPlayerId(GameIO.GetPlayerDataDir(),
                    clientInfo.InternalId.CombinedString),
                    null,
                    false,
                    true);
            }
        }
    }
}
