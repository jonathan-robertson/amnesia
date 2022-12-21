using Amnesia.Data;
using System;
using UnityEngine;

namespace Amnesia.Utilities {
    internal class PlayerHelper {
        private static readonly ModLog<PlayerHelper> log = new ModLog<PlayerHelper>();

        /// <remarks>Most of the following core logic was lifted from ActionResetPlayerData.PerformTargetAction</remarks>
        public static void ResetPlayer(EntityPlayer player) {

            // TODO: setting maxLives at 0 screws everything up; fix it?

            log.Info($"resetting {player.GetDebugName()}");

            var needsSave = false;

            if (Config.ForgetSchematics) {
                var recipes = CraftingManager.GetRecipes();
                for (var i = 0; i < recipes.Count; i++) {
                    if (recipes[i].IsLearnable) {
                        player.SetCVar(recipes[i].GetName(), 0);
                    }
                }
            }

            // Zero out Player KD Stats
            if (Config.ForgetKDR) { // TODO: wrap this reset in a config option
                player.Died = 0;
                player.KilledPlayers = 0;
                player.KilledZombies = 0;
                needsSave = true;
            }

            if (Config.ForgetLevelsAndSkills) {
                player.Progression.ResetProgression(Config.ForgetBooks);
                player.Progression.Level = 1;
                player.Progression.ExpToNextLevel = player.Progression.GetExpForNextLevel();
                player.Progression.ExpDeficit = 0;
                player.Progression.SkillPoints = 0;

                // Return all skill points rewarded from completed quest; should cover vanilla quest_BasicSurvival8, for example
                if (!Config.ForgetIntroQuests) {
                    try {
                        var newSkillPoints = 0;
                        for (var i = 0; i < player.QuestJournal.quests.Count; i++) {
                            if (player.QuestJournal.quests[i].CurrentState == Quest.QuestState.Completed) {
                                var rewards = player.QuestJournal.quests[i].Rewards;
                                for (var j = 0; j < rewards.Count; j++) {
                                    if (rewards[j] is RewardSkillPoints) {
                                        newSkillPoints += Convert.ToInt32((rewards[j] as RewardSkillPoints).Value);
                                    }
                                }
                            }
                        }
                        player.Progression.SkillPoints = newSkillPoints;
                    } catch (Exception e) {
                        log.Error("Failed to scan for completed skillpoints.", e);
                    }
                }

                // Inform client cycles of level adjustment for health/stamina/food/water max values
                player.SetCVar("$LastPlayerLevel", player.Progression.Level);

                // Flush xp tracking counters
                player.SetCVar("_xpFromLoot", player.Progression.Level);
                player.SetCVar("_xpFromHarvesting", player.Progression.Level);
                player.SetCVar("_xpFromKill", player.Progression.Level);
                player.SetCVar("$xpFromLootLast", player.Progression.Level);
                player.SetCVar("$xpFromHarvestingLast", player.Progression.Level);
                player.SetCVar("$xpFromKillLast", player.Progression.Level);

                // TODO: zero out xp debt

                needsSave = true;
            }

            if (needsSave) {
                // Set flags to trigger incorporation of new stats/values into update cycle
                player.Progression.bProgressionStatsChanged = true;
                player.bPlayerStatsChanged = true;
                ConnectionManager.Instance.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerStats>().Setup(player), false, player.entityId);
            }
        }

        public static float AddPositiveOutlookTime(EntityPlayer player, int valueToAdd) {
            if (valueToAdd == 0) {
                return 0;
            }
            var playerRemTime = Math.Max(0, player.GetCVar(Values.PositiveOutlookRemTimeCVar));
            var targetValue = Math.Min(playerRemTime + valueToAdd, Config.PositiveOutlookMaxTime);
            player.SetCVar(Values.PositiveOutlookRemTimeCVar, targetValue);
            if (!player.Buffs.HasBuff(Values.PositiveOutlookBuff)) {
                player.Buffs.AddBuff(Values.PositiveOutlookBuff);
            }
            return targetValue;
        }

        /// <summary>
        /// Give an item to the player, placing it in the player's inventory if possible.
        /// </summary>
        /// <param name="player">EntityPlayer to give item to.</param>
        /// <param name="itemName">Name of the item to give the player.</param>
        /// <param name="count">Number of items to give within a single stack (only works with stackable items).</param>
        public static void GiveItem(EntityPlayer player, string itemName, int count = 1) {
            var itemStack = new ItemStack(ItemClass.GetItem(itemName, true), count);
            var clientInfo = ConnectionManager.Instance.Clients.ForEntityId(player.entityId);
            GiveItemStack(clientInfo, player.GetBlockPosition(), itemStack);
        }

        internal static void GiveItemStack(ClientInfo clientInfo, Vector3i pos, ItemStack itemStack) {
            var entityId = EntityFactory.nextEntityID++;
            GameManager.Instance.World.SpawnEntityInWorld((EntityItem)EntityFactory.CreateEntity(new EntityCreationData {
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
