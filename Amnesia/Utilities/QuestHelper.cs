﻿using Amnesia.Data;
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
                GiveStarterQuestIfMissing(player);
                return changed;
            } catch (Exception e) {
                log.Error("Failed to reset quests.", e);
                return true; // tell server to disconnect player
            }
        }

        private static bool RemoveQuest(EntityPlayer player, Quest quest) {
            log.Trace($"{quest.ID}\n  - CurrentState: {quest.CurrentState}\n  - Active: {quest.Active}\n  - IsIntroQuest: {IsIntroQuest(quest)}\n  - QuestClass: {quest.QuestClass}\n  - Tracked: {quest.Tracked}");

            var questIsActive = quest.Active; // cache calculated value
            if (Config.ClearIntroQuests && IsIntroQuest(quest)) {
                return questIsActive
                    ? RemoveActiveQuest(player, quest)
                    : RemoveInactiveQuest(player, quest);
            }
            return questIsActive
                ? Config.ResetQuests && RemoveActiveQuest(player, quest)
                : Config.ResetFactionPoints && RemoveInactiveQuest(player, quest);
        }

        private static bool IsIntroQuest(Quest quest) {
            var idToLower = quest.ID.ToLower();
            return idToLower.StartsWith("quest_BasicSurvival".ToLower()) || idToLower.StartsWith("quest_whiteRiverCitizen".ToLower());
        }

        private static bool RemoveActiveQuest(EntityPlayer player, Quest quest) {
            quest.CurrentState = QuestState.Failed;
            HandleUnlockPOI(player.entityId, quest);
            if (quest.SharedOwnerID != -1) {
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackageSharedQuest>().Setup(quest.QuestCode, quest.SharedOwnerID, player.entityId, false), false, quest.SharedOwnerID, -1, -1, -1);
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

                // Process for objective.HandleFailed();
                switch (objective) {
                    case ObjectiveFetchFromContainer o:
                        RemoveFetchItems(player, quest, o, o.questItemClassID);
                        quest.RemovePositionData((o.FetchMode == ObjectiveFetchFromContainer.FetchModeTypes.Standard) ? PositionDataTypes.FetchContainer : PositionDataTypes.HiddenCache);
                        quest.RemoveMapObject();
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
                        RemoveFetchItems(player, quest, o, o.ID);
                        break;
                    default:
                        objective.HandleFailed();
                        break;
                }


                // Process for objective.HandleRemoveHooks()
                objective.HandleRemoveHooks();

                // Process for objective.RemoveObjectives()
                if (!(objective is ObjectiveFetch)) {
                    objective.RemoveObjectives();
                }

                // Process for quest.UnhookQuest();
                QuestEventManager.Current.RemoveObjectiveToBeUpdated(objective);
                quest.Objectives.Remove(objective);
            }
        }

        private static void RemoveFetchItems(EntityPlayer player, Quest quest, BaseObjective objective, string questItemClassID) {
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

        private static void GiveStarterQuestIfMissing(EntityPlayer player) {
            // TODO: does not fire... perhaps issue this on each player's login?
            //GameEventManager.Current.HandleAction("game_first_spawn", player, player, false, "", "", false);
            // TODO: perhaps.. just create the quest and put it in the quests list? Using QuestJournal.Add might not be necessary at all
            log.Warn("GiveStarterQuestIfMissing is NOT YET IMPLEMENTED");
        }
    }
}
