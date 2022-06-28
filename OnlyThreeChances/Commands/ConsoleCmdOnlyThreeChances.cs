using OnlyThreeChances.Data;
using System;
using System.Collections.Generic;

namespace OnlyThreeChances.Commands {
    internal class ConsoleCmdOnlyThreeChances : ConsoleCmdAbstract {
        private static readonly string[] Commands = new string[] {
            "onlyThreeChances",
            "otc"
        };

        private static readonly string[] Options = new string[] {
            "MaxLives"
        };

        public override string[] GetCommands() {
            return Commands;
        }

        public override string GetDescription() {
            return "Configure or adjust settings for the Only Three Chances mod.";
        }

        public override string GetHelp() {
            int i = 1;
            int j = 1;
            return $@"Usage:
  {i++}. {GetCommands()[0]}
  {i++}. {GetCommands()[0]} list
  {i++}. {GetCommands()[0]} config <{string.Join(" / ", Options)}> <value>
  {i++}. {GetCommands()[0]} update <user id / player name / entity id> <remainingLives>
Description Overview
{j++}. View current mod options
{j++}. List remaining lives for all players
{j++}. Configure a given option
{j++}. Update a specific player's remaining lives";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo) {
            try {
                if (_params.Count == 0) {
                    SdtdConsole.Instance.Output($"MaxLives: {Config.MaxLives}");
                    return;
                }
                switch (_params[0].ToLower()) {
                    case "list":
                        if (_params.Count != 1) {
                            break;
                        }
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
            switch (_params[1].ToLower()) {
                case "maxlives":
                    if (int.TryParse(_params[2], out int value)) {
                        Config.SetMaxLives(value);
                        SdtdConsole.Instance.Output($"Successfully updated; MaxLives set to {value}");
                    } else {
                        SdtdConsole.Instance.Output("Unable to parse value: must be of type int");
                    }
                    break;
                default:
                    SdtdConsole.Instance.Output("Invald parameter provided");
                    break;
            }
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
    }
}
