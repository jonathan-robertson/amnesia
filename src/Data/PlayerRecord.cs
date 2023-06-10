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
        public List<(string, int)> Changes { get; private set; } = new List<(string, int)>();

        public static void Load(ClientInfo clientInfo)
        {
            var entityId = ClientInfoHelper.SafelyGetEntityIdFor(clientInfo);
            var userIdentifier = ClientInfoHelper.GetUserIdentifier(clientInfo);
            if (Entries.ContainsKey(entityId))
            {
                _log.Warn($"Player Record is already loaded for player {entityId} / {userIdentifier.CombinedString} and this is NOT expected.");
                return;
            }
            var playerRecord = new PlayerRecord(entityId, userIdentifier);
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

        public PlayerRecord(int entityId, PlatformUserIdentifierAbs userIdentifier)
        {
            EntityId = entityId;
            UserIdentifier = userIdentifier;
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
                _log.Info($"Successfully saved {filename}");
                // TODO: perhaps also save up to 1 backup?
            }
            catch (Exception e)
            {
                _log.Error($"Failed to save Player Record for {EntityId}", e);
            }
        }
    }
}
