using Amnesia.Data;
using System;
using UnityEngine;

namespace Amnesia.Utilities
{
    internal class PlayerHelper
    {
        private static readonly ModLog<PlayerHelper> _log = new ModLog<PlayerHelper>();

        public static bool TryGetClientInfo(int entityId, out ClientInfo clientInfo)
        {
            clientInfo = ConnectionManager.Instance.Clients.ForEntityId(entityId);
            if (clientInfo == null)
            {
                _log.Error($"Could not retrieve remote player connection for {entityId}");
                return false;
            }
            return true;
        }

        public static bool TryGetClientInfoAndEntityPlayer(World world, int entityId, out ClientInfo clientInfo, out EntityPlayer player)
        {
            clientInfo = ConnectionManager.Instance.Clients.ForEntityId(entityId);
            if (clientInfo == null)
            {
                _log.Error($"Could not retrieve remote player connection for {entityId}");
                player = null;
                return false;
            }
            if (!world.Players.dict.TryGetValue(entityId, out player))
            {
                _log.Error($"Could not retrieve player for {entityId}");
                return false;
            }
            return true;
        }

        public static void Respec(EntityPlayer player)
        {
            if (!PlayerRecord.Entries.TryGetValue(player.entityId, out var record))
            {
                _log.Error($"Unable to find player record under {player.entityId} for Respec Request.");
                return;
            }

            if (!TryGetClientInfo(player.entityId, out var clientInfo))
            {
                _log.Error($"Unable to find client info for player {player.entityId}.");
                return;
            }

            // TODO: use this soon-ish... doesn't work on servers right now
            // https://discord.com/channels/243577046616375297/1120457997190189231
            //TriggerGameEvent(clientInfo, player, "amnesia_respec");

            // TODO: remove when above option is available
            record.Respec(clientInfo, player);
            ConnectionManager.Instance.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerStats>().Setup(player), false, player.entityId);
        }

        /// <remarks>Most of the following core logic was lifted from ActionResetPlayerData.PerformTargetAction</remarks>
        public static void Rewind(EntityPlayer player, PlayerRecord record, int levelsToRewind)
        {
            var needsSave = false;

            if (Config.ForgetKdr)
            {
                needsSave = true;
                player.Died = 0;
                player.KilledPlayers = 0;
                player.KilledZombies = 0;
            }

            if (Config.ForgetSchematics)
            {
                needsSave = true;
                var recipes = CraftingManager.GetRecipes();
                for (var i = 0; i < recipes.Count; i++)
                {
                    if (recipes[i].IsLearnable)
                    {
                        player.SetCVar(recipes[i].GetName(), 0);
                    }
                }
            }

            if (Config.ForgetLevelsAndSkills)
            {
                needsSave = true;
                var targetLevel = Math.Max(Config.LongTermMemoryLevel, player.Progression.Level - levelsToRewind);

                // Reset skills
                //  NOTE: this flushes all skill points and reapplies points from quests (self-healing from mistakes)
                //        if we want to support dynamic/unlimited skill points in the future, we should refactor how reset works.

                // Reset spent skill points
                player.Progression.ResetProgression(true, Config.ForgetBooks, Config.ForgetCrafting);

                // Update level
                player.Progression.Level = targetLevel;
                player.Progression.ExpToNextLevel = player.Progression.GetExpForNextLevel();
                record.SetLevel(player.Progression.Level);

                // Update skills and skill points
                player.Progression.SkillPoints = player.QuestJournal.GetRewardedSkillPoints();
                player.Progression.SkillPoints += Progression.SkillPointsPerLevel * (targetLevel - 1);
                record.ReapplySkills(player);

                // Inform client cycles of level adjustment for health/stamina/food/water max values
                player.SetCVar("$LastPlayerLevel", player.Progression.Level);

                // Flush xp tracking counters
                player.SetCVar("_xpFromLoot", 0);
                player.SetCVar("_xpFromHarvesting", 0);
                player.SetCVar("_xpFromKill", 0);
                player.SetCVar("$xpFromLootLast", 0);
                player.SetCVar("$xpFromHarvestingLast", 0);
                player.SetCVar("$xpFromKillLast", 0);
            }

            if (needsSave)
            {
                // Set flags to trigger incorporation of new stats/values into update cycle
                player.Progression.bProgressionStatsChanged = true;
                player.bPlayerStatsChanged = true;
                ConnectionManager.Instance.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerStats>().Setup(player), false, player.entityId);
            }

            _ = player.Buffs.AddBuff(Values.BuffMemoryLoss);
            if (Config.PositiveOutlookTimeOnMemoryLoss > 0)
            {
                _log.Trace($"{player.GetDebugName()} will receive the Positive Outlook buff.");
                player.SetCVar(Values.CVarPositiveOutlookRemTime, Config.PositiveOutlookTimeOnMemoryLoss);
                _ = player.Buffs.AddBuff(Values.BuffPositiveOutlook);
            }
        }

        /// <summary>
        /// Update player progression data with data received during save.
        /// </summary>
        /// <param name="playerDataFile">PlayerDataFile containing the latest progression information.</param>
        /// <param name="player">EntityPlayer containing the active data we'll cache the PlayerDataFile.Progression info into.</param>
        public static void SyncProgression(PlayerDataFile playerDataFile, EntityPlayer player)
        {
            _log.Trace($"Syncing progression for player {player.entityId}");
            try
            {
                if (playerDataFile.progressionData.Length == 0L)
                {
                    _log.Trace($"No progression data to sync with {player.entityId}");
                    return;
                }

                using (var pooledBinaryReader = MemoryPools.poolBinaryReader.AllocSync(false))
                {
                    // TODO: is a sync lock necessary?
                    pooledBinaryReader.SetBaseStream(playerDataFile.progressionData);
                    playerDataFile.progressionData.Position = 0L;
                    player.Progression = Progression.Read(pooledBinaryReader, player);
                }
            }
            catch (Exception e)
            {
                _log.Error($"Failed to sync progression data for {player.entityId}", e);
            }
        }

        public static float AddPositiveOutlookTime(EntityPlayer player, int valueToAdd)
        {
            if (valueToAdd == 0)
            {
                return 0;
            }
            var playerRemTime = Math.Max(0, player.GetCVar(Values.CVarPositiveOutlookRemTime));
            var targetValue = Math.Min(playerRemTime + valueToAdd, Config.PositiveOutlookMaxTime);
            player.SetCVar(Values.CVarPositiveOutlookRemTime, targetValue);
            if (!player.Buffs.HasBuff(Values.BuffPositiveOutlook))
            {
                _ = player.Buffs.AddBuff(Values.BuffPositiveOutlook);
            }
            return targetValue;
        }

        public static void TriggerGameEvent(ClientInfo clientInfo, EntityPlayer player, string eventName)
        {
            _ = GameEventManager.Current.HandleAction(eventName, null, player, false);
            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageGameEventResponse>().Setup(eventName, clientInfo.entityId, "", "", NetPackageGameEventResponse.ResponseTypes.Approved));
        }

        /// <summary>
        /// Give an item to the player, placing it in the player's inventory if possible.
        /// </summary>
        /// <param name="player">EntityPlayer to give item to.</param>
        /// <param name="itemName">Name of the item to give the player.</param>
        /// <param name="count">Number of items to give within a single stack (only works with stackable items).</param>
        public static void GiveItem(EntityPlayer player, string itemName, int count = 1)
        {
            var itemStack = new ItemStack(ItemClass.GetItem(itemName, true), count);
            var clientInfo = ConnectionManager.Instance.Clients.ForEntityId(player.entityId);
            GiveItemStack(clientInfo, player.GetBlockPosition(), itemStack);
        }

        /// <summary>
        /// Give an item to the player, placing it in the player's inventory if possible.
        /// </summary>
        /// <param name="clientInfo">ClientInfo for player to send the network package to.</param>
        /// <param name="player">EntityPlayer to give item to.</param>
        /// <param name="itemName">Name of the item to give the player.</param>
        /// <param name="count">Number of items to give within a single stack (only works with stackable items).</param>
        public static void GiveItem(ClientInfo clientInfo, EntityPlayer player, string itemName, int count = 1)
        {
            var itemStack = new ItemStack(ItemClass.GetItem(itemName, true), count);
            GiveItemStack(clientInfo, player.GetBlockPosition(), itemStack);
        }

        internal static void GiveItemStack(ClientInfo clientInfo, Vector3i pos, ItemStack itemStack)
        {
            var entityId = EntityFactory.nextEntityID++;
            GameManager.Instance.World.SpawnEntityInWorld((EntityItem)EntityFactory.CreateEntity(new EntityCreationData
            {
                entityClass = EntityClass.FromString("item"),
                id = entityId,
                itemStack = itemStack,
                pos = pos,
                rot = new Vector3(20f, 0f, 20f),
                lifetime = 60f,
                belongsPlayerId = clientInfo.entityId
            }));
            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageEntityCollect>().Setup(entityId, clientInfo.entityId));
            _ = GameManager.Instance.World.RemoveEntity(entityId, EnumRemoveEntityReason.Despawned);
        }
    }
}
