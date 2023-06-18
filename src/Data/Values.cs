using System.Collections.Generic;
using System.Linq;

namespace Amnesia.Data
{
    internal class Values
    {
        // cvars
        public static string CVarLongTermMemoryLevel { get; private set; } = "amnesiaLongTermMemoryLevel";
        public static string CVarPositiveOutlookMaxTime { get; private set; } = "amnesiaPositiveOutlookMaxTime";
        public static string CVarPositiveOutlookRemTime { get; private set; } = "amnesiaPositiveOutlookRemTime";
        public static string CVarTreatmentPrice { get; private set; } = "amnesiaTreatmentPrice";
        public static string CVarTherapyPrice { get; private set; } = "amnesiaTherapyPrice";

        // buffs
        public static string BuffPositiveOutlook { get; private set; } = "buffAmnesiaPositiveOutlook";
        public static string BuffBloodmoonLifeProtection { get; private set; } = "buffAmnesiaBloodmoonLifeProtection";
        public static string BuffPostBloodmoonLifeProtection { get; private set; } = "buffAmnesiaPostBloodmoonLifeProtection";
        public static string BuffFragileMemory { get; private set; } = "buffAmnesiaFragileMemory";
        public static string BuffMemoryLoss { get; private set; } = "buffAmnesiaMemoryLoss";
        public static string BuffNewbieCoat { get; private set; } = "buffNewbieCoat";
        public static string BuffNearDeathTrauma { get; private set; } = "buffNearDeathTrauma";
        public static string BuffTryBuyTreatment { get; private set; } = "buffAmnesiaTryBuyTreatment";
        public static string BuffRequestChangeCallback { get; private set; } = "buffAmnesiaRequestChangeCallback";
        public static string BuffTreatmentComplete { get; private set; } = "buffAmnesiaTreatmentComplete";
        public static string BuffTreatmentUnnecessary { get; private set; } = "buffAmnesiaTreatmentUnnecessary";
        public static string BuffTreatmentUnaffordable { get; private set; } = "buffAmnesiaTreatmentUnaffordable";

        // game_events
        public static string GameEventPayFromBag { get; private set; } = "amnesia_pay_from_bag";
        public static string GameEventPayFromBlt { get; private set; } = "amnesia_pay_from_blt";
        public static string GameEventRequestChg { get; private set; } = "amnesia_request_change";

        // names
        public static string NameLongTermMemoryLevel { get; private set; } = "LongTermMemoryLevel";
        public static string NamePositiveOutlookMaxTime { get; private set; } = "PositiveOutlookMaxTime";
        public static string NamePositiveOutlookTimeOnFirstJoin { get; private set; } = "PositiveOutlookTimeOnFirstJoin";
        public static string NamePositiveOutlookTimeOnMemoryLoss { get; private set; } = "PositiveOutlookTimeOnMemoryLoss";
        public static string NamePositiveOutlookTimeOnKill { get; private set; } = "PositiveOutlookTimeOnKill";
        public static string NameProtectMemoryDuringBloodmoon { get; private set; } = "ProtectMemoryDuringBloodmoon";
        public static string NameProtectMemoryDuringPvp { get; private set; } = "ProtectMemoryDuringPvp";
        public static string NameForgetLevelsAndSkills { get; private set; } = "ForgetLevelsAndSkills";
        public static string NameForgetBooks { get; private set; } = "ForgetBooks";
        public static string NameForgetSchematics { get; private set; } = "ForgetSchematics";
        public static string NameForgetKdr { get; private set; } = "ForgetKdr";
        public static string NameForgetNonIntroQuests { get; private set; } = "ForgetNonIntroQuests";

        public static Dictionary<string, string> SingleValueNamesAndDescriptionsDict { get; private set; } = new Dictionary<string, string> {
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

            { NameForgetNonIntroQuests, "[DISABLED FOR NOW] whether to forget non-intro quests (and trader tier levels) on memory loss" },
        };
        public static List<string> SingleValueFieldNames { get; private set; } = SingleValueNamesAndDescriptionsDict.Keys.ToList();
        public static string SingleValueFieldNamesAndDescriptions { get; private set; } = "    - " + string.Join("\n    - ", SingleValueNamesAndDescriptionsDict.Select(kvp => kvp.Key + ": " + kvp.Value));

        public static readonly Dictionary<string, string> KeyValueNamesAndDescriptionsDict = new Dictionary<string, string> {
            { NamePositiveOutlookTimeOnKill, "length of time awarded to all online players when any player defeats an entity of the given type (name is case sensitive)" },
        };
        public static string KeyValueFieldNamesAndDescriptions { get; private set; } = "    - " + string.Join("\n    - ", KeyValueNamesAndDescriptionsDict.Select(kvp => kvp.Key + ": " + kvp.Value));
    }
}
