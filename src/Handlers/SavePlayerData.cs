using Amnesia.Data;
using Amnesia.Utilities;
using System;
using System.Collections;
using UnityEngine;

namespace Amnesia.Handlers {
    internal class SavePlayerData {
        private static readonly ModLog<SavePlayerData> log = new ModLog<SavePlayerData>();

        public static void Handle(ClientInfo clientInfo, PlayerDataFile playerDataFile) {
            if (!Config.Loaded) { return; }
            try {
                if (!ModApi.Obituary.ContainsKey(clientInfo.entityId)) {
                    return;
                }
                _ = ModApi.Obituary.Remove(clientInfo.entityId);

                if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player)) {
                    log.Warn("EntityWasKilled event sent from a non-player client... may want to investigate");
                    return; // exit early, do not interrupt other mods from processing event
                }

                if (player.Buffs.HasBuff(Values.HardenedMemoryBuff)) {
                    player.Buffs.RemoveBuff(Values.HardenedMemoryBuff);
                    log.Info($"{clientInfo.InternalId.CombinedString} ({player.GetDebugName()}) died but will not be reset, thanks to Hardened Memory (which has now expired).");
                    return;
                }

                if ((Config.ForgetActiveQuests || Config.ForgetInactiveQuests) && QuestHelper.ResetQuests(player)) {
                    // TODO: actually just redesign quest resets to issue remote admin call for client to run locally (an amazing feature!)
                    // =================================================

                    // TODO: fix NRE that client experiences after getting kicked
                    // TODO: delay for a bit?

                    // Safe disconnection that allows the client to affirm the disconnection? - still experiences NRE on disconnection :P
                    //clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerDenied>()
                    //    .Setup(new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default(DateTime), API.QuestResetKickReason)));

                    //ConnectionManager.Instance.DisconnectClient(clientInfo);

                    GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default, Config.QuestResetKickReason));
                    _ = ThreadManager.StartCoroutine(SaveLater(2.0f, clientInfo, player));
                    return;
                }

                // Reset Player
                log.Info($"{clientInfo.InternalId.CombinedString} ({player.GetDebugName()}) died and has suffered memory loss.");
                PlayerHelper.ResetPlayer(player);
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
                _ = ThreadManager.AddSingleTask(
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
