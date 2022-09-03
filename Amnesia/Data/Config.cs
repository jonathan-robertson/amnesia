using Amnesia.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Amnesia.Data {
    internal class Config {
        private static readonly ModLog log = new ModLog(typeof(Config));
        private static readonly string filename = Path.Combine(GameIO.GetSaveGameDir(), "amnesia.xml");

        public static string QuestResetKickReason { get; private set; } = "This server is configured to erase some settings from your player file when you die for the final time. Please reconnect whenever you're ready.";

        public static bool Loaded { get; private set; } = false;

        public static string MaxLivesName { get; private set; } = "MaxLives";
        public static string WarnAtLifeName { get; private set; } = "WarnAtLife";
        public static string EnablePositiveOutlookName { get; private set; } = "EnablePositiveOutlook";
        public static string ProtectMemoryDuringBloodmoonName { get; private set; } = "ProtectMemoryDuringBloodmoon";
        public static string ForgetLevelsAndSkillsName { get; private set; } = "ForgetLevelsAndSkills";
        public static string ForgetBooksName { get; private set; } = "ForgetBooks";
        public static string ForgetSchematicsName { get; private set; } = "ForgetSchematics";
        public static string ForgetKDRName { get; private set; } = "ForgetKDR";

        public static string ForgetActiveQuestsName { get; private set; } = "ForgetActiveQuests";
        public static string ForgetInactiveQuestsName { get; private set; } = "ForgetInactiveQuests";
        public static string ForgetIntroQuestsName { get; private set; } = "ForgetIntroQuests";

        private static readonly string experimentalWarning = "\n        - [!] EXPERIMENTAL FEATURE - USE AT YOUR OWN RISK...";
        private static readonly string disconnectionWarning = "\n        - [!] SYSTEM WILL DISCONNECT PLAYER ON FINAL DEATH IF ENABLED!";
        private static readonly Dictionary<string, string> FieldNamesAndDescriptionsDict = new Dictionary<string, string> {
            { MaxLivesName, "how many lives players start with\n        - reducing this number will reduce remaining lives for those players whose remaining lives would exceed the new max\n        - increasing this number will also increase remaining lives for all players by the difference between the old max lives and new max lives"},
            { WarnAtLifeName, "number of lives remaining when system should start warning players about amnesia" },
            { EnablePositiveOutlookName, $"whether to grant temporary buff that boosts xp growth at initial server join and on memory loss" },
            { ProtectMemoryDuringBloodmoonName, "whether deaths during bloodmoon will cost lives" },
            { ForgetLevelsAndSkillsName, "whether to forget levels, skills, and skill points on memory loss" },
            { ForgetBooksName, "whether books should be forgotten on memory loss" },
            { ForgetSchematicsName, "whether schematics should be forgotten on memory loss" },
            { ForgetKDRName, "whether players/zombies killed and times died should be forgotten on memory loss" },

            { ForgetActiveQuestsName, $"whether ongoing quests should be forgotten on memory loss{disconnectionWarning}{experimentalWarning}" },
            { ForgetInactiveQuestsName, $"whether completed quests (AND TRADER TIER LEVELS) should be forgotten on memory loss{disconnectionWarning}{experimentalWarning}" },
            { ForgetIntroQuestsName, $"whether the intro quests should be forgotten/reset on memory loss{disconnectionWarning}{experimentalWarning}" }
        };
        public static List<string> FieldNames { get; private set; } = FieldNamesAndDescriptionsDict.Keys.ToList();
        public static string FieldNamesAndDescriptions { get; private set; } = "    - " + string.Join("\n    - ", FieldNamesAndDescriptionsDict.Select(kvp => kvp.Key + ": " + kvp.Value));

        public static int MaxLives { get; private set; } = 2;
        public static int WarnAtLife { get; private set; } = 1;
        public static bool EnablePositiveOutlook { get; private set; } = true;
        public static bool ProtectMemoryDuringBloodmoon { get; private set; } = true;
        public static bool ForgetLevelsAndSkills { get; private set; } = true;
        public static bool ForgetBooks { get; private set; } = false;
        public static bool ForgetSchematics { get; private set; } = false;
        public static bool ForgetKDR { get; private set; } = false;

        public static bool ForgetActiveQuests { get; private set; } = false;
        public static bool ForgetInactiveQuests { get; private set; } = false;
        public static bool ForgetIntroQuests { get; private set; } = false;

        public static string AsString() {
            return $@"=== Amnesia Configuration ===
{MaxLivesName}: {MaxLives}
{WarnAtLifeName}: {WarnAtLife}
{EnablePositiveOutlookName}: {EnablePositiveOutlook}
{ProtectMemoryDuringBloodmoonName}: {ProtectMemoryDuringBloodmoon}
{ForgetLevelsAndSkillsName}: {ForgetLevelsAndSkills}
{ForgetBooksName}: {ForgetBooks}
{ForgetSchematicsName}: {ForgetSchematics}
{ForgetKDRName}: {ForgetKDR}

== Experimental Features (require player disconnection on final death) ==
{ForgetActiveQuestsName}: {ForgetActiveQuests}
{ForgetInactiveQuestsName}: {ForgetInactiveQuests}
{ForgetIntroQuestsName}: {ForgetIntroQuests}";
        }

        public static void AdjustToMaxOrRemainingLivesChange(EntityPlayer player) {
            // Initialize player and/or adjust max lives
            var maxLivesSnapshot = player.GetCVar(Values.MaxLivesCVar);
            var remainingLivesSnapshot = player.GetCVar(Values.RemainingLivesCVar);

            // update max lives if necessary
            if (maxLivesSnapshot != MaxLives) {
                // increase so player has same count fewer than max before and after
                if (maxLivesSnapshot < MaxLives) {
                    var increase = MaxLives - maxLivesSnapshot;
                    player.SetCVar(Values.RemainingLivesCVar, remainingLivesSnapshot + increase);
                }
                player.SetCVar(Values.MaxLivesCVar, MaxLives);
            }

            // cap remaining lives to max lives if necessary
            if (remainingLivesSnapshot > MaxLives) {
                player.SetCVar(Values.RemainingLivesCVar, MaxLives);
            }
        }

        /**
         * <summary>Adjust the remaining lives for a player.</summary>
         * <param name="player">The player to set remaining lives for.</param>
         * <param name="remainingLives">The remaining lives to set for this player.</param>
         */
        public static void SetRemainingLives(EntityPlayer player, int remainingLives) {
            if (player != null) {
                player.SetCVar(Values.RemainingLivesCVar, remainingLives);
                // TODO: is there a way to apply this to the player data without the player needing to be on?
                AdjustToMaxOrRemainingLivesChange(player);
            }
        }

        /**
         * <summary>Adjust the maximum number of lives a player has.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetMaxLives(int value) {
            if (MaxLives != value) {
                MaxLives = value;
                Save();
                GameManager.Instance.World.Players.list.ForEach(p => AdjustToMaxOrRemainingLivesChange(p));
            }
        }

        /**
         * <summary>Adjust the maximum number of lives a player has.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetWarnAtLife(int value) {
            if (WarnAtLife != value) {
                WarnAtLife = value;
                Save();
                GameManager.Instance.World.Players.list.ForEach(p => p.SetCVar(Values.WarnAtLifeCVar, WarnAtLife));
            }
        }

        /**
         * <summary>Enable or disable PositiveOutlook buff on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetEnablePositiveOutlook(bool value) {
            if (EnablePositiveOutlook != value) {
                EnablePositiveOutlook = value;
                Save();
                if (!EnablePositiveOutlook) {
                    GameManager.Instance.World.Players.list.ForEach(p => p.Buffs.RemoveBuff(Values.PositiveOutlookBuff));
                }
            }
        }

        /**
         * <summary>Enable or disable whether lives are lost during Blood Moon.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetProtectMemoryDuringBloodmoon(bool value) {
            if (ProtectMemoryDuringBloodmoon != value) {
                ProtectMemoryDuringBloodmoon = value;
                Save();
                if (ProtectMemoryDuringBloodmoon && GameManager.Instance.World.aiDirector.BloodMoonComponent.BloodMoonActive) {
                    GameManager.Instance.World.Players.list.ForEach(p => p.Buffs.AddBuff(Values.BloodmoonLifeProtectionBuff));
                } else {
                    GameManager.Instance.World.Players.list.ForEach(p => p.Buffs.RemoveBuff(Values.BloodmoonLifeProtectionBuff));
                    GameManager.Instance.World.Players.list.ForEach(p => p.Buffs.RemoveBuff(Values.PostBloodmoonLifeProtectionBuff));
                }
            }
        }

        /**
         * <summary>Enable or disable ForgetLevelsAndSkills on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetForgetLevelsAndSkills(bool value) {
            if (ForgetLevelsAndSkills != value) {
                ForgetLevelsAndSkills = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable ForgetBooks on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetForgetBooks(bool value) {
            if (ForgetBooks != value) {
                ForgetBooks = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable ForgetSchematics on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetForgetSchematics(bool value) {
            if (ForgetSchematics != value) {
                ForgetSchematics = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable ForgetKDR on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetForgetKDR(bool value) {
            if (ForgetKDR != value) {
                ForgetKDR = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable ForgetActiveQuests on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetForgetActiveQuests(bool value) {
            if (ForgetActiveQuests != value) {
                ForgetActiveQuests = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable ForgetIntroQuests on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetForgetIntroQuests(bool value) {
            if (ForgetIntroQuests != value) {
                ForgetIntroQuests = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable ForgetInactiveQuests on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetForgetInactiveQuests(bool value) {
            if (ForgetInactiveQuests != value) {
                ForgetInactiveQuests = value;
                Save();
            }
        }

        public static bool Save() {
            try {
                new XElement("config",
                    new XElement(MaxLivesName, MaxLives),
                    new XElement(WarnAtLifeName, WarnAtLife),
                    new XElement(EnablePositiveOutlookName, EnablePositiveOutlook),
                    new XElement(ProtectMemoryDuringBloodmoonName, ProtectMemoryDuringBloodmoon),
                    new XElement(ForgetLevelsAndSkillsName, ForgetLevelsAndSkills),
                    new XElement(ForgetBooksName, ForgetBooks),
                    new XElement(ForgetSchematicsName, ForgetSchematics),
                    new XElement(ForgetKDRName, ForgetKDR),

                    new XElement(ForgetActiveQuestsName, ForgetActiveQuests),
                    new XElement(ForgetInactiveQuestsName, ForgetInactiveQuests),
                    new XElement(ForgetIntroQuestsName, ForgetIntroQuests)
                ).Save(filename);
                log.Info($"Successfully saved {filename}");
                return true;
            } catch (Exception e) {
                log.Error($"Failed to save {filename}", e);
                return false;
            }
        }

        public static void Load() {
            try {
                XElement config = XElement.Load(filename);
                MaxLives = ParseInt(config, MaxLivesName, MaxLives);
                WarnAtLife = ParseInt(config, WarnAtLifeName, WarnAtLife);
                EnablePositiveOutlook = ParseBool(config, EnablePositiveOutlookName, EnablePositiveOutlook);
                ProtectMemoryDuringBloodmoon = ParseBool(config, ProtectMemoryDuringBloodmoonName, ProtectMemoryDuringBloodmoon);
                ForgetLevelsAndSkills = ParseBool(config, ForgetLevelsAndSkillsName, ForgetLevelsAndSkills);
                ForgetBooks = ParseBool(config, ForgetBooksName, ForgetBooks);
                ForgetSchematics = ParseBool(config, ForgetSchematicsName, ForgetSchematics);
                ForgetKDR = ParseBool(config, ForgetKDRName, ForgetKDR);

                ForgetActiveQuests = ParseBool(config, ForgetActiveQuestsName, ForgetActiveQuests);
                ForgetInactiveQuests = ParseBool(config, ForgetInactiveQuestsName, ForgetInactiveQuests);
                ForgetIntroQuests = ParseBool(config, ForgetIntroQuestsName, ForgetIntroQuests);
                log.Info($"Successfully loaded {filename}");
                Loaded = true;
            } catch (FileNotFoundException) {
                log.Info($"No file detected, creating a config with defaults under {filename}");
                Loaded = Save();
            } catch (Exception e) {
                log.Error($"Failed to load {filename}; Amnesia will remain disabled until server restart - fix file (possibly delete or try updating settings) then try restarting server.", e);
                Loaded = false;
            }
        }

        private static int ParseInt(XElement config, string name, int fallback) {
            try {
                if (int.TryParse(config.Descendants(name).First().Value, out var value)) {
                    return value;
                }
            } catch (Exception) { }
            log.Warn($"Unable to parse {name} from {filename}.\nFalling back to a default value of {fallback}.\nTry updating any setting to write the default option to this file.");
            return fallback;
        }

        private static bool ParseBool(XElement config, string name, bool fallback) {
            try {
                if (bool.TryParse(config.Descendants(name).First().Value, out var value)) {
                    return value;
                }
            } catch (Exception) { }
            log.Warn($"Unable to parse {name} from {filename}.\nFalling back to a default value of {fallback}.\nTry updating any setting to write the default option to this file.");
            return fallback;
        }
    }
}
