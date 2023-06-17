﻿using Amnesia.Data;
using Amnesia.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amnesia.Commands
{
    internal class ConsoleCmdAmnesia : ConsoleCmdAbstract
    {
        private static readonly string[] Commands = new string[]
        {
            "amnesia",
            "amn"
        };
        private readonly string help;

        public ConsoleCmdAmnesia()
        {
            var dict = new Dictionary<string, string>()
            {
                { "debug", "toggle debug logging mode" },
                { "players", "show players and their amnesia-related info" },
                { "grant <user id / player name / entity id> <timeInSeconds>", "grant player some bonus xp time" },
                { "fragile <user id / player name / entity id> <true/false>", "give or remove fragile memory debuff" },
                { "skills <user id / player name / entity id>", "show skill/perk records in order they were purchased by the given player" },
                { "config", "show current amnesia configuration" },
                { "set", "show the single-value fields you can adjust" },
                { "set <field>", "describe how you can update this field" },
                { "set <field> <valueToAdd>", "update a standard field with a new valueToAdd" },
                { "list", "show the complex config fields you can adjust" },
                { "list <complex-field>", "show contents of complex field" },
                { "list <complex-field> add <key> <name> <value>", "add or update a complex field" },
                { "list <complex-field> rem <key>", "add or update a complex field" },
                { "list <complex-field> clear", "add or update a complex field" },
                { "test", "admin command which triggers reset on self for testing purposes" },
                { "owed", "debugging command to check any pending change owed to a player" },
            };

            var i = 1; var j = 1;
            help = $"Usage:\n  {string.Join("\n  ", dict.Keys.Select(command => $"{i++}. {GetCommands()[0]} {command}").ToList())}\nDescription Overview\n{string.Join("\n", dict.Values.Select(description => $"{j++}. {description}").ToList())}";
        }

        protected override string[] getCommands()
        {
            return Commands;
        }

        protected override string getDescription()
        {
            return "Configure or adjust settings for the Amnesia mod.";
        }

        public override string GetHelp()
        {
            return help;
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            try
            {
                if (_params.Count == 0)
                {
                    SdtdConsole.Instance.Output($"A parameter is required; run 'help {Commands[0]}' for more info");
                    return;
                }
                switch (_params[0].ToLower())
                {
                    case "debug":
                        ModApi.DebugMode = !ModApi.DebugMode;
                        SdtdConsole.Instance.Output($"Debug Mode has successfully been {(ModApi.DebugMode ? "enabled" : "disabled")}.");
                        return;
                    case "players":
                        HandleShowPlayers();
                        return;
                    case "test":
                        HandleTest(_params, _senderInfo);
                        return;
                    case "grant":
                        if (_params.Count != 3)
                        {
                            break;
                        }
                        HandleGrant(_params);
                        return;
                    case "fragile":
                        if (_params.Count != 3)
                        {
                            break;
                        }
                        HandleFragile(_params);
                        return;
                    case "skills":
                        HandleSkills(_params);
                        return;
                    case "config":
                        SdtdConsole.Instance.Output(Config.AsString());
                        return;
                    case "list":
                        RouteListRequest(_params);
                        return;
                    case "set":
                        RouteSetRequest(_params);
                        return;
                    case "owed":
                        if (DialogShop.Change.Count == 0)
                        {
                            SdtdConsole.Instance.Output("[no change owed to any player]");
                            return;
                        }
                        foreach (var kvp in DialogShop.Change)
                        {
                            SdtdConsole.Instance.Output($"player {kvp.Key} is owed {kvp.Value}");
                        }
                        return;
                    default:
                        SdtdConsole.Instance.Output("Invald parameter provided");
                        return;
                }
                SdtdConsole.Instance.Output("Invald request");
            }
            catch (Exception e)
            {
                SdtdConsole.Instance.Output($"Exception encountered: \"{e.Message}\"\n{e.StackTrace}");
            }
        }

        private void HandleShowPlayers()
        {
            var players = GameManager.Instance.World.Players.list;
            if (players.Count == 0)
            {
                SdtdConsole.Instance.Output("No players are currently online.");
            }
            for (var i = 0; i < players.Count; i++)
            {
                SdtdConsole.Instance.Output($"{players[i].entityId,8} | {players[i].GetDebugName()} | level {players[i].Progression.Level} | {Values.BuffFragileMemory}: {players[i].Buffs.HasBuff(Values.BuffFragileMemory)} | {Values.CVarPositiveOutlookRemTime}: {players[i].GetCVar(Values.CVarPositiveOutlookRemTime)}");
            }
        }

        private void HandleTest(List<string> @params, CommandSenderInfo senderInfo)
        {
            if (senderInfo.RemoteClientInfo == null
                || !GameManager.Instance.World.Players.dict.TryGetValue(senderInfo.RemoteClientInfo.entityId, out var player)
                || !PlayerRecord.Entries.TryGetValue(player.entityId, out var record))
            {
                SdtdConsole.Instance.Output("RemoteClientInfo and/or player is null; if using telnet, you need to actually be inside the game instead.");
                return;
            }

            if (!int.TryParse(@params[1], out var levelsToRewind))
            {
                SdtdConsole.Instance.Output("ERROR: Expecting second parameter to be of type int.");
                return;
            }

            if (@params.Count < 3 || !@params[2].EqualsCaseInsensitive("confirm"))
            {
                SdtdConsole.Instance.Output($"Running this command will allow you to test your amnesia configurations by wiping YOUR OWN character.\nDue to the nature of how the game updates, it's recommended to only test this right after logging out/in.\nIf you're sure you want to do this, run \"{GetCommands()[0]} test {levelsToRewind} confirm\"");
                return;
            }

            PlayerHelper.Rewind(player, record, levelsToRewind);
            SdtdConsole.Instance.Output("Your player was reset. Some UI elements might not have updated; schematics on your toolbelt, for example, may need to be moved to another slot to indicate they now need to be learned.");
        }

        private void HandleGrant(List<string> @params)
        {
            if (!int.TryParse(@params[2], out var valueToAdd))
            {
                SdtdConsole.Instance.Output("Unable to parse valueToAdd: must be of type int");
                return;
            }
            var clientInfo = ConsoleHelper.ParseParamIdOrName(@params[1], true, false);
            if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player))
            {
                SdtdConsole.Instance.Output("Unable to find this player; note: player must be online");
                return;
            }
            var newValue = PlayerHelper.AddPositiveOutlookTime(player, valueToAdd);
            SdtdConsole.Instance.Output($"Added {valueToAdd} seconds of bonus xp time to {player.GetDebugName()} for a new value of {newValue}.");
        }

        private void HandleFragile(List<string> @params)
        {
            if (!bool.TryParse(@params[2], out var shouldAdd))
            {
                SdtdConsole.Instance.Output("Unable to parse valueToAdd: must be of type bool");
                return;
            }
            var clientInfo = ConsoleHelper.ParseParamIdOrName(@params[1], true, false);
            if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player))
            {
                SdtdConsole.Instance.Output("Unable to find this player; note: player must be online");
                return;
            }
            if (shouldAdd)
            {
                _ = player.Buffs.AddBuff(Values.BuffFragileMemory);
                SdtdConsole.Instance.Output($"Successfully added {Values.BuffFragileMemory} to {player.GetDebugName()}.");
                return;
            }
            player.Buffs.RemoveBuff(Values.BuffFragileMemory);
            SdtdConsole.Instance.Output($"Successfully removed {Values.BuffFragileMemory} from {player.GetDebugName()}.");
        }

        private void HandleSkills(List<string> @params)
        {
            var clientInfo = ConsoleHelper.ParseParamIdOrName(@params[1], true, false); // TODO: fix index out of range exception
            if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out _))
            {
                SdtdConsole.Instance.Output("Unable to find this player; note: player must be online");
                return;
            }
            if (!PlayerRecord.Entries.TryGetValue(clientInfo.entityId, out var record))
            {
                SdtdConsole.Instance.Output($"Unable to find an active record for player {clientInfo.entityId}");
                return;
            }
            for (var i = 0; i < record.Changes.Count; i++)
            {
                SdtdConsole.Instance.Output($"{i + 1,3}. {record.Changes[i].Item1}: {record.Changes[i].Item2}");
            }
            if (record.Changes.Count == 0)
            {
                SdtdConsole.Instance.Output("========= no recorded changes =========");
            }
            SdtdConsole.Instance.Output($"====================================\n{record.UnspentSkillPoints,3} skill point{(record.UnspentSkillPoints != 0 ? "s" : "")} currently unassigned.\n{record.Level,3} current level.");
        }

        private void RouteListRequest(List<string> @params)
        {
            if (@params.Count() == 1)
            {
                SdtdConsole.Instance.Output(Values.KeyValueFieldNamesAndDescriptions);
                return;
            }
            if (@params.Count() >= 3 &&
                !"add".EqualsCaseInsensitive(@params[2]) &&
                !"rem".EqualsCaseInsensitive(@params[2]) &&
                !"clear".EqualsCaseInsensitive(@params[2]))
            {
                SdtdConsole.Instance.Output($"Unable to parse command value; expecting 'add', 'rem', or 'clear'.\n{string.Join(", ", @params)}");
                return;
            }
            if (Values.NamePositiveOutlookTimeOnKill.EqualsCaseInsensitive(@params[1]))
            {
                UpdatePositiveOutlookOnKill(@params);
                return;
            }
        }

        private void UpdatePositiveOutlookOnKill(List<string> @params)
        {
            if (@params.Count == 2)
            {
                SdtdConsole.Instance.Output(Config.PrintPositiveOutlookTimeOnMemoryLoss());
                return;
            }
            switch (@params[2])
            {
                case "add":
                    if (@params.Count != 6) { break; }
                    if (!int.TryParse(@params[5], out var intValue))
                    {
                        SdtdConsole.Instance.Output($"Unable to parse value; expecting int");
                        break;
                    }
                    if (Config.AddPositiveOutlookTimeOnKill(@params[3], @params[4], intValue))
                    {
                        SdtdConsole.Instance.Output($"Successfully added {@params[3]} to the collection.");
                    }
                    else
                    {
                        SdtdConsole.Instance.Output($"Updating {@params[3]} was not necessary; it already exists in the collection just as entered.");
                    }
                    return;
                case "rem":
                    if (@params.Count != 4) { break; }
                    if (Config.RemPositiveOutlookTimeOnKill(@params[3]))
                    {
                        SdtdConsole.Instance.Output($"Successfully removed {@params[3]} from the collection.");
                    }
                    else
                    {
                        SdtdConsole.Instance.Output($"Removal of {@params[3]} was not necessary; due to either not being present or due to a typo.");
                    }
                    return;
                case "clear":
                    if (@params.Count != 3) { break; }
                    if (Config.ClearPositiveOutlookTimeOnKill())
                    {
                        SdtdConsole.Instance.Output($"Successfully cleared the collection.");
                    }
                    else
                    {
                        SdtdConsole.Instance.Output($"This collection was already clear.");
                    }
                    return;
            }
            SdtdConsole.Instance.Output($"Invald request; run '{Commands[0]} set {Values.NamePositiveOutlookTimeOnKill}' to see a list of options.");
            return;
        }

        private void RouteSetRequest(List<string> @params)
        {
            if (@params.Count() == 1)
            {
                SdtdConsole.Instance.Output(Values.SingleValueFieldNamesAndDescriptions);
                return;
            }
            if (Values.NameLongTermMemoryLevel.EqualsCaseInsensitive(@params[1]))
            {
                UpdateLongTermMemoryLevel(@params);
                return;
            }
            if (Values.NamePositiveOutlookMaxTime.EqualsCaseInsensitive(@params[1]))
            {
                UpdatePositiveOutlookMaxTime(@params);
                return;
            }
            if (Values.NamePositiveOutlookTimeOnFirstJoin.EqualsCaseInsensitive(@params[1]))
            {
                UpdatePositiveOutlookTimeOnFirstJoin(@params);
                return;
            }
            if (Values.NamePositiveOutlookTimeOnMemoryLoss.EqualsCaseInsensitive(@params[1]))
            {
                UpdatePositiveOutlookTimeOnMemoryLoss(@params);
                return;
            }
            if (Values.NameProtectMemoryDuringBloodmoon.EqualsCaseInsensitive(@params[1]))
            {
                UpdateProtectMemoryDuringBloodmoon(@params);
                return;
            }
            if (Values.NameProtectMemoryDuringPvp.EqualsCaseInsensitive(@params[1]))
            {
                UpdateProtectMemoryDuringPvp(@params);
                return;
            }
            if (Values.NameForgetLevelsAndSkills.EqualsCaseInsensitive(@params[1]))
            {
                UpdateForgetLevelsAndSkills(@params);
                return;
            }
            if (Values.NameForgetBooks.EqualsCaseInsensitive(@params[1]))
            {
                UpdateForgetBooks(@params);
                return;
            }
            if (Values.NameForgetSchematics.EqualsCaseInsensitive(@params[1]))
            {
                UpdateForgetSchematics(@params);
                return;
            }
            if (Values.NameForgetKdr.EqualsCaseInsensitive(@params[1]))
            {
                UpdateForgetKdr(@params);
                return;
            }
            if (Values.NameForgetShareableQuests.EqualsCaseInsensitive(@params[1]))
            {
                UpdateForgetShareableQuests(@params);
                return;
            }
            SdtdConsole.Instance.Output($"Invald request; run '{Commands[0]} set' to see a list of options.");
        }

        private void UpdateLongTermMemoryLevel(List<string> @params)
        {
            if (@params.Count == 2)
            {
                SdtdConsole.Instance.Output($@"{Values.SingleValueNamesAndDescriptionsDict[Values.NameLongTermMemoryLevel]}
{Commands[0]} set {Values.NameLongTermMemoryLevel} <level>");
                return;
            }
            if (!int.TryParse(@params[2], out var value))
            {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting int");
                return;
            }
            Config.SetLongTermMemoryLevel(value);
        }

        private void UpdatePositiveOutlookMaxTime(List<string> @params)
        {
            if (@params.Count == 2)
            {
                SdtdConsole.Instance.Output($@"{Values.SingleValueNamesAndDescriptionsDict[Values.NamePositiveOutlookMaxTime]}
{Commands[0]} set {Values.NamePositiveOutlookMaxTime} <timeInSeconds>");
                return;
            }
            if (!int.TryParse(@params[2], out var value))
            {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting int");
                return;
            }
            Config.SetPositiveOutlookMaxTime(value);
        }

        private void UpdatePositiveOutlookTimeOnFirstJoin(List<string> @params)
        {
            if (@params.Count == 2)
            {
                SdtdConsole.Instance.Output($@"{Values.SingleValueNamesAndDescriptionsDict[Values.NamePositiveOutlookTimeOnFirstJoin]}
{Commands[0]} set {Values.NamePositiveOutlookTimeOnFirstJoin} <timeInSeconds>");
                return;
            }
            if (!int.TryParse(@params[2], out var value))
            {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting int");
                return;
            }
            Config.SetPositiveOutlookTimeOnFirstJoin(value);
        }

        private void UpdatePositiveOutlookTimeOnMemoryLoss(List<string> @params)
        {
            if (@params.Count == 2)
            {
                SdtdConsole.Instance.Output($@"{Values.SingleValueNamesAndDescriptionsDict[Values.NamePositiveOutlookTimeOnMemoryLoss]}
{Commands[0]} set {Values.NamePositiveOutlookTimeOnMemoryLoss} <timeInSeconds>");
                return;
            }
            if (!int.TryParse(@params[2], out var value))
            {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting int");
                return;
            }
            Config.SetPositiveOutlookTimeOnMemoryLoss(value);
        }

        private void UpdateProtectMemoryDuringBloodmoon(List<string> @params)
        {
            if (@params.Count == 2)
            {
                SdtdConsole.Instance.Output($@"{Values.SingleValueNamesAndDescriptionsDict[Values.NameProtectMemoryDuringBloodmoon]}
{Commands[0]} set {Values.NameProtectMemoryDuringBloodmoon} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value))
            {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetProtectMemoryDuringBloodmoon(value);
        }

        private void UpdateProtectMemoryDuringPvp(List<string> @params)
        {
            if (@params.Count == 2)
            {
                SdtdConsole.Instance.Output($@"{Values.SingleValueNamesAndDescriptionsDict[Values.NameProtectMemoryDuringPvp]}
{Commands[0]} set {Values.NameProtectMemoryDuringPvp} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value))
            {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetProtectMemoryDuringPvp(value);
        }

        private void UpdateForgetLevelsAndSkills(List<string> @params)
        {
            if (@params.Count == 2)
            {
                SdtdConsole.Instance.Output($@"{Values.SingleValueNamesAndDescriptionsDict[Values.NameForgetLevelsAndSkills]}
{Commands[0]} set {Values.NameForgetLevelsAndSkills} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value))
            {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetForgetLevelsAndSkills(value);
        }

        private void UpdateForgetBooks(List<string> @params)
        {
            if (@params.Count == 2)
            {
                SdtdConsole.Instance.Output($@"{Values.SingleValueNamesAndDescriptionsDict[Values.NameForgetBooks]}
{Commands[0]} set {Values.NameForgetBooks} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value))
            {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetForgetBooks(value);
        }

        private void UpdateForgetSchematics(List<string> @params)
        {
            if (@params.Count == 2)
            {
                SdtdConsole.Instance.Output($@"{Values.SingleValueNamesAndDescriptionsDict[Values.NameForgetSchematics]}
{Commands[0]} set {Values.NameForgetSchematics} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value))
            {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetForgetSchematics(value);
        }

        private void UpdateForgetKdr(List<string> @params)
        {
            if (@params.Count == 2)
            {
                SdtdConsole.Instance.Output($@"{Values.SingleValueNamesAndDescriptionsDict[Values.NameForgetKdr]}
{Commands[0]} set {Values.NameForgetKdr} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value))
            {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetForgetKdr(value);
        }

        private void UpdateForgetShareableQuests(List<string> @params)
        {
            if (@params.Count == 2)
            {
                SdtdConsole.Instance.Output($@"{Values.SingleValueNamesAndDescriptionsDict[Values.NameForgetShareableQuests]}
{Commands[0]} set {Values.NameForgetShareableQuests} <true|false>");
                return;
            }
            if (!bool.TryParse(@params[2], out var value))
            {
                SdtdConsole.Instance.Output($"Unable to parse value; expecting bool");
                return;
            }
            Config.SetForgetShareableQuests(value);
        }
    }
}
