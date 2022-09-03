using Amnesia.Data;
using System;
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

            log.Info($"resetting {player.GetDebugName()}");

            bool needsSave = false;

            if (Config.ForgetSchematics) {
                CraftingManager.GetRecipes().ForEach(recipe => {
                    if (recipe.IsLearnable) {
                        player.SetCVar(recipe.GetName(), 0);
                    }
                });
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
                        player.Progression.SkillPoints = player.QuestJournal.quests
                            .Where(q => q.CurrentState == Quest.QuestState.Completed)
                            .Select(q => q.Rewards
                                .Where(r => r is RewardSkillPoints)
                                .Select(r => Convert.ToInt32((r as RewardSkillPoints).Value))
                                .Sum())
                            .Sum();
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
    }
}
