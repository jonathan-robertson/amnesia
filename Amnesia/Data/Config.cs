using Amnesia.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Amnesia.Data {
    internal class Config {
        private static readonly ModLog log = new ModLog(typeof(Config));
        private static readonly string filename = Path.Combine(GameIO.GetSaveGameDir(), "only-three-chances-config.xml");

        public static int MaxLives { get; private set; } = 2;

        public static void SetRemainingLives(EntityPlayer player, int remainingLives) {
            if (player != null) {
                player.SetCVar(Values.RemainingLivesCVar, remainingLives);
            }
        }

        public static void SetMaxLives(int maxLives) {
            if (MaxLives != maxLives) {
                MaxLives = maxLives;
                GameManager.Instance.World.Players.list.ForEach(p => p.SetCVar(Values.MaxLivesCVar, MaxLives));
            }
        }

        public static bool Save() {
            try {
                new XElement("config", new XElement("maxLives", MaxLives))
                    .Save(filename);
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
                string maxLivesString = config.Descendants("maxLives").First().Value;
                if (!int.TryParse(maxLivesString, out var maxLives)) {
                    log.Warn($"Unable to parse maxLives element; expecting int");
                    return false;
                }
                MaxLives = maxLives;
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
    }
}
