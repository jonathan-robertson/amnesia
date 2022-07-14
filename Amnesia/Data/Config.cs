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

        public static string MaxLivesName { get; private set; } = "MaxLives";
        public static string WarnAtLifeName { get; private set; } = "WarnAtLife";
        public static string EnablePositiveOutlookName { get; private set; } = "EnablePositiveOutlook";
        public static string ResetLevelsName { get; private set; } = "ResetLevels";
        public static string ResetQuestsName { get; private set; } = "ResetQuests";
        public static string ClearIntroQuestsName { get; private set; } = "ClearIntroQuests";
        public static string RemoveSharedQuestsName { get; private set; } = "RemoveSharedQuests";
        public static string ResetFactionPointsName { get; private set; } = "ResetFactionPoints";

        private static readonly string disconnectionWarning = "\n        - [!] SYSTEM WILL DISCONNECT PLAYER ON FINAL DEATH IF ENABLED!";
        private static readonly Dictionary<string, string> FieldNamesAndDescriptionsDict = new Dictionary<string, string> {
            { MaxLivesName, "how many lives players start with\n        - reducing this number will reduce remaining lives for all players only if remaining lives are below the new max\n        - increasing this number will also increase remaining lives for all players by the difference between the old max lives and new max lives"},
            { WarnAtLifeName, "when to start warning players about amnesia" },
            { EnablePositiveOutlookName, $"whether to grant temporary buff that boosts xp growth at initial join and on memory loss" },
            { ResetLevelsName, "whether to reset player levels on memory loss" },
            { ResetQuestsName, $"whether to remove player quests on memory loss{disconnectionWarning}" },
            { ClearIntroQuestsName, $"whether the intro quests should *also* be removed on memory loss\n        - [!] {ResetQuestsName} MUST BE ENABLED FOR THIS OPTION TO MATTER." },
            { RemoveSharedQuestsName, $"whether to remove all shared quests on memory loss (party members would need to re-share quests){disconnectionWarning}" },
            { ResetFactionPointsName, $"whether to erase relationship progression with trader on memory loss{disconnectionWarning}" }
        };
        public static List<string> FieldNames { get; private set; } = FieldNamesAndDescriptionsDict.Keys.ToList();
        public static string FieldNamesAndDescriptions { get; private set; } = "    - " + string.Join("\n    - ", FieldNamesAndDescriptionsDict.Select(kvp => kvp.Key + ": " + kvp.Value));

        public static int MaxLives { get; private set; } = 2;
        public static int WarnAtLife { get; private set; } = 1;
        public static bool EnablePositiveOutlook { get; private set; } = true;
        // TODO: add/support options in console for this
        public static bool ResetLevels { get; private set; } = true;

        // TODO: recommend against this + clearIntroQuests both being enabled if not also resetting levels
        // TODO: default to false
        // TODO: add/support options in console for this
        public static bool ResetQuests { get; private set; } = false;
        // TODO: add/support options in console for this
        public static bool ClearIntroQuests { get; private set; } = false;
        // TODO: add/support options in console for this
        public static bool RemoveSharedQuests { get; private set; } = false;
        public static bool ResetFactionPoints { get; private set; } = false;

        public static string AsString() {
            return $@"=== Amnesia Configuration ===
{MaxLivesName}: {MaxLives}
{WarnAtLifeName}: {WarnAtLife}
{EnablePositiveOutlookName}: {EnablePositiveOutlook}
{ResetLevelsName}: {ResetLevels}
{ResetQuestsName}: {ResetQuests}
{ClearIntroQuestsName}: {ClearIntroQuests}
{RemoveSharedQuestsName}: {RemoveSharedQuests}
{ResetFactionPointsName}: {ResetFactionPoints}";
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
                API.AdjustToMaxOrRemainingLivesChange(player);
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
                GameManager.Instance.World.Players.list.ForEach(p => API.AdjustToMaxOrRemainingLivesChange(p));
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
            if (ResetLevels != value) {
                ResetLevels = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable ResetQuests on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetResetQuests(bool value) {
            if (ResetQuests != value) {
                ResetQuests = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable ClearIntroQuests on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetClearIntroQuests(bool value) {
            if (ClearIntroQuests != value) {
                ClearIntroQuests = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable RemoveSharedQuests on memory loss.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetRemoveSharedQuests(bool value) {
            if (RemoveSharedQuests != value) {
                RemoveSharedQuests = value;
                Save();
            }
        }

        /**
         * <summary>Enable or disable the process to erase trader relationships on final death.</summary>
         * <param name="value">New value to use.</param>
         */
        public static void SetResetFactionPoints(bool value) {
            if (ResetFactionPoints != value) {
                ResetFactionPoints = value;
                Save();
            }
        }

        public static bool Save() {
            try {
                new XElement("config",
                    new XElement(MaxLivesName, MaxLives),
                    new XElement(WarnAtLifeName, WarnAtLife),
                    new XElement(EnablePositiveOutlookName, EnablePositiveOutlook),
                    new XElement(ResetLevelsName, ResetLevels),
                    new XElement(ResetQuestsName, ResetQuests),
                    new XElement(ClearIntroQuestsName, ClearIntroQuests),
                    new XElement(RemoveSharedQuestsName, RemoveSharedQuests),
                    new XElement(ResetFactionPointsName, ResetFactionPoints)
                ).Save(filename);
                log.Info($"Successfully saved {filename}");
                return true;
            } catch (Exception e) {
                log.Error($"Failed to save {filename}", e);
                return false;
            }
        }

        public static bool Load() {
            try {
                XElement config = XElement.Load(filename);
                MaxLives = ParseInt(config, MaxLivesName);
                WarnAtLife = ParseInt(config, WarnAtLifeName);
                EnablePositiveOutlook = ParseBool(config, EnablePositiveOutlookName);
                ResetLevels = ParseBool(config, ResetLevelsName);
                ResetQuests = ParseBool(config, ResetQuestsName);
                ClearIntroQuests = ParseBool(config, ClearIntroQuestsName);
                RemoveSharedQuests = ParseBool(config, RemoveSharedQuestsName);
                ResetFactionPoints = ParseBool(config, ResetFactionPointsName);
                log.Info($"Successfully loaded {filename}");
                return true;
            } catch (FileNotFoundException) {
                log.Info($"No file detected, creating a config with defaults under {filename}");
                Save();
                return true;
            } catch (Exception e) {
                log.Error($"Failed to load {filename}", e);
                return false;
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
