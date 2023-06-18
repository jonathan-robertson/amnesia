using System;
using System.Globalization;
using static Quest;

namespace Amnesia.Utilities
{
    internal class QuestHelper
    {
        private const string QUEST_GROUP_BASIC_SURVIVAL = "Basic Survival";
        private const string QUEST_GROUP_WHITE_RIVER_CITIZEN = "White River Citizen";

        private static readonly ModLog<QuestHelper> _log = new ModLog<QuestHelper>();

        /// <summary>
        /// Remove quests without having to disconnect the player.
        /// </summary>
        /// <param name="player">Player to remove quests for.</param>
        /// <param name="clientInfo">Connection to send removal requests through.</param>
        public static void RemoveNonIntroQuests(EntityPlayer player, ClientInfo clientInfo)
        {
            for (var i = 0; i < player.QuestJournal.quests.Count; i++)
            {
                var quest = player.QuestJournal.quests[i];
                if (!IsStarterQuest(quest))
                {
                    _log.Trace($"Forgetting quest {quest.ID} ({quest.QuestClass.Name}) from player {player.entityId}");
                    clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageConsoleCmdClient>().Setup($"removequest {quest.ID}", true));
                }
            }
        }

        private static bool IsStarterQuest(Quest quest)
        {
            return QUEST_GROUP_BASIC_SURVIVAL.Equals(quest.QuestClass.GroupName)
                || QUEST_GROUP_WHITE_RIVER_CITIZEN.Equals(quest.QuestClass.GroupName);
        }

        ///// <summary>
        ///// Remove all quests from the given player based on admin configuration.
        ///// </summary>
        ///// <param name="player">The player to remove quests for.</param>
        ///// <returns>Whether any quests were removed.</returns>
        //public static bool ResetQuests(EntityPlayer player)
        //{
        //    try
        //    {
        //        var changed = false;
        //        for (var i = 0; i < player.QuestJournal.quests.Count; i++)
        //        {
        //            changed = changed || RemoveQuest(player, player.QuestJournal.quests[i]);
        //        }
        //        _log.Trace($"Quests Changed after RemoveQuests? {changed}");
        //        changed = changed || GiveStarterQuestIfMissing(player);
        //        _log.Trace($"Quests Changed after GiveStarterQuestIfMissing? {changed}");
        //        return changed;
        //    }
        //    catch (Exception e)
        //    {
        //        _log.Error("Failed to reset quests.", e);
        //        return true; // tell server to disconnect player
        //    }
        //}

        //        private static bool RemoveQuest(EntityPlayer player, Quest quest)
        //        {
        //            _log.Trace($"{quest.ID}\n  - CurrentState: {quest.CurrentState}\n  - Active: {quest.Active}\n  - IsIntroQuest: {IsIntroQuest(quest)}\n  - QuestClass: {quest.QuestClass}\n  - Tracked: {quest.Tracked}\n!quest.ID.EqualsCaseInsensitive('quest_BasicSurvival1') {!quest.ID.EqualsCaseInsensitive("quest_BasicSurvival1")}");

        //            var questIsActive = quest.Active; // cache calculated value

        //            return IsIntroQuest(quest)
        //                ? Config.ForgetIntroQuests
        //&& (!quest.ID.EqualsCaseInsensitive("quest_BasicSurvival1") || !questIsActive)
        //&& (questIsActive
        //                        ? RemoveActiveQuest(player, quest)
        //                        : RemoveInactiveQuest(player, quest))
        //                : questIsActive
        //                ? Config.ForgetActiveQuests && RemoveActiveQuest(player, quest)
        //                : Config.ForgetInactiveQuests && RemoveInactiveQuest(player, quest);
        //        }

        private static bool IsIntroQuest(Quest quest)
        {
            var idToLower = quest.ID.ToLower();
            return idToLower.StartsWith("quest_BasicSurvival".ToLower()) || idToLower.StartsWith("quest_whiteRiverCitizen".ToLower());
        }

        private static bool RemoveActiveQuest(EntityPlayer player, Quest quest)
        {
            _log.Trace($"Removing {quest.ID}");
            quest.CurrentState = QuestState.Failed;
            HandleUnlockPOI(player.entityId, quest);
            if (quest.SharedOwnerID != -1)
            {
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackageSharedQuest>().Setup(quest.QuestCode, quest.SharedOwnerID, player.entityId, false), false, quest.SharedOwnerID, -1, -1, -1);
            }

            try
            {
                quest.RemoveMapObject(); // TODO: test this
                _log.Debug($"success: removed map object for {quest.ID}");
            }
            catch (Exception e)
            {
                _log.Debug($"fail: remove map object for {quest.ID}: {e.Message}");
            }
            PreProcessTrickyQuestObjectives(player, quest);
            player.QuestJournal.ForceRemoveQuest(quest.ID); // NOTE: removes quest from journal, triggers UnhookQuest, and triggers quest removal delegate

            if (player.QuestJournal.TrackedQuest == quest)
            {
                player.QuestJournal.TrackedQuest = null; // TODO: does this really need to be called for server-side op?
            }
            HandlePartyRemoveQuest(player, quest);
            if (player.QuestJournal.ActiveQuest == quest)
            {
                player.QuestJournal.ActiveQuest = null; // TODO: is this necessary for server-side op?
                player.QuestJournal.RefreshRallyMarkerPositions(); // TODO: this seems like it would probably be necessary... not 100% sure, though
            }
            return true;
        }

        /// <summary>
        /// Server-safe version of quest.HandleUnlockPOI.
        /// </summary>
        /// <param name="playerEntityId">Entity Id to unlock POI with.</param>
        /// <param name="quest">Quest object to unlock.</param>
        private static void HandleUnlockPOI(int playerEntityId, Quest quest)
        {
            if (quest.SharedOwnerID == -1)
            {
                if (quest.GetPositionData(out var pos, PositionDataTypes.POIPosition))
                {
                    QuestEventManager.Current.QuestUnlockPOI(playerEntityId, pos);
                }
            }
        }

        private static void PreProcessTrickyQuestObjectives(EntityPlayer player, Quest quest)
        {
            for (var i = 0; i < quest.Objectives.Count; i++)
            {
                if (quest.Objectives[i] is ObjectiveFetch ||
                    quest.Objectives[i] is ObjectiveClearSleepers ||
                    quest.Objectives[i] is ObjectiveFetchAnyContainer ||
                    quest.Objectives[i] is ObjectiveFetchFromTreasure ||
                    quest.Objectives[i] is ObjectiveFetchFromContainer ||
                    quest.Objectives[i] is ObjectiveBaseFetchContainer)
                {
                    try
                    {
                        // Process for objective.HandleFailed() and objective.RemoveHooks
                        switch (quest.Objectives[i])
                        {
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
                                quest.Objectives[i].HandleFailed();
                                quest.Objectives[i].HandleRemoveHooks();
                                break;
                        }
                        quest.Objectives[i].RemoveNavObject();

                        // Process for objective.RemoveObjectives()
                        if (!(quest.Objectives[i] is ObjectiveFetch))
                        { // if not ObjectiveFetch...
                            quest.Objectives[i].RemoveObjectives();
                        }

                        // Process for quest.UnhookQuest();
                        //quest.RemoveMapObject();
                        QuestEventManager.Current.RemoveObjectiveToBeUpdated(quest.Objectives[i]);
                        _ = quest.Objectives.Remove(quest.Objectives[i]);
                    }
                    catch (Exception e)
                    {
                        _log.Error($@"Failed to process tricky quest objectives:
ID: {quest.Objectives[i].ID}
Owner Class: {quest.Objectives[i].OwnerQuestClass}
Objective Type: {quest.Objectives[i].GetType().Name}", e);
                        throw e;
                    }
                }
            }
        }

        private static void RemoveFetchItems(EntityPlayer player, Quest quest, BaseObjective objective, string questItemClassID)
        { // TODO: TEST
            var expectedItemClass = ItemClass.GetItemClass(questItemClassID, false);
            var expectedItem = new ItemValue(expectedItemClass.Id, false);
            if (expectedItemClass is ItemClassQuest)
            {
                var num = StringParsers.ParseUInt16(objective.ID, 0, -1, NumberStyles.Integer);
                //expectedItemClass = ItemClassQuest.GetItemQuestById(num);
                expectedItem.Seed = num;
            }
            expectedItem.Meta = quest.QuestCode;

            _ = player.bag.DecItem(expectedItem, 1, false);
        }

        /// <summary>
        /// Server-safe version of QuestJournal.HandlePartyRemoveQuest.
        /// </summary>
        /// <param name="player">Player to remove quest for.</param>
        /// <param name="quest">Quest to remove.</param>
        private static void HandlePartyRemoveQuest(EntityPlayer player, Quest quest)
        {
            if (player.IsInParty() && quest.SharedOwnerID == -1)
            {
                for (var i = 0; i < player.Party.MemberList.Count; i++)
                {
                    ConnectionManager.Instance.SendPackage(NetPackageManager.GetPackage<NetPackageSharedQuest>().Setup(quest.QuestCode, player.entityId), false, player.Party.MemberList[i].entityId, -1, -1, -1);
                }
            }
        }

        private static bool RemoveInactiveQuest(EntityPlayer player, Quest quest)
        {
            return player.QuestJournal.quests.Remove(quest);
        }

        private static bool GiveStarterQuestIfMissing(EntityPlayer player)
        { // TODO: TEST THIS
            // TODO: does not fire... perhaps issue this on each player's login?
            //GameEventManager.Current.HandleAction("game_first_spawn", player, player, false, "", "", false);

            // TODO: perhaps.. just create the quest and put it in the quests list? Using QuestJournal.Add might not be necessary at all

            for (var i = 0; i < player.QuestJournal.quests.Count; i++)
            {
                if (player.QuestJournal.quests[i].ID.EqualsCaseInsensitive("quest_BasicSurvival1"))
                {
                    _log.Trace("quest_BasicSurvival1 was present");
                    return false;
                }
            }

            _log.Trace("quest_BasicSurvival1 is missing; trying to add");

            var quest = QuestClass.CreateQuest("quest_BasicSurvival1");
            quest.CurrentState = QuestState.InProgress;
            player.QuestJournal.quests.Add(quest);
            //player.QuestJournal.AddQuest(quest);
            return true;
        }
    }
}
