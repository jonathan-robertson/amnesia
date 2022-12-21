using System.Collections.Generic;
using System.Linq;

namespace Amnesia.Data {
    internal class Values {
        public const string LongTermMemoryLevelCVar = "amnesiaLongTermMemoryLevel";
        public const string PositiveOutlookMaxTimeCVar = "amnesiaPositiveOutlookMaxTime";
        public const string PositiveOutlookRemTimeCVar = "amnesiaPositiveOutlookRemTime";

        public const string PositiveOutlookBuff = "buffAmnesiaPositiveOutlook";
        public const string BloodmoonLifeProtectionBuff = "buffAmnesiaBloodmoonLifeProtection";
        public const string PostBloodmoonLifeProtectionBuff = "buffAmnesiaPostBloodmoonLifeProtection";
        public const string HardenedMemoryBuff = "buffAmnesiaHardenedMemory";
        public const string NewbieCoatBuff = "buffNewbieCoat";
        public const string MemoryLossNotificationBuff = "buffAmnesiaMemoryLoss";

        public const string MemoryBoosterItemName = "drugAmnesiaMemoryBooster";

        public const string LongTermMemoryLevelName = "LongTermMemoryLevel";
        public const string PositiveOutlookMaxTimeName = "PositiveOutlookMaxTime";
        public const string PositiveOutlookTimeOnFirstJoinName = "PositiveOutlookTimeOnFirstJoin";
        public const string PositiveOutlookTimeOnMemoryLossName = "PositiveOutlookTimeOnMemoryLoss";
        public const string PositiveOutlookTimeOnKillName = "PositiveOutlookTimeOnKill";
        public const string ProtectMemoryDuringBloodmoonName = "ProtectMemoryDuringBloodmoon";
        public const string ProtectMemoryDuringPvpName = "ProtectMemoryDuringPvp";
        public const string ForgetLevelsAndSkillsName = "ForgetLevelsAndSkills";
        public const string ForgetBooksName = "ForgetBooks";
        public const string ForgetSchematicsName = "ForgetSchematics";
        public const string ForgetKdrName = "ForgetKdr";
        public const string ForgetActiveQuestsName = "ForgetActiveQuests";
        public const string ForgetInactiveQuestsName = "ForgetInactiveQuests";
        public const string ForgetIntroQuestsName = "ForgetIntroQuests";


        public const string DisconnectionWarning = "\n  - [!] SYSTEM WILL DISCONNECT PLAYER ON FINAL DEATH IF ENABLED!";
        public const string ExperimentalWarning = "\n  - [!] EXPERIMENTAL FEATURE - USE AT YOUR OWN RISK...";
        public static readonly Dictionary<string, string> FieldNamesAndDescriptionsDict = new Dictionary<string, string> {
            { LongTermMemoryLevelName, "the level players will be reset to on memory loss and the level at which losing memory on death starts" },

            { PositiveOutlookMaxTimeName, "maximum length of time allowed for buff that boost xp growth" },
            { PositiveOutlookTimeOnFirstJoinName, "length of time for buff that boosts xp growth at first-time server join" },
            { PositiveOutlookTimeOnMemoryLossName, "length of time for buff that boosts xp growth on memory loss" },
            { PositiveOutlookTimeOnKillName, "length of time awarded to all online players when any player defeats an entity of the given type (name is case sensitive)" },

            { ProtectMemoryDuringBloodmoonName, "whether deaths during bloodmoon will cost lives" },
            { ProtectMemoryDuringPvpName, "whether to prevent memory when defeated in pvp" },

            { ForgetLevelsAndSkillsName, $"whether to forget levels, skills, and skill points on memory loss (note that players will reset back to the level configured in {LongTermMemoryLevelName}" },
            { ForgetBooksName, "whether books should be forgotten on memory loss" },
            { ForgetSchematicsName, "whether schematics should be forgotten on memory loss" },
            { ForgetKdrName, "whether players/zombies killed and times died should be forgotten on memory loss" },

            { ForgetActiveQuestsName, $"whether ongoing quests should be forgotten on memory loss{DisconnectionWarning}{ExperimentalWarning}" },
            { ForgetInactiveQuestsName, $"whether completed quests (AND TRADER TIER LEVELS) should be forgotten on memory loss{DisconnectionWarning}{ExperimentalWarning}" },
            { ForgetIntroQuestsName, $"whether the intro quests should be forgotten/reset on memory loss{DisconnectionWarning}{ExperimentalWarning}" }
        };
        public static List<string> FieldNames { get; private set; } = FieldNamesAndDescriptionsDict.Keys.ToList();
        public static string FieldNamesAndDescriptions { get; private set; } = "    - " + string.Join("\n    - ", FieldNamesAndDescriptionsDict.Select(kvp => kvp.Key + ": " + kvp.Value));
    }
}
