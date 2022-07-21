using Amnesia.Data;
using System;
using System.Globalization;
using System.Linq;
using static Quest;

namespace Amnesia.Utilities {
    internal class QuestHelper {
        private static readonly ModLog log = new ModLog(typeof(QuestHelper));

        /**
         * <summary>Remove all quests from the given player based on admin configuration.</summary>
         * <param name="player">The player to remove quests for.</param>
         * <returns>Whether any quests were removed.</returns>
         */
        public static bool ResetQuests(EntityPlayer player) {
            try {
                var changed = false;
                player.QuestJournal.quests.ToList().ForEach(q => changed = changed || RemoveQuest(player, q));
                log.Trace($"Quests Changed after RemoveQuests? {changed}");
                changed = changed || GiveStarterQuestIfMissing(player);
                log.Trace($"Quests Changed after GiveStarterQuestIfMissing? {changed}");
                return changed;
            } catch (Exception e) {
                log.Error("Failed to reset quests.", e);
                return true; // tell server to disconnect player
            }
        }

        private static bool RemoveQuest(EntityPlayer player, Quest quest) {
            log.Trace($"{quest.ID}\n  - CurrentState: {quest.CurrentState}\n  - Active: {quest.Active}\n  - IsIntroQuest: {IsIntroQuest(quest)}\n  - QuestClass: {quest.QuestClass}\n  - Tracked: {quest.Tracked}\n!quest.ID.EqualsCaseInsensitive('quest_BasicSurvival1') {!quest.ID.EqualsCaseInsensitive("quest_BasicSurvival1")}");

            var questIsActive = quest.Active; // cache calculated value

            if (IsIntroQuest(quest)) {
                if (!Config.ForgetIntroQuests) {
                    return false;
                }

                if (quest.ID.EqualsCaseInsensitive("quest_BasicSurvival1") && questIsActive) {
                    return false;
                }

                return questIsActive
                        ? RemoveActiveQuest(player, quest)
                        : RemoveInactiveQuest(player, quest);
            }

            return questIsActive
                ? Config.ForgetActiveQuests && RemoveActiveQuest(player, quest)
                : Config.ForgetInactiveQuests && RemoveInactiveQuest(player, quest);
        }

        private static bool IsIntroQuest(Quest quest) {
            var idToLower = quest.ID.ToLower();
            return idToLower.StartsWith("quest_BasicSurvival".ToLower()) || idToLower.StartsWith("quest_whiteRiverCitizen".ToLower());
        }

        private static bool RemoveActiveQuest(EntityPlayer player, Quest quest) {
            log.Trace($"Removing {quest.ID}");
            quest.CurrentState = QuestState.Failed;
            HandleUnlockPOI(player.entityId, quest);
            if (quest.SharedOwnerID != -1) {
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackageSharedQuest>().Setup(quest.QuestCode, quest.SharedOwnerID, player.entityId, false), false, quest.SharedOwnerID, -1, -1, -1);
            }

            try {
                quest.RemoveMapObject(); // TODO: test this
                log.Debug($"success: removed map object for {quest.ID}");
            } catch (Exception e) {
                log.Debug($"fail: remove map object for {quest.ID}: {e.Message}");
            }
            PreProcessTrickyQuestObjectives(player, quest);
            player.QuestJournal.ForceRemoveQuest(quest.ID); // NOTE: removes quest from journal, triggers UnhookQuest, and triggers quest removal delegate

            if (player.QuestJournal.TrackedQuest == quest) {
                player.QuestJournal.TrackedQuest = null; // TODO: does this really need to be called for server-side op?
            }
            HandlePartyRemoveQuest(player, quest);
            if (player.QuestJournal.ActiveQuest == quest) {
                player.QuestJournal.ActiveQuest = null; // TODO: is this necessary for server-side op?
                player.QuestJournal.RefreshRallyMarkerPositions(); // TODO: this seems like it would probably be necessary... not 100% sure, though
            }
            return true;
        }

        /**
         * <summary>Server-safe version of quest.HandleUnlockPOI</summary>
         */
        private static void HandleUnlockPOI(int playerEntityId, Quest quest) {
            if (quest.SharedOwnerID == -1) {
                if (quest.GetPositionData(out var pos, PositionDataTypes.POIPosition)) {
                    QuestEventManager.Current.QuestUnlockPOI(playerEntityId, pos);
                }
            }
        }

        private static void PreProcessTrickyQuestObjectives(EntityPlayer player, Quest quest) {
            foreach (var objective in quest.Objectives.Where(objective =>
                   objective is ObjectiveFetch
                || objective is ObjectiveClearSleepers
                || objective is ObjectiveFetchAnyContainer
                || objective is ObjectiveFetchFromTreasure
                || objective is ObjectiveFetchFromContainer
                || objective is ObjectiveBaseFetchContainer
            ).ToList()) {

                try {
                    // Process for objective.HandleFailed() and objective.RemoveHooks
                    switch (objective) {
                        case ObjectiveFetchFromContainer o:
                            RemoveFetchItems(player, quest, o, o.questItemClassID);
                            quest.RemovePositionData((o.FetchMode == ObjectiveFetchFromContainer.FetchModeTypes.Standard) ? PositionDataTypes.FetchContainer : PositionDataTypes.HiddenCache);
                            break;
                        case ObjectiveFetchFromTreasure o:
                            RemoveFetchItems(player, quest, o, ObjectiveFetchFromTreasure.questItemClassID);
                            break;
                        case ObjectiveFetchAnyContainer o:
                            RemoveFetchItems(player, quest, o, o.questItemClassID);
                            break;
                        case ObjectiveBaseFetchContainer o:
                            RemoveFetchItems(player, quest, o, o.questItemClassID);
                            break;
                        case ObjectiveFetch o:
                            // don't trigger HandleFailed or HandleRemoveHooks
                            break;
                        default:
                            objective.HandleFailed();
                            objective.HandleRemoveHooks();
                            break;
                    }
                    objective.RemoveNavObject();

                    // Process for objective.RemoveObjectives()
                    if (!(objective is ObjectiveFetch)) { // if not ObjectiveFetch...
                        objective.RemoveObjectives();
                    }

                    // Process for quest.UnhookQuest();
                    //quest.RemoveMapObject();
                    QuestEventManager.Current.RemoveObjectiveToBeUpdated(objective);
                    quest.Objectives.Remove(objective);
                } catch (Exception e) {
                    log.Error($@"Failed to process tricky quest objectives:
ID: {objective.ID}
Owner Class: {objective.OwnerQuestClass}
Objective Type: {objective.GetType().Name}", e);
                    throw e;
                }
            }
        }

        private static void RemoveFetchItems(EntityPlayer player, Quest quest, BaseObjective objective, string questItemClassID) { // TODO: TEST
            var expectedItemClass = ItemClass.GetItemClass(questItemClassID, false);
            var expectedItem = new ItemValue(expectedItemClass.Id, false);
            if (expectedItemClass is ItemClassQuest) {
                ushort num = StringParsers.ParseUInt16(objective.ID, 0, -1, NumberStyles.Integer);
                //expectedItemClass = ItemClassQuest.GetItemQuestById(num);
                expectedItem.Seed = num;
            }
            expectedItem.Meta = quest.QuestCode;

            player.bag.DecItem(expectedItem, 1, false);
        }

        /**
         * <summary>Server-safe version of QuestJournal.HandlePartyRemoveQuest</summary>
         */
        private static void HandlePartyRemoveQuest(EntityPlayer player, Quest quest) {
            if (player.IsInParty() && quest.SharedOwnerID == -1) {
                player.Party.MemberList.ForEach(member => ConnectionManager.Instance.SendPackage(NetPackageManager.GetPackage<NetPackageSharedQuest>().Setup(quest.QuestCode, player.entityId), false, member.entityId, -1, -1, -1));
            }
        }

        private static bool RemoveInactiveQuest(EntityPlayer player, Quest quest) {
            return player.QuestJournal.quests.Remove(quest);
        }

        private static bool GiveStarterQuestIfMissing(EntityPlayer player) { // TODO: TEST THIS
            // TODO: does not fire... perhaps issue this on each player's login?
            //GameEventManager.Current.HandleAction("game_first_spawn", player, player, false, "", "", false);

            // TODO: perhaps.. just create the quest and put it in the quests list? Using QuestJournal.Add might not be necessary at all

            if (player.QuestJournal.quests.Where(q => q.ID.EqualsCaseInsensitive("quest_BasicSurvival1")).Any()) {
                log.Trace("quest_BasicSurvival1 was present");
                return false;
            }

            log.Trace("quest_BasicSurvival1 is missing; trying to add");

            var quest = QuestClass.CreateQuest("quest_BasicSurvival1");
            quest.CurrentState = QuestState.InProgress;
            player.QuestJournal.quests.Add(quest);
            //player.QuestJournal.AddQuest(quest);
            return true;
        }

        /*
2022-07-20T11:13:51 256.554 INF [Amnesia.Utilities.QuestHelper] TRACE: quest_basicsurvival1
  - CurrentState: InProgress
  - Active: True
  - IsIntroQuest: True
  - QuestClass: QuestClass
  - Tracked: False
!quest.ID.EqualsCaseInsensitive('quest_BasicSurvival1') False
2022-07-20T11:13:51 256.554 INF [Amnesia.Utilities.QuestHelper] TRACE: tier1_fetch
  - CurrentState: InProgress
  - Active: True
  - IsIntroQuest: False
  - QuestClass: QuestClass
  - Tracked: True
!quest.ID.EqualsCaseInsensitive('quest_BasicSurvival1') True
2022-07-20T11:13:51 256.554 INF [Amnesia.Utilities.QuestHelper] TRACE: Removing tier1_fetch
2022-07-20T11:13:51 256.557 ERR [Amnesia.Utilities.QuestHelper] Failed to reset quests.
2022-07-20T11:13:52 256.564 EXC Object reference not set to an instance of an object.
  at Quest.RemoveMapObject () [0x00053] in <e2b6e198645f47e5b8fbb6e48578450b>:0
  at Amnesia.Utilities.QuestHelper.PreProcessTrickyQuestObjectives (EntityPlayer player, Quest quest) [0x00123] in C:\Users\jract\source\repos\amnesia\Amnesia\Utilities\QuestHelper.cs:123
  at Amnesia.Utilities.QuestHelper.RemoveActiveQuest (EntityPlayer player, Quest quest) [0x00075] in C:\Users\jract\source\repos\amnesia\Amnesia\Utilities\QuestHelper.cs:57
  at Amnesia.Utilities.QuestHelper.RemoveQuest (EntityPlayer player, Quest quest) [0x000c8] in C:\Users\jract\source\repos\amnesia\Amnesia\Utilities\QuestHelper.cs:39
  at Amnesia.Utilities.QuestHelper+<>c__DisplayClass1_1.<ResetQuests>b__0 (Quest q) [0x00000] in C:\Users\jract\source\repos\amnesia\Amnesia\Utilities\QuestHelper.cs:19
  at System.Collections.Generic.List`1[T].ForEach (System.Action`1[T] action) [0x00024] in <695d1cc93cca45069c528c15c9fdd749>:0
  at Amnesia.Utilities.QuestHelper.ResetQuests (EntityPlayer player) [0x00023] in C:\Users\jract\source\repos\amnesia\Amnesia\Utilities\QuestHelper.cs:19
UnityEngine.StackTraceUtility:ExtractStringFromException(Object)
Log:Exception(Exception)
Amnesia.Utilities.ModLog:Error(String, Exception) (at C:\Users\jract\source\repos\amnesia\Amnesia\Utilities\ModLog.cs:33)
Amnesia.Utilities.QuestHelper:ResetQuests(EntityPlayer) (at C:\Users\jract\source\repos\amnesia\Amnesia\Utilities\QuestHelper.cs:25)
Amnesia.Handlers.SavePlayerData:Handle(ClientInfo, PlayerDataFile) (at C:\Users\jract\source\repos\amnesia\Amnesia\Handlers\SavePlayerData.cs:46)
ModEvent`2:Invoke(ClientInfo, PlayerDataFile)
GameManager:SavePlayerData(ClientInfo, PlayerDataFile)
NetPackagePlayerData:ProcessPackage(World, GameManager)
ConnectionManager:ProcessPackages(INetConnection, NetPackageDirection, ClientInfo)
ConnectionManager:Update()

         */
    }
}
