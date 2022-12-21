using Amnesia.Data;
using Amnesia.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using static WeatherManager;

namespace Amnesia.Commands {
    internal class ConsoleCmdAmnesia : ConsoleCmdAbstract {
        private static readonly string[] Commands = new string[] {
            "amnesia",
            "amn"
        };
        private readonly string help;

        public ConsoleCmdAmnesia() {
            var dict = new Dictionary<string, string>() {
                { "", "list players and their amnesia-related info" },
                { "grant <user id / player name / entity id> <timeInSeconds>", "grant player some bonus xp time" },
                { "config", "view current amnesia configuration" },
                { "set", "show a list of config fields" },
                { "set <field>", "describe how you can update this field" },
                { "set <field> <valueToAdd>", "update a standard field with a new valueToAdd" },
                { "set <field> <key> <valueToAdd>", "add or update a key-valueToAdd field" },
                { "test", "admin command which triggers reset on self for testing purposes" },
            };

            var i = 1; var j = 1;
            help = $"Usage:\n  {string.Join("\n  ", dict.Keys.Select(command => $"{i++}. {GetCommands()[0]} {command}").ToList())}\nDescription Overview\n{string.Join("\n", dict.Values.Select(description => $"{j++}. {description}").ToList())}";
        }

        public override string[] GetCommands() => Commands;

        public override string GetDescription() => "Configure or adjust settings for the Amnesia mod.";

        public override string GetHelp() => help;

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo) {
            try {
                if (_params.Count == 0) {
                    HandleListPlayers();
                    return;
                }
                switch (_params[0].ToLower()) {
                    case "test":
                        HandleTest(_params, _senderInfo);
                        return;
                    case "grant":
                        if (_params.Count != 3) {
                            break;
                        }
                        HandleGrant(_params);
                        return;
                    case "config":
                        SdtdConsole.Instance.Output(Config.AsString());
                        return;
                    case "set":
                        RouteSetRequest(_params);
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

        private void HandleListPlayers() {
            var players = GameManager.Instance.World.Players.list;
            if (players.Count == 0) {
                SdtdConsole.Instance.Output("No players are currently online.");
            }
            foreach (var player in players) {
                SdtdConsole.Instance.Output($"{player.entityId}. HardenedMemory: {player.Buffs.HasBuff("buffAmnesiaHardenedMemory")}, RemainingPositiveOutlookTime: {player.GetCVar(Values.PositiveOutlookRemTimeCVar)} ({player.GetDebugName()})");
            }
        }

        private void HandleTest(List<string> @params, CommandSenderInfo senderInfo) {
            if (@params.Count == 2 && @params[1].EqualsCaseInsensitive("confirm")) {
                if (senderInfo.RemoteClientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(senderInfo.RemoteClientInfo.entityId, out var playerToReset)) {
                    SdtdConsole.Instance.Output("RemoteClientInfo and/or player is null; if using telnet, you need to actually be inside the game instead.");
                    return;
                }
                PlayerHelper.ResetPlayer(playerToReset);
                SdtdConsole.Instance.Output("Your player was reset. Some UI elements might not have updated; schematics on your toolbelt, for example, may need to be moved to another slot to indicate they now need to be learned.");
            } else {
                SdtdConsole.Instance.Output($"Running this command will allow you to test your amnesia configurations by wiping YOUR OWN character.\nThis ONLY TESTS non-experimental features and will not trigger a disconnection.\nIf you're sure you want to do this, run \"{GetCommands()[0]} test confirm\"");
            }
        }

        private void HandleGrant(List<string> @params) {
            if (!int.TryParse(@params[2], out int valueToAdd)) {
                SdtdConsole.Instance.Output("Unable to parse valueToAdd: must be of type int");
                return;
            }
            var clientInfo = ConsoleHelper.ParseParamIdOrName(@params[1], true, false);
            if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player)) {
                SdtdConsole.Instance.Output("Unable to find this player; note: player must be online");
                return;
            }
            var newValue = PlayerHelper.AddPositiveOutlookTime(player, valueToAdd);
            SdtdConsole.Instance.Output($"Added {valueToAdd} seconds of bonus xp time to {player.GetDebugName()} for a new value of {newValue}.");
        }

        private void RouteSetRequest(List<string> @params) {
            if (@params.Count() == 1) {
                SdtdConsole.Instance.Output(Values.FieldNamesAndDescriptions);
                return;
            }
            if (Values.LongTermMemoryLevelName.EqualsCaseInsensitive(@params[1])) {
                UpdateLongTermMemoryLevel(@params);
                return;
            }
            if (Values.PositiveOutlookMaxTimeName.EqualsCaseInsensitive(@params[1])) {
                UpdatePositiveOutlookMaxTime(@params);
                return;
            }
            if (Values.PositiveOutlookTimeOnFirstJoinName.EqualsCaseInsensitive(@params[1])) {
                UpdatePositiveOutlookTimeOnFirstJoin(@params);
                return;
            }
            if (Values.PositiveOutlookTimeOnMemoryLossName.EqualsCaseInsensitive(@params[1])) {
                UpdatePositiveOutlookTimeOnMemoryLoss(@params);
                return;
            }
            if (Values.PositiveOutlookTimeOnKillName.EqualsCaseInsensitive(@params[1])) {
                UpdatePositiveOutlookOnKill(@params);
                return;
            }
            if (Values.ProtectMemoryDuringBloodmoonName.EqualsCaseInsensitive(@params[1])) {
                UpdateProtectMemoryDuringBloodmoon(@params);
                return;
            }
            if (Values.ProtectMemoryDuringPvpName.EqualsCaseInsensitive(@params[1])) {
                UpdateProtectMemoryDuringPvp(@params);
                return;
            }
            if (Values.ForgetLevelsAndSkillsName.EqualsCaseInsensitive(@params[1])) {
                UpdateForgetLevelsAndSkills(@params);
                return;
            }
            if (Values.ForgetBooksName.EqualsCaseInsensitive(@params[1])) {
                UpdateForgetBooks(@params);
                return;
            }
            if (Values.ForgetSchematicsName.EqualsCaseInsensitive(@params[1])) {
                UpdateForgetSchematics(@params);
                return;
            }
            if (Values.ForgetKdrName.EqualsCaseInsensitive(@params[1])) {
                UpdateForgetKdr(@params);
                return;
            }
            if (Values.ForgetActiveQuestsName.EqualsCaseInsensitive(@params[1])) {
                UpdateForgetActiveQuests(@params);
                return;
            }
            if (Values.ForgetInactiveQuestsName.EqualsCaseInsensitive(@params[1])) {
                UpdateForgetInactiveQuests(@params);
                return;
            }
            if (Values.ForgetIntroQuestsName.EqualsCaseInsensitive(@params[1])) {
                UpdateForgetIntroQuests(@params);
                return;
            }
            SdtdConsole.Instance.Output($"Invald request; run '{Commands[0]} set' to see a list of options.");
        }

        private void UpdateLongTermMemoryLevel(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.LongTermMemoryLevelName]}
{Commands[0]} set {Values.LongTermMemoryLevelName} <level>");
                return; 
            }
            if (!int.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetLongTermMemoryLevel(value);
            SdtdConsole.Instance.Output("done");
        }

        private void UpdatePositiveOutlookMaxTime(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.PositiveOutlookMaxTimeName]}
{Commands[0]} set {Values.PositiveOutlookMaxTimeName} <timeInSeconds>");
                return;
            }
            if (!int.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetPositiveOutlookMaxTime(value);
            SdtdConsole.Instance.Output("done");
        }

        private void UpdatePositiveOutlookTimeOnFirstJoin(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.PositiveOutlookTimeOnFirstJoinName]}
{Commands[0]} set {Values.PositiveOutlookTimeOnFirstJoinName} <timeInSeconds>");
                return;
            }
            if (!int.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetPositiveOutlookTimeOnFirstJoin(value);
            SdtdConsole.Instance.Output("done");
        }

        private void UpdatePositiveOutlookTimeOnMemoryLoss(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.PositiveOutlookTimeOnMemoryLossName]}
{Commands[0]} set {Values.PositiveOutlookTimeOnMemoryLossName} <timeInSeconds>");
                return;
            }
            if (!int.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetPositiveOutlookTimeOnMemoryLoss(value);
            SdtdConsole.Instance.Output("done");
        }

        private void UpdatePositiveOutlookOnKill(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.PositiveOutlookTimeOnKillName]}
{Commands[0]} set {Values.PositiveOutlookTimeOnKillName} list => show the currently configured entity/reward list
{Commands[0]} set {Values.PositiveOutlookTimeOnKillName} add <entityName> <rewardTimeInSeconds> => add an xp bonus for the given amount of time by killing the given entity
{Commands[0]} set {Values.PositiveOutlookTimeOnKillName} rem <entityName> => remove a named entry
{Commands[0]} set {Values.PositiveOutlookTimeOnKillName} clear => remove all entries");
                return;
            }

            var lowerParam = @params[2].ToLower();
            if (!lowerParam.Equals("list") &&
                !lowerParam.Equals("add") &&
                !lowerParam.Equals("rem") &&
                !lowerParam.Equals("clear")) {
                SdtdConsole.Instance.Output($"Unable to parse command value; expecting 'add', 'rem', or 'clear'.");
                return;
            }

            switch (@params[2]) {
                case "list":
                    if (@params.Count != 4) { break; }
                    SdtdConsole.Instance.Output(Config.PositiveOutlookTimeOnKill.Count == 0 ? "None set" : string.Join("\n", Config.PositiveOutlookTimeOnKill.ToArray().Select(kvp => "- " + kvp.Key + ": " + kvp.Value)));
                    return;
                case "add":
                    if (@params.Count != 5) { break; }
                    if (!int.TryParse(@params[4], out var intValue)) {
                        SdtdConsole.Instance.Output($"Unable to parse value; expecting int");
                        break;
                    }
                    Config.AddPositiveOutlookTimeOnKill(@params[3], intValue);
                    SdtdConsole.Instance.Output("done");
                    return;
                case "rem":
                    if (@params.Count != 4) { break; }
                    Config.RemPositiveOutlookTimeOnKill(@params[3]);
                    SdtdConsole.Instance.Output("done");
                    return;
                case "clear":
                    if (@params.Count != 3) { break; }
                    Config.ClearPositiveOutlookTimeOnKill();
                    SdtdConsole.Instance.Output("done");
                    return;
            }
            SdtdConsole.Instance.Output($"Invald request; run '{Commands[0]} set {Values.PositiveOutlookTimeOnKillName}' to see a list of options.");
            return;
        }

        private void UpdateProtectMemoryDuringBloodmoon(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.ProtectMemoryDuringBloodmoonName]}
{Commands[0]} set {Values.ProtectMemoryDuringBloodmoonName} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetProtectMemoryDuringBloodmoon(value);
            SdtdConsole.Instance.Output("done");
        }

        private void UpdateProtectMemoryDuringPvp(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.ProtectMemoryDuringPvpName]}
{Commands[0]} set {Values.ProtectMemoryDuringPvpName} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetProtectMemoryDuringPvp(value);
            SdtdConsole.Instance.Output("done");
        }

        private void UpdateForgetLevelsAndSkills(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.ForgetLevelsAndSkillsName]}
{Commands[0]} set {Values.ForgetLevelsAndSkillsName} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetForgetLevelsAndSkills(value);
            SdtdConsole.Instance.Output("done");
        }

        private void UpdateForgetBooks(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.ForgetBooksName]}
{Commands[0]} set {Values.ForgetBooksName} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetForgetBooks(value);
            SdtdConsole.Instance.Output("done");
        }

        private void UpdateForgetSchematics(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.ForgetSchematicsName]}
{Commands[0]} set {Values.ForgetSchematicsName} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetForgetSchematics(value);
            SdtdConsole.Instance.Output("done");
        }

        private void UpdateForgetKdr(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.ForgetKdrName]}
{Commands[0]} set {Values.ForgetKdrName} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetForgetKdr(value);
            SdtdConsole.Instance.Output("done");
        }

        private void UpdateForgetActiveQuests(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.ForgetActiveQuestsName]}
{Commands[0]} set {Values.ForgetActiveQuestsName} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetForgetActiveQuests(value);
            SdtdConsole.Instance.Output("done");
        }

        private void UpdateForgetInactiveQuests(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.ForgetInactiveQuestsName]}
{Commands[0]} set {Values.ForgetInactiveQuestsName} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetForgetInactiveQuests(value);
            SdtdConsole.Instance.Output("done");
        }

        private void UpdateForgetIntroQuests(List<string> @params) {
            if (@params.Count == 2) {
                SdtdConsole.Instance.Output($@"{Values.FieldNamesAndDescriptionsDict[Values.ForgetIntroQuestsName]}
{Commands[0]} set {Values.ForgetInactiveQuestsName} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value)) {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetForgetIntroQuests(value);
            SdtdConsole.Instance.Output("done");
        }
    }
}
