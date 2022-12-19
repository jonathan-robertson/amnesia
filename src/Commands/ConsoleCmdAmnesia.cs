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

        public override string[] GetCommands() => Commands;

        public override string GetDescription() => "Configure or adjust settings for the Amnesia mod.";

        public override string GetHelp() {
            var i = 1;
            var j = 1;
            return $@"Usage:
  {i++}. {GetCommands()[0]}
  {i++}. {GetCommands()[0]} list
  {i++}. {GetCommands()[0]} config <{string.Join(" / ", Config.FieldNames)}> <value>
  {i++}. {GetCommands()[0]} update <user id / player name / entity id> <remainingLives>
  {i++}. {GetCommands()[0]} test
Description Overview
{j++}. View current mod options
{j++}. List remaining lives for all players
{j++}. Configure a given option
{Config.FieldNamesAndDescriptions}
{j++}. Update a specific player's remaining lives
{j++}. Test your amnesia configurations by wiping YOUR OWN character";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo) {
            try {
                if (_params.Count == 0) {
                    SdtdConsole.Instance.Output(Config.AsString());
                    return;
                }
                switch (_params[0].ToLower()) {
                    case "test":
                        if (_params.Count == 2 && _params[1].EqualsCaseInsensitive("confirm")) {
                            if (_senderInfo.RemoteClientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(_senderInfo.RemoteClientInfo.entityId, out var playerToReset)) {
                                SdtdConsole.Instance.Output("RemoteClientInfo and/or player is null; if using telnet, you need to actually be inside the game instead.");
                                return;
                            }
                            PlayerHelper.ResetPlayer(playerToReset);
                            SdtdConsole.Instance.Output("Your player was reset. Some UI elements might not have updated; schematics on your toolbelt, for example, may need to be moved to another slot to indicate they now need to be learned.");
                        } else {
                            SdtdConsole.Instance.Output($"Running this command will allow you to test your amnesia configurations by wiping YOUR OWN character.\nThis ONLY TESTS non-experimental features and will not trigger a disconnection.\nIf you're sure you want to do this, run \"{GetCommands()[0]} test confirm\"");
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
                        var players = GameManager.Instance.World.Players.list;
                        for (var i = 0; i < players.Count; i++) {
                            SdtdConsole.Instance.Output($"{players[i].GetCVar(Values.RemainingLivesCVar)}/{players[i].GetCVar(Values.MaxLivesCVar)} remaining for {players[i].GetDebugName()} ({players[i].entityId})");
                        }
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
                _ = ApplyInt(_params[2], v => {
                    Config.SetMaxLives(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.WarnAtLifeName.EqualsCaseInsensitive(_params[1])) {
                _ = ApplyInt(_params[2], v => {
                    Config.SetWarnAtLife(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.EnablePositiveOutlookName.EqualsCaseInsensitive(_params[1])) {
                _ = ApplyBool(_params[2], v => {
                    Config.SetEnablePositiveOutlook(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.ProtectMemoryDuringBloodmoonName.EqualsCaseInsensitive(_params[1])) {
                _ = ApplyBool(_params[2], v => {
                    Config.SetProtectMemoryDuringBloodmoon(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.ForgetLevelsAndSkillsName.EqualsCaseInsensitive(_params[1])) {
                _ = ApplyBool(_params[2], v => {
                    Config.SetForgetLevelsAndSkills(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.ForgetBooksName.EqualsCaseInsensitive(_params[1])) {
                _ = ApplyBool(_params[2], v => {
                    Config.SetForgetBooks(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.ForgetSchematicsName.EqualsCaseInsensitive(_params[1])) {
                _ = ApplyBool(_params[2], v => {
                    Config.SetForgetSchematics(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.ForgetKDRName.EqualsCaseInsensitive(_params[1])) {
                _ = ApplyBool(_params[2], v => {
                    Config.SetForgetKDR(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.ForgetActiveQuestsName.EqualsCaseInsensitive(_params[1])) {
                _ = ApplyBool(_params[2], v => {
                    Config.SetForgetActiveQuests(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.ForgetIntroQuestsName.EqualsCaseInsensitive(_params[1])) {
                _ = ApplyBool(_params[2], v => {
                    Config.SetForgetIntroQuests(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }
            if (Config.ForgetInactiveQuestsName.EqualsCaseInsensitive(_params[1])) {
                _ = ApplyBool(_params[2], v => {
                    Config.SetForgetInactiveQuests(v);
                    SdtdConsole.Instance.Output($"Successfully updated to {v}");
                });
                return;
            }

            SdtdConsole.Instance.Output("Invald parameter provided");
        }

        private void HandleUpdate(List<string> _params) {
            if (!int.TryParse(_params[2], out var remainingLives)) {
                SdtdConsole.Instance.Output("Unable to parse value: must be of type int");
                return;
            }
            var clientInfo = ConsoleHelper.ParseParamIdOrName(_params[1], true, false);
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
