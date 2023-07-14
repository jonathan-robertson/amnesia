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

            record.Respec(player);
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

        public static void SkillPointIntegrityCheck(ClientInfo clientInfo, EntityPlayer player, PlayerDataFile playerDataFile, PlayerRecord record)
        {
            if (!TryReadProgression(playerDataFile, player, out var progression)) { return; }

            var current = CountCurrentSKillPoints(progression, out var assignedSkillPoints, out var currentUnassignedSkillPoints);
            var expected = CountExpectedSkillPoints(progression, playerDataFile.questJournal, out var expectedFromLevels, out var expectedFromQuests);

            if (current == expected) { return; }
            if (current < expected)
            {
                _log.Warn($"player {player.entityId} ({player.GetDebugName()} | {ClientInfoHelper.GetUserIdentifier(clientInfo)}) was expected to have {expected} skill points ({expectedFromLevels} from levels and {expectedFromQuests} from quests), but found to only have {current} ({assignedSkillPoints} assigned and {currentUnassignedSkillPoints} unassigned)");
                return;
            }
            // note: from this point forward, we handle the case: current > expected

            _log.Info($"player {player.entityId} ({player.GetDebugName()} | {ClientInfoHelper.GetUserIdentifier(clientInfo)}) was found to have too many skill points based on player level and quests; reducing now.");

            var skillPointsToRemove = current - expected;
            if (skillPointsToRemove <= currentUnassignedSkillPoints)
            {
                progression.SkillPoints = currentUnassignedSkillPoints - skillPointsToRemove;
                _log.Info($"Removed {skillPointsToRemove} unassigned skill points from player {player.entityId} ({player.GetDebugName()} | {ClientInfoHelper.GetUserIdentifier(clientInfo)}).");
            }
            else
            {
                progression.ResetProgression(_resetSkills: true);
                progression.SkillPoints -= skillPointsToRemove;
                record.ReapplySkills(player);
                _log.Info($"Reset progression for, removed {skillPointsToRemove} skill points from, and reapplied all affordable skill points to player {player.entityId} ({player.GetDebugName()} | {ClientInfoHelper.GetUserIdentifier(clientInfo)}).");
            }

            player.Progression = progression; // update player progression data
            SavePlayerDataFile(clientInfo, player);
            SyncPlayerStatChanges(player, true);
            OpenWindow(clientInfo, Values.WindowSkillPointIntegrityCheckReduced);
        }

        public static void SavePlayerDataFile(ClientInfo clientInfo, EntityPlayer player)
        {
            var pdf = new PlayerDataFile();
            pdf.FromPlayer(player);
            clientInfo.latestPlayerData = pdf;
            pdf.Save(GameIO.GetPlayerDataDir(), clientInfo.InternalId.CombinedString);
        }

        public static void SyncPlayerStatChanges(EntityPlayer player, bool progressionChanged = false)
        {
            player.bPlayerStatsChanged = true;
            player.Progression.bProgressionStatsChanged = progressionChanged;
            ConnectionManager.Instance.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerStats>().Setup(player), false, player.entityId);
        }

        public static int CountCurrentSKillPoints(Progression progression, out int assignedSkillPoints, out int unassignedSkillPoints)
        {
            assignedSkillPoints = 0;
            foreach (var kvp in Progression.ProgressionClasses)
            {
                switch (kvp.Value.Type)
                {
                    case ProgressionType.Attribute:
                        for (var i = progression.GetProgressionValue(kvp.Key).Level; i > 1; i--)
                        {
                            assignedSkillPoints += kvp.Value.CalculatedCostForLevel(i);
                        }
                        continue;
                    case ProgressionType.Perk:
                        for (var i = progression.GetProgressionValue(kvp.Key).Level; i > 0; i--)
                        {
                            assignedSkillPoints += kvp.Value.CalculatedCostForLevel(i);
                        }
                        continue;
                    default:
                        continue;
                }
            }
            unassignedSkillPoints = progression.SkillPoints;
            return unassignedSkillPoints + assignedSkillPoints;
        }

        public static int CountExpectedSkillPoints(Progression progression, QuestJournal questJournal, out int skillPointsFromLevels, out int skillPointsFromQuests)
        {
            skillPointsFromLevels = Progression.SkillPointsPerLevel * (progression.Level - 1);
            skillPointsFromQuests = questJournal.GetRewardedSkillPoints();
            return skillPointsFromLevels + skillPointsFromQuests;
        }

        /// <summary>
        /// Try parsing and returning player progression data from the given PlayerDataFile.
        /// </summary>
        /// <param name="playerDataFile">PlayerDataFile containing the latest progression information.</param>
        /// <param name="player">EntityPlayer containing the active data we'll cache the PlayerDataFile.Progression info into.</param>
        /// <param name="progression">Parsed progression data from the given PlayerDataFile.</param>
        /// <returns>Whether the Progression data could be parsed.</returns>
        public static bool TryReadProgression(PlayerDataFile playerDataFile, EntityPlayer player, out Progression progression)
        {
            try
            {
                if (playerDataFile.progressionData.Length > 0L)
                {
                    using (var pooledBinaryReader = MemoryPools.poolBinaryReader.AllocSync(false))
                    {
                        // TODO: is a sync lock necessary?
                        pooledBinaryReader.SetBaseStream(playerDataFile.progressionData);
                        playerDataFile.progressionData.Position = 0L;
                        progression = Progression.Read(pooledBinaryReader, player);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error($"Failed to sync progression data for {player.entityId}", e);
            }
            progression = default;
            return false;
        }

        public static void OpenWindow(ClientInfo clientInfo, string windowGroupName)
        {
            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageConsoleCmdClient>().Setup($"xui open {windowGroupName}", true));
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
