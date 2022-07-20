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

            /*
            if (resetQuests && !resetAfterDisconnectMap.ContainsKey(player.entityId)) {
                resetAfterDisconnectMap.Add(player.entityId, true);
                // TODO: KICKING DOES NOT TRIGGER OnPlayerDisconnected
                //GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default, factionResetKickReason));
                //clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerDisconnect>().Setup(player));
                ConnectionManager.Instance.DisconnectClient(clientInfo);
            }
            */
            /*
               GameManager.Instance.World.RemoveEntity(clientInfo.entityId, EnumRemoveEntityReason.Despawned);

               log.Debug($"{player.GetDebugName()} -> dead: {player.IsDead()}");

               var playerDataFile = new PlayerDataFile();
               playerDataFile.FromPlayer(player);
               playerDataFile.questJournal.QuestFactionPoints.Clear();



               SingletonMonoBehaviour<ConnectionManager>.Instance.SetClientEntityId(clientInfo, clientInfo.entityId, playerDataFile);
               // TODO: get/store chunkViewDim instead?
               clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerId>().Setup(player.entityId, player.TeamNumber, playerDataFile, 12));


               */
            /*
             * make sure bloaded = true before sending
            SingletonMonoBehaviour<ConnectionManager>.Instance.SetClientEntityId(_cInfo, num4, playerDataFile);
            _cInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerId>().Setup(num4, num2, playerDataFile, _chunkViewDim));
             */



            /* TODO: Maybe this can be added later. Does not work as is written; probably needs to send some net packages to update the client
            var removeLandclaims = true; // TODO: could be a param
            var removeSleepingBag = true; // TODO: could be a param

            if (removeLandclaims) {
                PersistentPlayerData playerDataFromEntityID = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(player.entityId);
                if (playerDataFromEntityID.LPBlocks != null) {
                    playerDataFromEntityID.LPBlocks.Clear();
                }
                NavObjectManager.Instance.UnRegisterNavObjectByOwnerEntity(player, "land_claim");
                SdtdConsole.Instance.Output("removed land claims.");
            }
            if (removeSleepingBag) {
                PersistentPlayerData playerDataFromEntityID = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(player.entityId);
                player.SpawnPoints.Clear();
                playerDataFromEntityID.ClearBedroll();
                SdtdConsole.Instance.Output("removed sleeping bag and respawn target.");
            }
            */
        }
    }
}
