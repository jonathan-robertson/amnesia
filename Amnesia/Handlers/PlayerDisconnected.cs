using Amnesia.Data;
using Amnesia.Utilities;
using System;
using System.Linq;

namespace Amnesia.Handlers {
    internal class PlayerDisconnected {
        private static readonly ModLog log = new ModLog(typeof(PlayerDisconnected));

        public static void Handle(ClientInfo clientInfo, bool forShutdown) {
            try {
                if (API.Obituary.ContainsKey(clientInfo.entityId)) {
                    log.Trace($"Player {clientInfo.entityId} disconnected for quest removal.");

                    if (clientInfo.latestPlayerData == null) {
                        log.Trace($"player data file not present for {clientInfo.entityId}");
                        return;
                    }

                    // TODO: cloning might not be necessary
                    var questJournal = clientInfo.latestPlayerData.questJournal.Clone();

                    if (Config.ForgetActiveQuests) {
                        log.Trace($"triggered: reset quests for {clientInfo.entityId}");
                        questJournal = ResetQuests(clientInfo.entityId, questJournal);
                    } else {
                        log.Trace($"skipped: reset quests for {clientInfo.entityId}");
                    }
                    if (Config.ForgetInactiveQuests) {
                        log.Trace($"triggered: reset faction points for {clientInfo.entityId}");
                        //questJournal.QuestFactionPoints.Clear();
                        // TODO: try looping thorugh each entry and setting to zero (maybe missing entries are skipped in the save process?)
                        var keys = questJournal.QuestFactionPoints.Keys.ToList(); // TODO: if this works, just inline this in the foreach and re-confirm
                        foreach (var key in keys) {
                            //questJournal.QuestFactionPoints[key] = 0; // TODO: also does not work :[
                            questJournal.QuestFactionPoints.Remove(key);
                            log.Debug($"removed faction {key}");
                        }
                    } else {
                        log.Trace($"skipped: reset faction points for {clientInfo.entityId}");
                    }

                    // Attempt to update player
                    if (GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player)) {
                        log.Trace($"updating active player quest journal for {clientInfo.entityId}");
                        player.QuestJournal = questJournal; // TODO: test to see if this is necessary
                    } else {
                        log.Trace($"unable to find active player for {clientInfo.entityId}");
                    }

                    // Update player data file and mark it to be saved momentarily
                    log.Trace($"updating journal in playerDataFile for {clientInfo.entityId}");
                    clientInfo.latestPlayerData.questJournal = questJournal;
                    clientInfo.latestPlayerData.bModifiedSinceLastSave = true;

                    API.Obituary.Remove(clientInfo.entityId);

                    log.Trace($"Player {clientInfo.entityId} quest removal succeeded.");
                } else {
                    log.Trace($"Player {clientInfo.entityId} disconnected but was not queued for quest removal.");
                }
            } catch (Exception e) {
                log.Error($"Player {clientInfo.entityId} quest removal failed.", e);
            }
        }

        public static QuestJournal ResetQuests(int entityId, QuestJournal questJournal) { // TODO: make private

            // TODO: scan for furthest completed intro quest and grant that further down in method if ClearIntroQuests is enabled


            // TODO: remove all fetch quest boxes from this player's inventory. It would usually be removed in objectives, but only for LocalPlayer

            // create a new [required] entityLocalPlayer with the entity ID so deeper commands can complete
            EntityPlayerLocal epl = new EntityPlayerLocal {
                entityId = entityId
            };
            questJournal.quests
                .Where(quest => Config.ForgetIntroQuests || !IsIntroQuest(quest))
                .Where(quest => quest.CurrentState == Quest.QuestState.InProgress || quest.CurrentState == Quest.QuestState.ReadyForTurnIn)
                .ToList().ForEach(quest => {
                    try {
                        quest.OwnerJournal.OwnerPlayer = epl;
                        quest.Objectives.Clear();
                        log.Trace($"attempt: RemoveQuest {quest.ID}, code: {quest.QuestCode}, sharedOwnerCode: {quest.SharedOwnerID}, OwnerJournal.OwnerPlayer == null: {quest.OwnerJournal.OwnerPlayer == null}, player: {entityId}");
                        questJournal.RemoveQuest(quest); // TODO: test with Fetch Item in inventory
                        log.Trace($"success: RemoveQuest {quest.ID}, code: {quest.QuestCode}, sharedOwnerCode: {quest.SharedOwnerID}, OwnerJournal.OwnerPlayer == null: {quest.OwnerJournal.OwnerPlayer == null}, player: {entityId}");
                    } catch (Exception e) {
                        log.Error($"failure: RemoveQuest {quest.ID}, code: {quest.QuestCode}, sharedOwnerCode: {quest.SharedOwnerID}, OwnerJournal.OwnerPlayer == null: {quest.OwnerJournal.OwnerPlayer == null}, player: {entityId}", e);
                    }
                });

            // Start player with intro quest if all quests including intro quests were removed
            if (Config.ForgetIntroQuests && QuestClass.s_Quests.ContainsKey("quest_BasicSurvival1")) {
                Quest quest = QuestClass.CreateQuest("quest_BasicSurvival1");
                if (quest != null) {
                    questJournal.AddQuest(quest);
                    log.Debug($"Added back quest_BasicSurvival1 quest to {entityId}");
                } else {
                    log.Debug($"Unable to find quest_BasicSurvival1; could not add quest to {entityId}");
                }
            }

            return questJournal;
        }

        // TODO: make private or move to GameMessage class
        public static bool IsIntroQuest(Quest quest) {
            var idToLower = quest.ID.ToLower();
            if (idToLower.StartsWith("quest_BasicSurvival".ToLower()) || idToLower.StartsWith("quest_whiteRiverCitizen".ToLower())) {
                return true;
            }
            return false;
        }


        //latestPlayerData.Save(GameIO.GetPlayerDataDir(), clientInfo.InternalId.CombinedString);
        //GameManager.Instance.SavePlayerData(clientInfo, latestPlayerData);

        // TODO: this command could be used to trigger a Game Event
        //GameEventManager.Current.HandleAction("")


        /*
        var resetLevels = true;
        var resetQuests = true;
        var removeSharedQuests = false;

        if (resetQuests) {
            var quests = clientInfo.latestPlayerData.questJournal.quests
                    .Where(q => clearIntroQuests || !IsIntroQuest(q))
                    .Where(q => q.CurrentState == Quest.QuestState.InProgress || q.CurrentState == Quest.QuestState.ReadyForTurnIn)
                    .ToList();
            quests.ForEach(q => {
                log.Debug($"planning to wipe quest: {q.ID}, {q.QuestCode}");
            });
            quests.ForEach(q => {
                try {
                    log.Debug($"wiping quest: {q.ID}, {q.QuestCode}");
                    player.QuestJournal.RemoveQuest(q);
                    log.Debug("quest wipe successful");
                } catch (Exception e) {
                    log.Warn($"encountered error attempting to remove quest {q.ID}; attempting to recover by using ForceRemoveQuest");
                    player.QuestJournal.ForceRemoveQuest(q.ID);
                    log.Debug("ForceRemoveQuest successfully removed quest");
                }
            });

            // Return skill points from quest_BasicSurvival8 if player had completed this quest
            if (resetLevels && !clearIntroQuests) {
                Quest quest = player.QuestJournal.FindQuest("quest_BasicSurvival8");
                if (quest != null && quest.CurrentState == Quest.QuestState.Completed) {
                    foreach (var reward in quest.Rewards) {
                        if (reward is RewardSkillPoints) {
                            int count = Convert.ToInt32((reward as RewardSkillPoints).Value);
                            player.Progression.SkillPoints += count;
                            player.Progression.bProgressionStatsChanged = true;
                            player.bPlayerStatsChanged = true;
                            SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerStats>().Setup(player), false, player.entityId);
                        }
                    }
                }
            }

            // Start player with intro quest if all quests including intro quests were removed
            if (clearIntroQuests && QuestClass.s_Quests.ContainsKey("quest_BasicSurvival1")) {
                Quest quest = QuestClass.CreateQuest("quest_BasicSurvival1");
                if (quest != null) {
                    player.QuestJournal.AddQuest(quest);
                }
            }

            //player.QuestJournal.TradersByFaction[]
        }

        if (removeSharedQuests) {
            player.QuestJournal.RemoveAllSharedQuests();
        }
        */


        /*
        // TODO: see NetPackagePlayerDisconnect

        if (clientInfo == null) {
            log.Debug("cannot move forward: clientInfo == null");
            return;
        } else {
            log.Debug("clientInfo != null");
        }
        log.Debug($"OnPlayerDisconnected client: {clientInfo.entityId}, forShutdown: {forShutdown}");

        foreach (var otherPlayer in GameManager.Instance.World.Players.list) {
            // TODO: is this step actually necessary?
            otherPlayer.QuestJournal.RemoveSharedQuestForOwner(clientInfo.entityId);
        }

        var playerDataFile = clientInfo.latestPlayerData;
        if (playerDataFile == null) {
            log.Debug("cannot move forward: playerDataFile == null");
            return;
        } else {
            log.Debug("playerDataFile != null");
        }
        if (playerDataFile.questJournal == null) {
            log.Debug("cannot move forward: playerDataFile.questJournal == null");
            return;
        } else {
            log.Debug("playerDataFile.questJournal != null");
        }

        // reset trader relationships
        if (playerDataFile.questJournal.QuestFactionPoints != null) {
            log.Debug("playerDataFile.questJournal.QuestFactionPoints != null");
            foreach (var kvp in playerDataFile.questJournal.QuestFactionPoints) {
                log.Debug($"removing faction points for {clientInfo.entityId} - {kvp.Key}: {kvp.Value}");
            }
            playerDataFile.questJournal.QuestFactionPoints.Clear();
        } else {
            log.Debug("playerDataFile.questJournal.QuestFactionPoints == null");
        }
        playerDataFile.bModifiedSinceLastSave = true;
        */


        //GameManager.Instance.SavePlayerData(clientInfo, playerDataFile);

        //GameEventManager.Current.HandleAction("")

        // NOTE: DOES NOT WORK :-/
        // TODO: cancel any active quests
        // TODO: delete quest list
        // TODO: reset faction relationships
        //player.QuestJournal.FailAllActivatedQuests(); // TODO: not necessary... right? cause we already died
        // player.QuestJournal.FailAllSharedQuests();
        //player.QuestJournal.ActiveQuest.MarkFailed();
        //player.QuestJournal.ClearSharedQuestMarkers();


        //player.QuestJournal.ClearTraderDataTier(-1, new Vector2(1, 2)); // TODO: find trader positions

        //player.QuestJournal.GetCurrentFactionTier()
        //player.QuestJournal.GetTraderData
        //player.QuestJournal.GetTraderList

        // TODO: give the starting quest or perhaps... the final stage in the starting quest (the one that grants skill points)
        // TODO: OR initailize with those x number of free skill points


        // remove quests from other players which were shared by this player


        //player.QuestJournal.RemoveSharedQuestByOwner();

        //currentTier = player.QuestJournal.GetCurrentFactionTier(base.NPCInfo.QuestFaction, 0, false);


        /* FIRST
2022-07-07T18:12:07 1125.336 INF [Amnesia.API] resetting Kanaverum
2022-07-07T18:12:07 1125.336 INF [Amnesia.API] [before] QuestFactionPoints for Kanaverum
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] 2: 7
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] 4: 4
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] [after] QuestFactionPoints for Kanaverum
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] [before] quests for Kanaverum
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] quest_basicsurvival1:
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier1_fetch: house_old_bungalow_09
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier1_buried_supplies: UNNAMED
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier1_fetch: house_old_victorian_05
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier1_fetch: survivor_site_08
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier1_fetch: house_burnt_03
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier1_buried_supplies: UNNAMED
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier1_fetch: house_burnt_01
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] quest_tier1complete:
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier2_nexttrader: trader_hugh
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier1_fetch: house_burnt_02
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier1_fetch: cabin_08
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier1_buried_supplies: UNNAMED
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier1_fetch: cave_01
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] tier1_fetch: cabin_04
2022-07-07T18:12:07 1125.337 INF [Amnesia.API] [after] quests for Kanaverum
2022-07-07T18:12:07 1125.339 INF GMSG: Player 'Kanaverum' killed by 'Kanaverum'
         */
        /* SECOND
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] resetting Kanaverum
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] [before] QuestFactionPoints for Kanaverum
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] 2: 8
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] 4: 4
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] [after] QuestFactionPoints for Kanaverum
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] [before] quests for Kanaverum
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] quest_basicsurvival1:
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] tier1_fetch: house_old_bungalow_09
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] tier1_buried_supplies: UNNAMED
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] tier1_fetch: house_old_victorian_05
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] tier1_fetch: survivor_site_08
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] tier1_fetch: house_burnt_03
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] tier1_buried_supplies: UNNAMED
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] tier1_fetch: house_burnt_01
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] quest_tier1complete:
2022-07-07T18:13:55 1233.206 INF [Amnesia.API] tier2_nexttrader: trader_hugh
2022-07-07T18:13:55 1233.207 INF [Amnesia.API] tier1_fetch: house_burnt_02
2022-07-07T18:13:55 1233.207 INF [Amnesia.API] tier1_fetch: cabin_08
2022-07-07T18:13:55 1233.207 INF [Amnesia.API] tier1_buried_supplies: UNNAMED
2022-07-07T18:13:55 1233.207 INF [Amnesia.API] tier1_fetch: cave_01
2022-07-07T18:13:55 1233.207 INF [Amnesia.API] tier1_fetch: cabin_04
2022-07-07T18:13:55 1233.207 INF [Amnesia.API] [after] quests for Kanaverum
         */
    }
}
