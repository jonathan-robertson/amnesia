using Amnesia.Data;
using Amnesia.Utilities;
using System;
using System.Collections;
using UnityEngine;

namespace Amnesia.Handlers
{
    internal class SavePlayerData
    {
        private static readonly ModLog<SavePlayerData> _log = new ModLog<SavePlayerData>();

        public static void Handle(ClientInfo clientInfo, PlayerDataFile playerDataFile)
        {
            if (!Config.Loaded) { return; }
            try
            {
                DialogShop.UpdateMoneyTracker(playerDataFile.id, playerDataFile.inventory, playerDataFile.bag);

                if (clientInfo == null
                    || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player))
                {
                    _log.Error($"SavePlayerData called for player {playerDataFile.id} who was not online; this is not expected!");
                    return;
                }

                // note: on first call, record is not expected to be loaded; should be fine after that
                if (PlayerRecord.Entries.TryGetValue(playerDataFile.id, out var record))
                {
                    record.SetLevel(player.Progression.Level);
                    record.ValidateAndRepairChangeIntegrity(player);
                }

                if (!ModApi.Obituary.ContainsKey(clientInfo.entityId))
                {
                    return;
                }
                _ = ModApi.Obituary.Remove(clientInfo.entityId);

                // Check/Give Fragile Memory 
                if (!player.Buffs.HasBuff(Values.BuffFragileMemory))
                {
                    _ = player.Buffs.AddBuff(Values.BuffFragileMemory);
                    _log.Info($"{clientInfo.InternalId.CombinedString} ({player.GetDebugName()}) died and will not be reset, but now has a Fragile Memory.");
                    return;
                }

                // Reset Quests
                // TODO: discontinuing for now since it doesn't work properly: disconnection and wiping doesn't work together
                //  Instead, maybe revisit this and refer back to how Amnesia 1.x.x handled things to see if it can be reproduced
                //// TODO: auto-kick for quest relationship refresh would happen here
                //if (Config.ForgetNonIntroQuests)
                //{
                //    var changed = QuestHelper.RemoveNonIntroQuests(clientInfo, playerDataFile);
                //    // TODO: send net package? Unfortunately, these approaches don't work :[
                //    //clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerData>().Setup(player));
                //    //clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerId>().Setup(player.entityId, player.TeamNumber, clientInfo.latestPlayerData, 4));
                //    if (changed)
                //    {
                //        GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default, Config.QuestResetKickReason));

                //        playerDataFile.bModifiedSinceLastSave = true;
                //        playerDataFile.bDead = true;
                //        playerDataFile.Save(GameIO.GetPlayerDataDir(), clientInfo.InternalId.CombinedString);

                //        //_ = ThreadManager.StartCoroutine(SaveLater(2.0f, clientInfo, player));
                //        // TODO: send packet to open URL in browser for auto re-login?
                //        return;
                //    }
                //}

                // Reset Player
                _log.Info($"{clientInfo.InternalId.CombinedString} ({player.GetDebugName()}) died and has suffered memory loss.");
                PlayerHelper.Rewind(player, record, Config.LevelPenalty > 0 ? Config.LevelPenalty : record.Level - Config.LongTermMemoryLevel);
            }
            catch (Exception e)
            {
                _log.Error("Failed to handle OnSavePlayerData", e);
            }
        }

        protected static IEnumerator SaveLater(float _delayInSec, ClientInfo clientInfo, EntityPlayer player)
        {
            yield return new WaitForSecondsRealtime(_delayInSec);
            WritePlayerData(clientInfo, player);
            yield break;
        }

        private static void WritePlayerData(ClientInfo clientInfo, EntityPlayer player, bool saveMap = false)
        {

            var pdf = new PlayerDataFile();
            pdf.FromPlayer(player);
            pdf.bModifiedSinceLastSave = true;
            pdf.Save(GameIO.GetPlayerDataDir(), clientInfo.InternalId.CombinedString);

            //clientInfo.latestPlayerData = pdf;
            //clientInfo.latestPlayerData.Save(GameIO.GetPlayerDataDir(), clientInfo.InternalId.CombinedString);

            if (saveMap && player.ChunkObserver.mapDatabase != null)
            {
                _ = ThreadManager.AddSingleTask(
                    new ThreadManager.TaskFunctionDelegate(player.ChunkObserver.mapDatabase.SaveAsync),
                    new MapChunkDatabase.DirectoryPlayerId(GameIO.GetPlayerDataDir(),
                    clientInfo.InternalId.CombinedString),
                    null,
                    true);
            }
        }
    }
}
