using Amnesia.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amnesia.Utilities {
    internal class PlayerHelper {
        private static readonly ModLog log = new ModLog(typeof(PlayerHelper));

        /**
         * <summary></summary>
         * <remarks>Most of the following core logic was lifted from ActionResetPlayerData.PerformTargetAction</remarks>
         */
        public static void ResetPlayer(EntityPlayer player) {

            // TODO: setting maxLives at 0 screws everything up; fix it?

            log.Info($"resetting {player.GetDebugName()}"); // TODO: remove


            log.Debug($"Resetting Levels? {Config.ForgetLevelsAndSkills}");
            if (Config.ForgetLevelsAndSkills) {
                log.Debug($"Resetting Levels");
                player.Progression.ResetProgression(true);
                player.Progression.Level = 1;
                player.Progression.ExpToNextLevel = player.Progression.GetExpForNextLevel();
                player.Progression.ExpDeficit = 0;
                List<Recipe> recipes = CraftingManager.GetRecipes();
                for (int i = 0; i < recipes.Count; i++) {
                    if (recipes[i].IsLearnable) {
                        player.Buffs.RemoveCustomVar(recipes[i].GetName());
                    }
                }

                log.Debug($"Resetting SkillPoints");
                player.Progression.SkillPoints = 0;

                // Return all skill points rewarded from completed quest; should cover vanilla quest_BasicSurvival8, for example
                log.Debug($"Updating skill points to {player.Progression.SkillPoints}");
                if (!Config.ForgetIntroQuests) {
                    try {
                        player.Progression.SkillPoints = player.QuestJournal.quests
                            .Where(q => q.CurrentState == Quest.QuestState.Completed)
                            .Select(q => q.Rewards
                                .Where(r => r is RewardSkillPoints)
                                .Select(r => Convert.ToInt32((r as RewardSkillPoints).Value))
                                .Sum())
                            .Sum();
                        log.Debug($"Updating skill points to {player.Progression.SkillPoints}");
                    } catch (Exception e) {
                        log.Error("Failed to scan for completed skillpoints.", e);
                    }
                }

                // Zero out Player KD Stats
                player.Died = 0;
                player.KilledPlayers = 0;
                player.KilledZombies = 0;

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

                // Set flags to trigger incorporation of new stats/values into update cycle
                player.Progression.bProgressionStatsChanged = true;
                player.bPlayerStatsChanged = true;
                ConnectionManager.Instance.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerStats>().Setup(player), false, player.entityId);
            }
        }
    }
}
