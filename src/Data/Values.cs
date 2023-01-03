using System.Collections.Generic;
using System.Linq;

namespace Amnesia.Data {
    internal class Values {

        // cvars
        public const string CVarLongTermMemoryLevel = "amnesiaLongTermMemoryLevel";
        public const string CVarPositiveOutlookMaxTime = "amnesiaPositiveOutlookMaxTime";
        public const string CVarPositiveOutlookRemTime = "amnesiaPositiveOutlookRemTime";

        // buffs
        public const string BuffPositiveOutlook = "buffAmnesiaPositiveOutlook";
        public const string BuffBloodmoonLifeProtection = "buffAmnesiaBloodmoonLifeProtection";
        public const string BuffPostBloodmoonLifeProtection = "buffAmnesiaPostBloodmoonLifeProtection";
        public const string BuffHardenedMemory = "buffAmnesiaHardenedMemory";
        public const string BuffFragileMemory = "buffAmnesiaFragileMemory";
        public const string BuffMemoryLoss = "buffAmnesiaMemoryLoss";
        public const string BuffNewbieCoat = "buffNewbieCoat";
        public const string BuffNearDeathTrauma = "buffNearDeathTrauma";

        // items
        public const string NameMemoryBoosters = "drugAmnesiaMemoryBoosters";

        // names
        public const string NameLongTermMemoryLevel = "LongTermMemoryLevel";
        public const string NamePositiveOutlookMaxTime = "PositiveOutlookMaxTime";
        public const string NamePositiveOutlookTimeOnFirstJoin = "PositiveOutlookTimeOnFirstJoin";
        public const string NamePositiveOutlookTimeOnMemoryLoss = "PositiveOutlookTimeOnMemoryLoss";
        public const string NamePositiveOutlookTimeOnKill = "PositiveOutlookTimeOnKill";
        public const string NameProtectMemoryDuringBloodmoon = "ProtectMemoryDuringBloodmoon";
        public const string NameProtectMemoryDuringPvp = "ProtectMemoryDuringPvp";
        public const string NameForgetLevelsAndSkills = "ForgetLevelsAndSkills";
        public const string NameForgetBooks = "ForgetBooks";
        public const string NameForgetSchematics = "ForgetSchematics";
        public const string NameForgetKdr = "ForgetKdr";
        public const string NameForgetActiveQuests = "ForgetActiveQuests";
        public const string NameForgetInactiveQuests = "ForgetInactiveQuests";
        public const string NameForgetIntroQuests = "ForgetIntroQuests";

        private const string DisconnectionWarning = "\n      - [!] SYSTEM WILL DISCONNECT PLAYER ON FINAL DEATH IF ENABLED!";
        private const string ExperimentalWarning = "\n      - [!] EXPERIMENTAL FEATURE - USE AT YOUR OWN RISK...";
        public static readonly Dictionary<string, string> SingleValueNamesAndDescriptionsDict = new Dictionary<string, string> {
            { NameLongTermMemoryLevel, "the level players will be reset to on memory loss and the level at which losing memory on death starts" },

            { NamePositiveOutlookMaxTime, "maximum length of time allowed for buff that boost xp growth" },
            { NamePositiveOutlookTimeOnFirstJoin, "length of time for buff that boosts xp growth at first-time server join" },
            { NamePositiveOutlookTimeOnMemoryLoss, "length of time for buff that boosts xp growth on memory loss" },

            { NameProtectMemoryDuringBloodmoon, "whether deaths during bloodmoon will harm memory" },
            { NameProtectMemoryDuringPvp, "whether to prevent harm to memory when defeated in pvp" },

            { NameForgetLevelsAndSkills, $"whether to forget levels, skills, and skill points on memory loss (note that players will reset back to the level configured in {NameLongTermMemoryLevel}" },
            { NameForgetBooks, "whether books should be forgotten on memory loss" },
            { NameForgetSchematics, "whether schematics should be forgotten on memory loss" },
            { NameForgetKdr, "whether players/zombies killed and times died should be forgotten on memory loss" },

            { NameForgetActiveQuests, $"whether ongoing quests should be forgotten on memory loss{DisconnectionWarning}{ExperimentalWarning}" },
            { NameForgetInactiveQuests, $"whether completed quests (AND TRADER TIER LEVELS) should be forgotten on memory loss{DisconnectionWarning}{ExperimentalWarning}" },
            { NameForgetIntroQuests, $"whether the intro quests should be forgotten/reset on memory loss{DisconnectionWarning}{ExperimentalWarning}" }
        };
        public static List<string> SingleValueFieldNames { get; private set; } = SingleValueNamesAndDescriptionsDict.Keys.ToList();
        public static string SingleValueFieldNamesAndDescriptions { get; private set; } = "    - " + string.Join("\n    - ", SingleValueNamesAndDescriptionsDict.Select(kvp => kvp.Key + ": " + kvp.Value));

        public static readonly Dictionary<string, string> KeyValueNamesAndDescriptionsDict = new Dictionary<string, string> {
            { NamePositiveOutlookTimeOnKill, "length of time awarded to all online players when any player defeats an entity of the given type (name is case sensitive)" },
        };
        public static string KeyValueFieldNamesAndDescriptions { get; private set; } = "    - " + string.Join("\n    - ", KeyValueNamesAndDescriptionsDict.Select(kvp => kvp.Key + ": " + kvp.Value));
    }
}
