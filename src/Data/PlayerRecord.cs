using Amnesia.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Amnesia.Data
{
    internal class PlayerRecord
    {
        #region constants
        private const string ROOT = "amnesiaPlayerRecord";
        private const string USER_IDENTIFIER = "userIdentifier";
        private const string ENTITY_ID = "entityId";
        private const string CHANGES = "changes";
        private const string CHANGE = "change";
        private const string NAME = "name";
        private const string LEVEL = "level";
        private const string FILENAME_EXTENSION = "apr";
        #endregion

        private static readonly ModLog<PlayerRecord> _log = new ModLog<PlayerRecord>();

        public static Dictionary<int, PlayerRecord> Entries { get; private set; } = new Dictionary<int, PlayerRecord>();

        public int EntityId { get; private set; }
        public PlatformUserIdentifierAbs UserIdentifier { get; private set; }
        public int Level { get; private set; } = 0;
        public List<(string, int)> Changes { get; private set; } = new List<(string, int)>();

        public static void Load(ClientInfo clientInfo)
        {
            var entityId = ClientInfoHelper.SafelyGetEntityIdFor(clientInfo);
            var userIdentifier = ClientInfoHelper.GetUserIdentifier(clientInfo);
            if (Entries.ContainsKey(entityId))
            {
                _log.Error($"Player Record is already loaded for player {entityId} / {userIdentifier.CombinedString} and this is NOT expected.");
                return;
            }
            if (!GameManager.Instance.World.Players.dict.TryGetValue(entityId, out var player))
            {
                _log.Error($"Could not find player at {entityId} / {userIdentifier.CombinedString} even though one is logging in with this info.");
                return;
            }
            var playerRecord = new PlayerRecord(entityId, userIdentifier, player.Progression.Level, player.Progression.SkillPoints);
            var filename = Path.Combine(GameIO.GetPlayerDataDir(), $"{userIdentifier}.apr");
            try
            {
                var xml = new XmlDocument();
                xml.Load(filename);
                var changes = xml.GetElementsByTagName(CHANGE);
                for (var i = 0; i < changes.Count; i++)
                {
                    var name = changes[i].Attributes[NAME].Value;
                    var level = int.Parse(changes[i].Attributes[LEVEL].Value);
                    playerRecord.Changes.Add((name, level));
                }
                _log.Info($"Successfully loaded {filename}");
            }
            catch (FileNotFoundException)
            {
                _log.Info($"No player record file found for player {entityId}; creating a new one with defaults under {filename}");
                playerRecord.Save();
            }
            catch (Exception e)
            {
                _log.Error($"Failed to load player record file {filename}; attempting to recover from backup.", e);

                // TODO: try to recover from backup

                // TODO: if backup recovery failed, store broken file under different filename for future reference
                var failureFilename = filename + ".failure";

                // otherwise, create default
                _log.Info($"Unable to recover player record for player {entityId}; creating a new one with defaults under {filename}; admin can attempt to inspect backup file {failureFilename}");
                playerRecord.Save();
            }
            finally
            {
                Entries.Add(entityId, playerRecord);
            }
        }

        public static void Unload(ClientInfo clientInfo)
        {
            var entityId = ClientInfoHelper.SafelyGetEntityIdFor(clientInfo);
            if (Entries.TryGetValue(entityId, out var playerRecord))
            {
                //playerRecord.Save(); // TODO: save as backup instead?
                playerRecord.Changes.Clear(); // proactively free memory
                _ = Entries.Remove(entityId);
            }
        }

        public PlayerRecord(int entityId, PlatformUserIdentifierAbs userIdentifier, int level, int unspentSkillPoints)
        {
            EntityId = entityId;
            UserIdentifier = userIdentifier;
            Level = level;
        }

        public void Save()
        {
            var filename = Path.Combine(GameIO.GetPlayerDataDir(), $"{UserIdentifier}.{FILENAME_EXTENSION}");
            try
            {
                var xml = new XmlDocument();
                var root = xml.AddXmlElement(ROOT);
                root.AddXmlElement(ENTITY_ID).InnerText = EntityId.ToString();
                root.AddXmlElement(USER_IDENTIFIER).InnerText = UserIdentifier.CombinedString;
                var changes = root.AddXmlElement(CHANGES);
                for (var i = 0; i < Changes.Count; i++)
                {
                    var change = changes.AddXmlElement(CHANGE);
                    change.SetAttribute(NAME, Changes[i].Item1);
                    change.SetAttribute(LEVEL, Changes[i].Item2.ToString());
                }
                xml.Save(filename);
                _log.Trace($"Successfully saved {filename}");
                // TODO: perhaps also save up to 1 backup?
            }
            catch (Exception e)
            {
                _log.Error($"Failed to save Player Record for {EntityId}", e);
            }
        }

        /// <summary>
        /// Set player level and automatically infer the change to unspent skill points.
        /// </summary>
        /// <param name="level">Level to set this record to.</param>
        public void UpdateLevel(int level)
        {
            if (Level != level)
            {
                _log.Trace($"Player {EntityId}'s level changed: {Level} -> {level}");
                Level = level;
            }
        }

        /// <summary>
        /// Set player level without inferring any change to unspent skill points.
        /// </summary>
        /// <param name="level">Level to set this record to.</param>
        public void SetLevel(int level)
        {
            if (Level != level)
            {
                _log.Trace($"Player {EntityId}'s level changed: {Level} -> {level}");
                Level = level;
            }
        }

        /// <summary>
        /// Record skill acquisition and incorporate cost into unspent skill points pool.
        /// </summary>
        /// <param name="name">name of skill</param>
        /// <param name="level">skill level acquired</param>
        /// <param name="cost">cost in skill points for this skill</param>
        public void PurchaseSkill(string name, int level, int cost)
        {
            _log.Trace($"Player {EntityId} purchased {name} at level {level} for {cost} skill {(cost == 1 ? "point" : "points")}");
            Changes.Add((name, level));
            Save();
        }

        /// <summary>
        /// Respec player, returning/unassigning all skill points but leaving level the same.
        /// </summary>
        /// <param name="player">Player to respec.</param>
        public void Respec(ClientInfo clientInfo, EntityPlayer player)
        {
            player.Progression.ResetProgression(true);
            Changes.Clear();
            Save();
            player.Progression.bProgressionStatsChanged = true;
            player.bPlayerStatsChanged = true;
            ConnectionManager.Instance.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerStats>().Setup(player), false, player.entityId);

        }

        /// <summary>
        /// Apply as many recorded skills in order as can be done; this is meant to be called after resetting player levels and skill points.
        /// </summary>
        /// <remarks>Logic lifted in part from XUiC_SkillPerkLevel.btnBuy_OnPress. <br />Should only call this method immediately after receiving a sync update for EntityPlayer's progression.</remarks>
        /// <param name="player">Reliable EntityPlayer that can be trusted</param>
        public void ReapplySkills(EntityPlayer player)
        {
            ValidateAndRepairChangeIntegrity(player);

            int i;
            for (i = 0; i < Changes.Count; i++)
            {
                var progressionValue = player.Progression.GetProgressionValue(Changes[i].Item1);
                var cost = progressionValue.ProgressionClass.CalculatedCostForLevel(Changes[i].Item2);
                if (cost > player.Progression.SkillPoints)
                {
                    break;
                }

                player.Progression.SkillPoints -= cost;
                progressionValue.Level = Changes[i].Item2;
            }

            Changes = Changes.GetRange(0, i); // forget remaining entries we couldn't afford
        }

        /// <summary>
        /// Attempt to repair any missing skills that may've been lost due to bugs/issues.
        /// </summary>
        /// <param name="dummy">entity for use in acquiring progression value only (TODO: possibly replace in the future)</param>
        public void ValidateAndRepairChangeIntegrity(EntityPlayer dummy)
        {
            var list = new List<(string, int)>();
            var dict = new Dictionary<string, int>();
            for (var i = 0; i < Changes.Count; i++)
            {
                var name = Changes[i].Item1;
                var level = Changes[i].Item2;

                if (!dict.ContainsKey(name))
                {
                    dict.Add(name, dummy.Progression.GetProgressionValue(Changes[i].Item1).ProgressionClass.MinLevel);
                }

                // fill any gaps that may've been missed
                while (dict[name] < level)
                {
                    dict[name]++;
                    list.Add((name, dict[name]));
                }
            }
            Changes = list;
            Save();
        }
    }
}
