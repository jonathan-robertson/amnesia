using Amnesia.Data;
using Amnesia.Utilities;
using System;
using System.Collections.Generic;

namespace Amnesia.Commands {
    internal class Amnesia : ConsoleCmdAbstract {
        private static readonly string[] Commands = new string[] {
            "amnesia",
            "amn"
        };

        public override string[] GetCommands() {
            return Commands;
        }

        public override string GetDescription() {
            return "Configure or adjust settings for the Amnesia mod.";
        }

        public override string GetHelp() {
            int i = 1;
            int j = 1;
            return $@"Usage:
  {i++}. {GetCommands()[0]}
  {i++}. {GetCommands()[0]} list
  {i++}. {GetCommands()[0]} config <{string.Join(" / ", Config.FieldNames)}> <value>
  {i++}. {GetCommands()[0]} update <user id / player name / entity id> <remainingLives>
Description Overview
{j++}. View current mod options
{j++}. List remaining lives for all players
{j++}. Configure a given option
{Config.FieldNamesAndDescriptions}
{j++}. Update a specific player's remaining lives";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo) {
            try {
                if (_params.Count == 0) {
                    SdtdConsole.Instance.Output(Config.AsString());
                    return;
                }
                switch (_params[0].ToLower()) {
                    case "resetplayer": // TODO: remove
                        if (_senderInfo.RemoteClientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(_senderInfo.RemoteClientInfo.entityId, out var playerToReset)) {
                            SdtdConsole.Instance.Output("RemoteClientInfo and/or player is null; if using telnet, you need to actually be inside the game instead.");
                            return;
                        }
                        PlayerHelper.ResetPlayer(playerToReset);
                        return;
                    case "quests":
                        if (_senderInfo.RemoteClientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(_senderInfo.RemoteClientInfo.entityId, out var playerForQuestList)) {
                            SdtdConsole.Instance.Output("RemoteClientInfo and/or player is null; if using telnet, you need to actually be inside the game instead.");
                            return;
                        }
                        if (playerForQuestList.QuestJournal == null || playerForQuestList.QuestJournal.quests == null || playerForQuestList.QuestJournal.quests.Count == 0) {
                            SdtdConsole.Instance.Output("We're not seeing that you currently have any quests.");
                            return;
                        }
                        playerForQuestList.QuestJournal.quests.ForEach(quest => {
                            SdtdConsole.Instance.Output($"- {quest.ID}: {quest.GetPOIName()}; {quest.CurrentState}");
                            if (quest.Objectives != null) {
                                quest.Objectives.ForEach(o => SdtdConsole.Instance.Output($"    - {o.ID}: {o.ObjectiveValueType}; {o.ObjectiveState}; {o.Description}"));
                            }
                        });
                        return;
                    case "resetquests": // TODO: remove
                        if (_senderInfo.RemoteClientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(_senderInfo.RemoteClientInfo.entityId, out var playerToQuestReset)) {
                            SdtdConsole.Instance.Output("RemoteClientInfo and/or player is null; if using telnet, you need to actually be inside the game instead.");
                            return;
                        }
                        SdtdConsole.Instance.Output($"Queuing up {_senderInfo.RemoteClientInfo.entityId} for reset on disconnect.");
                        if (!API.Obituary.ContainsKey(_senderInfo.RemoteClientInfo.entityId)) {
                            API.Obituary.Add(_senderInfo.RemoteClientInfo.entityId, true);
                        }
                        //SdtdConsole.Instance.Output($"Disconnecting {_senderInfo.RemoteClientInfo.entityId} for reset.");
                        //ThreadManager.StartCoroutine(DisconnectWithDelay(_senderInfo.RemoteClientInfo));
                        return;
                    case "resettest": // TODO: remove
                        if (_senderInfo.RemoteClientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(_senderInfo.RemoteClientInfo.entityId, out var playerForResetTest)) {
                            SdtdConsole.Instance.Output("RemoteClientInfo and/or player is null; if using telnet, you need to actually be inside the game instead.");
                            return;
                        }
                        GameEventManager.Current.HandleAction("game_on_death", playerForResetTest, playerForResetTest, false);
                        return;
                    case "points": // TODO: remove
                        if (_senderInfo.RemoteClientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(_senderInfo.RemoteClientInfo.entityId, out var player)) {
                            SdtdConsole.Instance.Output("RemoteClientInfo and/or player is null; if using telnet, you need to actually be inside the game instead.");
                            return;
                        }

                        if (player.QuestJournal == null || player.QuestJournal.QuestFactionPoints == null || player.QuestJournal.QuestFactionPoints.Count == 0) {
                            SdtdConsole.Instance.Output("QuestJournal and/or QuestFactionPoints is null; try again after completing at least 1 quest.");
                            return;
                        }

                        foreach (var kvp in player.QuestJournal.QuestFactionPoints) {
                            SdtdConsole.Instance.Output($"{kvp.Key}: {kvp.Value}");
                        }
                        return;
                    case "list":
                        if (_params.Count != 1) {
                            break;
                        }

                        // TODO: is there a way to check this in player data without the players needing to be on?

                        if (GameManager.Instance.World.Players.Count == 0) {
                            SdtdConsole.Instance.Output("There are no players currently online");
                            return;
                        }
                        GameManager.Instance.World.Players.list.ForEach(p => {
                            SdtdConsole.Instance.Output($"{p.GetCVar(Values.RemainingLivesCVar)}/{p.GetCVar(Values.MaxLivesCVar)} remaining for {p.GetDebugName()} ({p.entityId})");
                        });
                        return;
                    case "config":
                        if (_params.Count != 3) {
                            break;
                        }
                        HandleConfig(_params);
                        return;
                    case "update":
                        if (_params.Count != 3) {
                            break;
                        }
                        HandleUpdate(_params);
                        return;
                    default:
                        SdtdConsole.Instance.Output("Invald parameter provided");
                        return;
                }
                SdtdConsole.Instance.Output("Invald request");
            } catch (Exception e) {
                SdtdConsole.Instance.Output($"Exception encountered: \"{e.Message}\"\n{e.StackTrace}");
            }
        }

        private void HandleConfig(List<string> _params) {
            if (Config.MaxLivesName.EqualsCaseInsensitive(_params[1])) {
                ApplyInt(_params[2], v => {
                    Config.SetMaxLives(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.WarnAtLifeName.EqualsCaseInsensitive(_params[1])) {
                ApplyInt(_params[2], v => {
                    Config.SetWarnAtLife(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.EnablePositiveOutlookName.EqualsCaseInsensitive(_params[1])) {
                ApplyBool(_params[2], v => {
                    Config.SetEnablePositiveOutlook(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.ForgetLevelsAndSkillsName.EqualsCaseInsensitive(_params[1])) {
                ApplyBool(_params[2], v => {
                    Config.SetResetLevels(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.ForgetActiveQuests.EqualsCaseInsensitive(_params[1])) {
                ApplyBool(_params[2], v => {
                    Config.SetResetQuests(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.ForgetIntroQuests.EqualsCaseInsensitive(_params[1])) {
                ApplyBool(_params[2], v => {
                    Config.SetClearIntroQuests(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.ForgetInactiveQuests.EqualsCaseInsensitive(_params[1])) {
                ApplyBool(_params[2], v => {
                    Config.SetResetFactionPoints(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }

            SdtdConsole.Instance.Output("Invald parameter provided");
        }

        private void HandleUpdate(List<string> _params) {
            if (!int.TryParse(_params[2], out int remainingLives)) {
                SdtdConsole.Instance.Output("Unable to parse value: must be of type int");
                return;
            }
            ClientInfo clientInfo = ConsoleHelper.ParseParamIdOrName(_params[1], true, false);
            if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player)) {
                SdtdConsole.Instance.Output("Unable to find this player; note: player must be online");
                return;
            }
            Config.SetRemainingLives(player, remainingLives);
            SdtdConsole.Instance.Output($"Updated lives remaining for {player.GetDebugName()} to {remainingLives}");
        }

        private static bool ApplyInt(string param, Action<int> onSuccess) {
            if (!int.TryParse(param, out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting int");
                return false;
            }
            onSuccess(value);
            return true;
        }

        private static bool ApplyBool(string param, Action<bool> onSuccess) {
            if (!bool.TryParse(param, out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return false;
            }
            onSuccess(value);
            return true;
        }
    }
}
