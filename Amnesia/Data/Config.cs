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

        public static bool Loaded { get; private set; } = false;

        public static string MaxLivesName { get; private set; } = "MaxLives";
        public static string WarnAtLifeName { get; private set; } = "WarnAtLife";
        public static string EnablePositiveOutlookName { get; private set; } = "EnablePositiveOutlook";
        public static string ForgetLevelsAndSkillsName { get; private set; } = "ForgetLevelsAndSkills";
        public static string ForgetActiveQuestsName { get; private set; } = "ForgetActiveQuests";
        public static string ForgetInactiveQuestsName { get; private set; } = "ForgetInactiveQuests";
        public static string ForgetIntroQuestsName { get; private set; } = "ForgetIntroQuests";

        private static readonly string experimentalWarning = "\n        - [!] EXPERIMENTAL FEATURE - USE AT YOUR OWN RISK...";
        private static readonly string disconnectionWarning = "\n        - [!] SYSTEM WILL DISCONNECT PLAYER ON FINAL DEATH IF ENABLED!";
        private static readonly Dictionary<string, string> FieldNamesAndDescriptionsDict = new Dictionary<string, string> {
            { MaxLivesName, "how many lives players start with\n        - reducing this number will reduce remaining lives for all players only if remaining lives are below the new max\n        - increasing this number will also increase remaining lives for all players by the difference between the old max lives and new max lives"},
            { WarnAtLifeName, "number of lives remaining when system should start warning players about amnesia" },
            { EnablePositiveOutlookName, $"whether to grant temporary buff that boosts xp growth at initial server join and on memory loss" },
            { ForgetLevelsAndSkillsName, "whether to player levels and skills should be forgotten on memory loss" },
            { ForgetActiveQuestsName, $"whether ongoing quests should be forgotten on memory loss{disconnectionWarning}{experimentalWarning}" },
            { ForgetInactiveQuestsName, $"whether completed quests (AND TRADER TIER LEVELS) should be forgotten on memory loss{disconnectionWarning}{experimentalWarning}" },
            { ForgetIntroQuestsName, $"whether the intro quests should be forgotten/reset on memory loss{disconnectionWarning}{experimentalWarning}" }
        };
        public static List<string> FieldNames { get; private set; } = FieldNamesAndDescriptionsDict.Keys.ToList();
        public static string FieldNamesAndDescriptions { get; private set; } = "    - " + string.Join("\n    - ", FieldNamesAndDescriptionsDict.Select(kvp => kvp.Key + ": " + kvp.Value));

        public static int MaxLives { get; private set; } = 2;
        public static int WarnAtLife { get; private set; } = 1;
        public static bool EnablePositiveOutlook { get; private set; } = true;
        public static bool ForgetLevelsAndSkills { get; private set; } = true;

        public static bool ForgetActiveQuests { get; private set; } = false;
        public static bool ForgetInactiveQuests { get; private set; } = false;
        public static bool ForgetIntroQuests { get; private set; } = false;

        public static string AsString() {
            return $@"=== Amnesia Configuration ===
{MaxLivesName}: {MaxLives}
{WarnAtLifeName}: {WarnAtLife}
{EnablePositiveOutlookName}: {EnablePositiveOutlook}
{ForgetLevelsAndSkillsName}: {ForgetLevelsAndSkills}
{ForgetActiveQuestsName}: {ForgetActiveQuests}
{ForgetInactiveQuestsName}: {ForgetInactiveQuests}
{ForgetIntroQuestsName}: {ForgetIntroQuests}";
        }

        public static void AdjustToMaxOrRemainingLivesChange(EntityPlayer player) {
            // Initialize player and/or adjust max lives
            var maxLivesSnapshot = player.GetCVar(Values.MaxLivesCVar);
            var remainingLivesSnapshot = player.GetCVar(Values.RemainingLivesCVar);

            // update max lives if necessary
            if (maxLivesSnapshot != Config.MaxLives) {
                // increase so player has same count fewer than max before and after
                if (maxLivesSnapshot < Config.MaxLives) {
                    var increase = Config.MaxLives - maxLivesSnapshot;
                    player.SetCVar(Values.RemainingLivesCVar, remainingLivesSnapshot + increase);
                }
                player.SetCVar(Values.MaxLivesCVar, Config.MaxLives);
            }

            // cap remaining lives to max lives if necessary
            if (remainingLivesSnapshot > Config.MaxLives) {
                player.SetCVar(Values.RemainingLivesCVar, Config.MaxLives);
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
                    GameManager.Instance.World.Players.list.ForEach(p => p.Buffs.RemoveBuff("buffAmnesiaPositiveOutlook"));
                }
            }
        }

        /**
         * <summary>Enable or disable ResetLevels on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetResetLevels(bool value) {
            if (ForgetLevelsAndSkills != value) {
                ForgetLevelsAndSkills = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable ResetQuests on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetResetQuests(bool value) {
            if (ForgetActiveQuests != value) {
                ForgetActiveQuests = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable ClearIntroQuests on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetClearIntroQuests(bool value) {
            if (ForgetIntroQuests != value) {
                ForgetIntroQuests = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable the process to erase trader relationships on final death.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetResetFactionPoints(bool value) {
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
                    new XElement(ForgetLevelsAndSkillsName, ForgetLevelsAndSkills),
                    new XElement(ForgetActiveQuestsName, ForgetActiveQuests),
                    new XElement(ForgetIntroQuestsName, ForgetIntroQuests),
                    new XElement(ForgetInactiveQuestsName, ForgetInactiveQuests)
                ).Save(filename);
                log.Info($"Successfully saved {filename}");
                Loaded = true;
                return true;
            } catch (Exception e) {
                log.Error($"Failed to save {filename}", e);
                return false;
            }
        }

        public static void Load() {
            try {
                XElement config = XElement.Load(filename);
                MaxLives = ParseInt(config, MaxLivesName);
                WarnAtLife = ParseInt(config, WarnAtLifeName);
                EnablePositiveOutlook = ParseBool(config, EnablePositiveOutlookName);
                ForgetLevelsAndSkills = ParseBool(config, ForgetLevelsAndSkillsName);
                ForgetActiveQuests = ParseBool(config, ForgetActiveQuestsName);
                ForgetIntroQuests = ParseBool(config, ForgetIntroQuestsName);
                ForgetInactiveQuests = ParseBool(config, ForgetInactiveQuestsName);
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

        private static int ParseInt(XElement config, string name) {
            if (!int.TryParse(config.Descendants(name).First().Value, out var value)) {
                throw new Exception($"Unable to parse {name} element; expecting int");
            }
            return value;
        }

        private static bool ParseBool(XElement config, string name) {
            if (!bool.TryParse(config.Descendants(name).First().Value, out var value)) {
                throw new Exception($"Unable to parse {name} element; expecting bool");
            }
            return value;
        }
    }
}
