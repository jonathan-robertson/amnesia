using Amnesia.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Amnesia.Data
{
    internal class Config
    {
        private static readonly ModLog<Config> _log = new ModLog<Config>();
        private static readonly string filename = Path.Combine(GameIO.GetSaveGameDir(), "amnesia.xml");

        public static string QuestResetKickReason { get; private set; } = "This server is configured to erase some settings from your player file when you die and lose memory. Please reconnect whenever you're ready.";

        public static bool Loaded { get; private set; } = false;

        /// <summary>The level at which Amnesia mechanics activate and below which the player will never be reset.</summary>
        public static int LongTermMemoryLevel { get; private set; } = 1;
        /// <summary>The number of levels to lose on memory loss; if this is set to 0, memory loss will cause player to reset to LongTermMemoryLevel.</summary>
        public static int LevelPenalty { get; private set; } = 0;

        /// <summary>Base cost of Treatment.</summary>
        public static int TreatmentCostBase { get; private set; } = 0;
        /// <summary>Level Multplier cost of Treatment.</summary>
        public static int TreatmentCostMultiplier { get; private set; } = 1200;
        /// <summary>Base cost of Therapy.</summary>
        public static int TherapyCostBase { get; private set; } = 0;
        /// <summary>Level Multplier cost of Therapy.</summary>
        public static int TherapyCostMultiplier { get; private set; } = 600;

        /// <summary>Maximum length of time allowed for buff that boost xp growth.</summary>
        public static int PositiveOutlookMaxTime { get; private set; } = 3600; // 1 hr
        /// <summary>Length of time for buff that boosts xp growth at first-time server join.</summary>
        public static int PositiveOutlookTimeOnFirstJoin { get; private set; } = 3600; // 1 hr
        /// <summary>Length of time for buff that boosts xp growth on memory loss.</summary>
        public static int PositiveOutlookTimeOnMemoryLoss { get; private set; } = 3600; // 1 hr
        /// <summary>Length of time for server-wide xp boost buff when killing specific zombies.</summary>
        public static Dictionary<string, TimeOnKill> PositiveOutlookTimeOnKill { get; private set; } = new Dictionary<string, TimeOnKill>();
        public struct TimeOnKill
        {
            public string name;
            public string caption;
            public int value;

            public override string ToString()
            {
                return ToString("name", "caption", "value");
            }

            public string ToString(string nameDisplay, string captionDisplay, string valueDisplay, bool hideName = false)
            {
                return $"{{ {(hideName ? "" : nameDisplay + ": " + name + ", ")}{captionDisplay}: {caption}, {valueDisplay}: {value} }}";
            }
        };

        /// <summary>Whether to prevent memory loss during blood moon.</summary>
        public static bool ProtectMemoryDuringBloodmoon { get; private set; } = true;
        /// <summary>Whether to prevent memory when defeated in pvp.</summary>
        public static bool ProtectMemoryDuringPvp { get; private set; } = true;

        /// <summary>Whether to forget levels, skills, and skill points on memory loss.</summary>
        public static bool ForgetLevelsAndSkills { get; private set; } = true;
        /// <summary>Whether books should be forgotten on memory loss.</summary>
        public static bool ForgetBooks { get; private set; } = false;
        /// <summary>Whether crafting magazine progress should be forgotten on memory loss.</summary>
        public static bool ForgetCrafting { get; private set; } = false;
        /// <summary>Whether schematics should be forgotten on memory loss.</summary>
        public static bool ForgetSchematics { get; private set; } = false;
        /// <summary>Whether players/zombies killed and times died should be forgotten on memory loss.</summary>
        public static bool ForgetKdr { get; private set; } = false;
        /// <summary>Whether to forget shareable quests (and trader tier levels) on memory loss.</summary>
        public static bool ForgetNonIntroQuests { get; private set; } = false;

        public static string PrintPositiveOutlookTimeOnMemoryLoss()
        {
            return PositiveOutlookTimeOnKill.Count == 0 ? "None" : "[\n    " + string.Join(",\n    ", PositiveOutlookTimeOnKill.Select(kvp => kvp.Value.ToString("entityName", "displayName", "bonusTimeInSeconds")).ToArray()) + "\n]";
        }

        public static string AsString()
        {
            return $@"=== Amnesia Configuration ===
{Values.NameLongTermMemoryLevel}: {LongTermMemoryLevel}
{Values.NameLevelPenalty}: {LevelPenalty}

{Values.NamePositiveOutlookMaxTime}: {PositiveOutlookMaxTime}
{Values.NamePositiveOutlookTimeOnFirstJoin}: {PositiveOutlookTimeOnFirstJoin}
{Values.NamePositiveOutlookTimeOnMemoryLoss}: {PositiveOutlookTimeOnMemoryLoss}
{Values.NamePositiveOutlookTimeOnKill}: {PrintPositiveOutlookTimeOnMemoryLoss()}

{Values.NameProtectMemoryDuringBloodmoon}: {ProtectMemoryDuringBloodmoon}
{Values.NameProtectMemoryDuringPvp}: {ProtectMemoryDuringPvp}

{Values.NameTreatmentCostBase}: {TreatmentCostBase}
{Values.NameTreatmentCostMultiplier}: {TreatmentCostMultiplier}
{Values.NameTherapyCostBase}: {TherapyCostBase}
{Values.NameTherapyCostMultiplier}: {TherapyCostMultiplier}

{Values.NameForgetLevelsAndSkills}: {ForgetLevelsAndSkills}
{Values.NameForgetBooks}: {ForgetBooks}
{Values.NameForgetCrafting}: {ForgetCrafting}
{Values.NameForgetSchematics}: {ForgetSchematics}
{Values.NameForgetKdr}: {ForgetKdr}
{Values.NameForgetNonIntroQuests}: {ForgetNonIntroQuests}";
        }

        /// <summary>
        /// Update the long term memory level. This will determine when Amnesia activates and the level players will be reset to on death.
        /// </summary>
        /// <param key="value">The new level to use for long term memory.</param>
        public static bool SetLongTermMemoryLevel(int value)
        {
            if (LongTermMemoryLevel == value)
            {
                return false;
            }
            LongTermMemoryLevel = Math.Max(1, value);
            _ = Save();
            foreach (var player in GameManager.Instance.World.Players.list)
            {
                player.SetCVar(Values.CVarLongTermMemoryLevel, LongTermMemoryLevel);
                if (player.Progression.Level <= LongTermMemoryLevel)
                {
                    if (!player.Buffs.HasBuff(Values.BuffNewbieCoat))
                    {
                        _ = player.Buffs.AddBuff(Values.BuffNewbieCoat);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Update the level penalty. This will determine the number of lost levels experience when Memory is lost.
        /// </summary>
        /// <param key="value">The new level to use for Level Penalty.</param>
        public static bool SetLevelPenalty(int value)
        {
            if (LevelPenalty == value)
            {
                return false;
            }
            LevelPenalty = Math.Max(0, value);
            _ = Save();
            foreach (var player in GameManager.Instance.World.Players.list)
            {
                player.SetCVar(Values.CVarLevelPenalty, LevelPenalty);
            }
            return true;
        }

        /// <summary>
        /// Update the base cost of treatment.
        /// </summary>
        /// <param key="value">The value to use.</param>
        public static bool SetTreatmentCostBase(int value)
        {
            if (TreatmentCostBase == value)
            {
                return false;
            }
            TreatmentCostBase = Math.Max(0, value);
            _ = Save();
            return true;
        }

        /// <summary>
        /// Update the level multiplier cost of treatment.
        /// </summary>
        /// <param key="value">The value to use.</param>
        public static bool SetTreatmentCostMultiplier(int value)
        {
            if (TreatmentCostMultiplier == value)
            {
                return false;
            }
            TreatmentCostMultiplier = Math.Max(0, value);
            _ = Save();
            return true;
        }

        /// <summary>
        /// Update the base cost of therapy.
        /// </summary>
        /// <param key="value">The value to use.</param>
        public static bool SetTherapyCostBase(int value)
        {
            if (TherapyCostBase == value)
            {
                return false;
            }
            TherapyCostBase = Math.Max(0, value);
            _ = Save();
            return true;
        }

        /// <summary>
        /// Update the level multiplier cost of therapy.
        /// </summary>
        /// <param key="value">The value to use.</param>
        public static bool SetTherapyCostMultiplier(int value)
        {
            if (TherapyCostMultiplier == value)
            {
                return false;
            }
            TherapyCostMultiplier = Math.Max(0, value);
            _ = Save();
            return true;
        }

        /// <summary>
        /// Generously update max time for the positive outlook buff.
        /// </summary>
        /// <param key="timeInSeconds">The new value to limit max time to (generally represented as timeInSeconds).</param>
        public static bool SetPositiveOutlookMaxTime(int timeInSeconds)
        {
            if (PositiveOutlookMaxTime == timeInSeconds)
            {
                return false;
            }
            PositiveOutlookMaxTime = Math.Max(0, timeInSeconds);
            _ = Save();
            foreach (var player in GameManager.Instance.World.Players.list)
            {
                var playerRemTime = player.GetCVar(Values.CVarPositiveOutlookRemTime);
                if (playerRemTime > 0)
                {
                    var playerMaxTime = player.GetCVar(Values.CVarPositiveOutlookMaxTime);
                    if (playerMaxTime < PositiveOutlookMaxTime)
                    {
                        player.SetCVar(Values.CVarPositiveOutlookRemTime, PositiveOutlookMaxTime - playerMaxTime + playerRemTime);
                    }
                    else if (playerMaxTime > PositiveOutlookMaxTime)
                    {
                        player.SetCVar(Values.CVarPositiveOutlookRemTime, PositiveOutlookMaxTime);
                    }
                }
                player.SetCVar(Values.CVarPositiveOutlookMaxTime, PositiveOutlookMaxTime);
            }
            return true;
        }

        /// <summary>
        /// Update the Positive Outlook time granted when a player first joins the server.
        /// </summary>
        /// <param key="timeInSeconds">The number of seconds to grant.</param>
        /// <remarks>Set to <= zero to disable.</remarks>
        public static bool SetPositiveOutlookTimeOnFirstJoin(int timeInSeconds)
        {
            if (PositiveOutlookTimeOnFirstJoin == timeInSeconds)
            {
                return false;
            }
            PositiveOutlookTimeOnFirstJoin = Math.Min(PositiveOutlookMaxTime, Math.Max(0, timeInSeconds));
            _ = Save();
            return true;
        }

        /// <summary>
        /// Update the Positive Outlook time granted when a player loses memory.
        /// </summary>
        /// <param key="timeInSeconds">The number of seconds to grant.</param>
        /// <remarks>Set to <= zero to disable.</remarks>
        public static bool SetPositiveOutlookTimeOnMemoryLoss(int timeInSeconds)
        {
            if (PositiveOutlookTimeOnMemoryLoss == timeInSeconds)
            {
                return false;
            }
            PositiveOutlookTimeOnMemoryLoss = Math.Min(PositiveOutlookMaxTime, Math.Max(0, timeInSeconds));
            _ = Save();
            return true;
        }

        /// <summary>
        /// Add a zombie or animal key; killing this entity will provide extra time for Positive Outlook to everyone on the server.
        /// </summary>
        /// <param key="name">Lookup key and name id of the entity.</param>
        /// <param key="caption">The display name of the entity to trigger on.</param>
        /// <param key="timeInSeconds">Number of seconds to grant xp boost for.</param>
        public static bool AddPositiveOutlookTimeOnKill(string name, string caption, int timeInSeconds)
        {
            if (PositiveOutlookTimeOnKill.TryGetValue(name, out var entry))
            {
                if (entry.name.Equals(name) && entry.caption.Equals(caption) && entry.value == timeInSeconds)
                {
                    return false;
                }
                PositiveOutlookTimeOnKill[name] = new TimeOnKill
                {
                    name = name,
                    caption = caption,
                    value = Math.Min(PositiveOutlookMaxTime, Math.Max(1, timeInSeconds))
                };
            }
            else
            {
                PositiveOutlookTimeOnKill.Add(name, new TimeOnKill
                {
                    name = name,
                    caption = caption,
                    value = Math.Min(PositiveOutlookMaxTime, Math.Max(1, timeInSeconds))
                });
            }
            _ = Save();
            return true;
        }

        /// <summary>
        /// Remove a zombie or animal by key from the Time On Kill list.
        /// </summary>
        /// <param key="name">Name of the entity to remove the trigger for.</param>
        public static bool RemPositiveOutlookTimeOnKill(string name)
        {
            if (!PositiveOutlookTimeOnKill.ContainsKey(name))
            {
                return false;
            }
            _ = PositiveOutlookTimeOnKill.Remove(name);
            _ = Save();
            return true;
        }

        /// <summary>
        /// Clear all zombies or animals from the Time On Kill list.
        /// </summary>
        public static bool ClearPositiveOutlookTimeOnKill()
        {
            if (PositiveOutlookTimeOnKill.Count == 0)
            {
                return false;
            }
            PositiveOutlookTimeOnKill.Clear();
            _ = Save();
            return true;
        }

        /// <summary>
        /// Enable or disable whether memory can be lost during Blood Moon.
        /// </summary>
        /// <param key="value">New value to use.</param>
        public static bool SetProtectMemoryDuringBloodmoon(bool value)
        {
            if (ProtectMemoryDuringBloodmoon == value)
            {
                return false;
            }
            ProtectMemoryDuringBloodmoon = value;
            _ = Save();
            if (ProtectMemoryDuringBloodmoon && GameManager.Instance.World.aiDirector.BloodMoonComponent.BloodMoonActive)
            {
                foreach (var player in GameManager.Instance.World.Players.list)
                {
                    _ = player.Buffs.AddBuff(Values.BuffBloodmoonLifeProtection);
                }
            }
            else
            {
                foreach (var player in GameManager.Instance.World.Players.list)
                {
                    player.Buffs.RemoveBuff(Values.BuffBloodmoonLifeProtection);
                    player.Buffs.RemoveBuff(Values.BuffPostBloodmoonLifeProtection);
                }
            }
            return true;
        }

        /// <summary>
        /// Enable or disable whether memory can be lost to PVP.
        /// </summary>
        /// <param key="value">New value to use.</param>
        public static bool SetProtectMemoryDuringPvp(bool value)
        {
            if (ProtectMemoryDuringPvp == value)
            {
                return false;
            }
            ProtectMemoryDuringPvp = value;
            _ = Save();
            return true;
        }

        /// <summary>
        /// Enable or disable ForgetLevelsAndSkills on memory loss.
        /// </summary>
        /// <param key="value">New value to use.</param>
        public static bool SetForgetLevelsAndSkills(bool value)
        {
            if (ForgetLevelsAndSkills == value)
            {
                return false;
            }
            ForgetLevelsAndSkills = value;
            _ = Save();
            return true;
        }

        /// <summary>
        /// Enable or disable ForgetBooks on memory loss.
        /// </summary>
        /// <param key="value">New value to use.</param>
        public static bool SetForgetBooks(bool value)
        {
            if (ForgetBooks == value)
            {
                return false;
            }
            ForgetBooks = value;
            _ = Save();
            return true;
        }

        /// <summary>
        /// Enable or disable ForgetCrafting on memory loss.
        /// </summary>
        /// <param key="value">New value to use.</param>
        public static bool SetForgetCrafting(bool value)
        {
            if (ForgetCrafting == value)
            {
                return false;
            }
            ForgetCrafting = value;
            _ = Save();
            return true;
        }

        /// <summary>
        /// Enable or disable ForgetSchematics on memory loss.
        /// </summary>
        /// <param key="value">New value to use.</param>
        public static bool SetForgetSchematics(bool value)
        {
            if (ForgetSchematics == value)
            {
                return false;
            }
            ForgetSchematics = value;
            _ = Save();
            return true;
        }

        /// <summary>
        /// Enable or disable ForgetKdr on memory loss.
        /// </summary>
        /// <param key="value">New value to use.</param>
        public static bool SetForgetKdr(bool value)
        {
            if (ForgetKdr == value)
            {
                return false;
            }
            ForgetKdr = value;
            _ = Save();
            return true;
        }

        /// <summary>
        /// Enable or disable ForgetNonIntroQuests on memory loss.
        /// </summary>
        /// <param name="value">New value to use.</param>
        public static bool SetForgetNonIntroQuests(bool value)
        {
            if (ForgetNonIntroQuests == value)
            {
                return false;
            }
            ForgetNonIntroQuests = value;
            _ = Save();
            return true;
        }

        public static bool Save()
        {
            try
            {
                var timeOnKillElement = new XElement(Values.NamePositiveOutlookTimeOnKill);
                foreach (var kvp in PositiveOutlookTimeOnKill)
                {
                    timeOnKillElement.Add(new XElement("entry",
                        new XAttribute("name", kvp.Value.name),
                        new XAttribute("caption", kvp.Value.caption),
                        new XAttribute("value", kvp.Value.value)));
                }

                new XElement("config",
                    new XElement(Values.NameLongTermMemoryLevel, LongTermMemoryLevel),
                    new XElement(Values.NameLevelPenalty, LevelPenalty),

                    new XElement(Values.NamePositiveOutlookMaxTime, PositiveOutlookMaxTime),
                    new XElement(Values.NamePositiveOutlookTimeOnFirstJoin, PositiveOutlookTimeOnFirstJoin),
                    new XElement(Values.NamePositiveOutlookTimeOnMemoryLoss, PositiveOutlookTimeOnMemoryLoss),
                    timeOnKillElement,

                    new XElement(Values.NameProtectMemoryDuringBloodmoon, ProtectMemoryDuringBloodmoon),
                    new XElement(Values.NameProtectMemoryDuringPvp, ProtectMemoryDuringPvp),

                    new XElement(Values.NameTreatmentCostBase, TreatmentCostBase),
                    new XElement(Values.NameTreatmentCostMultiplier, TreatmentCostMultiplier),
                    new XElement(Values.NameTherapyCostBase, TherapyCostBase),
                    new XElement(Values.NameTherapyCostMultiplier, TherapyCostMultiplier),

                    new XElement(Values.NameForgetLevelsAndSkills, ForgetLevelsAndSkills),
                    new XElement(Values.NameForgetBooks, ForgetBooks),
                    new XElement(Values.NameForgetCrafting, ForgetCrafting),
                    new XElement(Values.NameForgetSchematics, ForgetSchematics),
                    new XElement(Values.NameForgetKdr, ForgetKdr),

                    new XElement(Values.NameForgetNonIntroQuests, ForgetNonIntroQuests)
                ).Save(filename);
                _log.Info($"Successfully saved {filename}");
                return true;
            }
            catch (Exception e)
            {
                _log.Error($"Failed to save {filename}", e);
                return false;
            }
        }

        public static void Load()
        {
            try
            {
                var config = XElement.Load(filename);
                LongTermMemoryLevel = ParseInt(config, Values.NameLongTermMemoryLevel, LongTermMemoryLevel);
                LevelPenalty = ParseInt(config, Values.NameLevelPenalty, LevelPenalty);

                PositiveOutlookMaxTime = ParseInt(config, Values.NamePositiveOutlookMaxTime, PositiveOutlookMaxTime);
                PositiveOutlookTimeOnFirstJoin = ParseInt(config, Values.NamePositiveOutlookTimeOnFirstJoin, PositiveOutlookTimeOnFirstJoin);
                PositiveOutlookTimeOnMemoryLoss = ParseInt(config, Values.NamePositiveOutlookTimeOnMemoryLoss, PositiveOutlookTimeOnMemoryLoss);

                PositiveOutlookTimeOnKill.Clear();
                foreach (var entry in config.Descendants(Values.NamePositiveOutlookTimeOnKill).First().Descendants("entry"))
                {
                    if (!int.TryParse(entry.Attribute("value").Value, out var intValue))
                    {
                        _log.Error($"Unable to parse value; expecting int");
                        return;
                    }
                    PositiveOutlookTimeOnKill.Add(entry.Attribute("name").Value, new TimeOnKill
                    {
                        name = entry.Attribute("name").Value,
                        caption = entry.Attribute("caption").Value,
                        value = intValue,
                    });
                }

                ProtectMemoryDuringBloodmoon = ParseBool(config, Values.NameProtectMemoryDuringBloodmoon, ProtectMemoryDuringBloodmoon);
                ProtectMemoryDuringPvp = ParseBool(config, Values.NameProtectMemoryDuringPvp, ProtectMemoryDuringPvp);

                TreatmentCostBase = ParseInt(config, Values.NameTreatmentCostBase, TreatmentCostBase);
                TreatmentCostMultiplier = ParseInt(config, Values.NameTreatmentCostMultiplier, TreatmentCostMultiplier);
                TherapyCostBase = ParseInt(config, Values.NameTherapyCostBase, TherapyCostBase);
                TherapyCostMultiplier = ParseInt(config, Values.NameTherapyCostMultiplier, TherapyCostMultiplier);

                ForgetLevelsAndSkills = ParseBool(config, Values.NameForgetLevelsAndSkills, ForgetLevelsAndSkills);
                ForgetBooks = ParseBool(config, Values.NameForgetBooks, ForgetBooks);
                ForgetCrafting = ParseBool(config, Values.NameForgetCrafting, ForgetCrafting);
                ForgetSchematics = ParseBool(config, Values.NameForgetSchematics, ForgetSchematics);
                ForgetKdr = ParseBool(config, Values.NameForgetKdr, ForgetKdr);

                ForgetNonIntroQuests = ParseBool(config, Values.NameForgetNonIntroQuests, ForgetNonIntroQuests);
                _log.Info($"Successfully loaded {filename}");
                Loaded = true;
            }
            catch (FileNotFoundException)
            {
                _log.Info($"No file detected, creating a config with defaults under {filename}");
                Loaded = Save();
            }
            catch (Exception e)
            {
                _log.Error($"Failed to load {filename}; Amnesia will remain disabled until server restart - fix file (possibly delete or try updating settings) then try restarting server.", e);
                Loaded = false;
            }
        }

        private static int ParseInt(XElement config, string name, int fallback)
        {
            try
            {
                if (int.TryParse(config.Descendants(name).First().Value, out var value))
                {
                    return value;
                }
            }
            catch (Exception) { }
            _log.Warn($"Unable to parse {name} from {filename}.\nFalling back to a default value of {fallback}.\nTry updating any setting to write the default option to this file.");
            return fallback;
        }

        private static bool ParseBool(XElement config, string name, bool fallback)
        {
            try
            {
                if (bool.TryParse(config.Descendants(name).First().Value, out var value))
                {
                    return value;
                }
            }
            catch (Exception) { }
            _log.Warn($"Unable to parse {name} from {filename}.\nFalling back to a default value of {fallback}.\nTry updating any setting to write the default option to this file.");
            return fallback;
        }
    }
}
