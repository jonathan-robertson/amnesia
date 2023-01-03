﻿using Amnesia.Data;
using System;
using UnityEngine;

namespace Amnesia.Utilities {
    internal class PlayerHelper {
        private static readonly ModLog<PlayerHelper> log = new ModLog<PlayerHelper>();

        /// <remarks>Most of the following core logic was lifted from ActionResetPlayerData.PerformTargetAction</remarks>
        public static void ResetPlayer(EntityPlayer player) {
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
            if (Config.ForgetKdr) {
                player.Died = 0;
                player.KilledPlayers = 0;
                player.KilledZombies = 0;
                needsSave = true;
            }

            if (Config.ForgetLevelsAndSkills) {
                player.Progression.ResetProgression(Config.ForgetBooks);
                player.Progression.Level = Config.LongTermMemoryLevel;
                player.Progression.ExpToNextLevel = player.Progression.GetExpForNextLevel();
                player.Progression.SkillPoints = Config.LongTermMemoryLevel - 1;

                // Zero out xp debt; the reset has caused enough suffering ;)
                player.Buffs.RemoveBuff(Values.BuffNearDeathTrauma);
                player.Progression.ExpDeficit = 0;
                player.SetCVar("_expdeficit", 0);

                // Return all skill points rewarded from completed quest; should cover vanilla quest_BasicSurvival8, for example
                if (!Config.ForgetIntroQuests) {
                    try {
                        for (var i = 0; i < player.QuestJournal.quests.Count; i++) {
                            if (player.QuestJournal.quests[i].CurrentState == Quest.QuestState.Completed) {
                                var rewards = player.QuestJournal.quests[i].Rewards;
                                for (var j = 0; j < rewards.Count; j++) {
                                    if (rewards[j] is RewardSkillPoints) {
                                        player.Progression.SkillPoints += Convert.ToInt32((rewards[j] as RewardSkillPoints).Value);
                                    }
                                }
                            }
                        }
                    } catch (Exception e) {
                        log.Error("Failed to scan for completed skillpoints.", e);
                    }
                }

                // Inform client cycles of level adjustment for health/stamina/food/water max values
                player.SetCVar("$LastPlayerLevel", player.Progression.Level);

                // Flush xp tracking counters
                player.SetCVar("_xpFromLoot", 0);
                player.SetCVar("_xpFromHarvesting", 0);
                player.SetCVar("_xpFromKill", 0);
                player.SetCVar("$xpFromLootLast", 0);
                player.SetCVar("$xpFromHarvestingLast", 0);
                player.SetCVar("$xpFromKillLast", 0);

                needsSave = true;
            }

            if (needsSave) {
                // Set flags to trigger incorporation of new stats/values into update cycle
                player.Progression.bProgressionStatsChanged = true;
                player.bPlayerStatsChanged = true;
                ConnectionManager.Instance.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerStats>().Setup(player), false, player.entityId);
            }

            _ = player.Buffs.AddBuff(Values.BuffMemoryLossNotification);
            if (Config.PositiveOutlookTimeOnMemoryLoss > 0) {
                log.Trace($"{player.GetDebugName()} will receive the Positive Outlook buff.");
                player.SetCVar(Values.CVarPositiveOutlookRemTime, Config.PositiveOutlookTimeOnMemoryLoss);
                _ = player.Buffs.AddBuff(Values.BuffPositiveOutlook);
            }
        }

        public static float AddPositiveOutlookTime(EntityPlayer player, int valueToAdd) {
            if (valueToAdd == 0) {
                return 0;
            }
            var playerRemTime = Math.Max(0, player.GetCVar(Values.CVarPositiveOutlookRemTime));
            var targetValue = Math.Min(playerRemTime + valueToAdd, Config.PositiveOutlookMaxTime);
            player.SetCVar(Values.CVarPositiveOutlookRemTime, targetValue);
            if (!player.Buffs.HasBuff(Values.BuffPositiveOutlook)) {
                _ = player.Buffs.AddBuff(Values.BuffPositiveOutlook);
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

        /// <summary>
        /// Give an item to the player, placing it in the player's inventory if possible.
        /// </summary>
        /// <param name="clientInfo">ClientInfo for player to send the network package to.</param>
        /// <param name="player">EntityPlayer to give item to.</param>
        /// <param name="itemName">Name of the item to give the player.</param>
        /// <param name="count">Number of items to give within a single stack (only works with stackable items).</param>
        public static void GiveItem(ClientInfo clientInfo, EntityPlayer player, string itemName, int count = 1) {
            var itemStack = new ItemStack(ItemClass.GetItem(itemName, true), count);
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
