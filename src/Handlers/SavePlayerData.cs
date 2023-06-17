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

                if (!PlayerRecord.Entries.TryGetValue(playerDataFile.id, out var record))
                {
                    _log.Error($"Unable to retrieve player record for entityId {playerDataFile.id}");
                    return;
                }
                if (PlayerHelper.TryExtractProgressionData(playerDataFile, out var progression))
                {
                    record.SetUnspentSkillPoints(progression.SkillPoints);
                    record.SetLevel(progression.Level);
                }

                if (!ModApi.Obituary.ContainsKey(clientInfo.entityId))
                {
                    return;
                }
                _ = ModApi.Obituary.Remove(clientInfo.entityId);

                if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player))
                {
                    _log.Warn("EntityWasKilled event sent from a non-player client... may want to investigate");
                    return; // exit early, do not interrupt other mods from processing event
                }

                if (!player.Buffs.HasBuff(Values.BuffFragileMemory))
                {
                    _ = player.Buffs.AddBuff(Values.BuffFragileMemory);
                    _log.Info($"{clientInfo.InternalId.CombinedString} ({player.GetDebugName()}) died and will not be reset, but now has a Fragile Memory.");
                    return; // let player know it's time for memory boosters
                }

                // Reset Player
                _log.Info($"{clientInfo.InternalId.CombinedString} ({player.GetDebugName()}) died and has suffered memory loss.");
                // TODO: add support for variable reset
                PlayerHelper.Rewind(player, record, record.Level - Config.LongTermMemoryLevel);
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
