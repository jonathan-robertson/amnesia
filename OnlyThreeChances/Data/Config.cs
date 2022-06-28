using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace OnlyThreeChances.Data {
    internal class Config {
        private static readonly string filename = Path.Combine(GameIO.GetSaveGameDir(), "only-three-chances-config.xml");

        public static int MaxLives { get; private set; } = 2;

        public static void SetLivesRemaining(EntityPlayer player, int livesRemaining) {
            if (player != null) {
                player.SetCVar(Values.LivesRemainingCvar, livesRemaining);
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
                Log.Out($"[OnlyThreeChances] Successfully saved {filename}");
                return true;
            } catch (Exception ex) {
                Log.Exception(ex);
                Log.Out($"[OnlyThreeChances] Failed to save {filename}");
                return false;
            }
        }

        public static bool Load() {
            try {
                XElement config = XElement.Load(filename);
                string maxLivesString = config.Descendants("maxLives").First().Value;
                if (!int.TryParse(maxLivesString, out var maxLives)) {
                    Log.Out("[OnlyThreeChances] Unable to parse maxLives element; expecting int");
                    return false;
                }
                MaxLives = maxLives;
                Log.Out($"[OnlyThreeChances] Successfully loaded {filename}");
                return true;
            } catch (FileNotFoundException) {
                Log.Out($"[OnlyThreeChances] No file detected, creating a config with defaults under {filename}");
                Save();
                return true;
            } catch (Exception ex) {
                Log.Error($"[OnlyThreeChances] Failed to load {filename}");
                Log.Exception(ex);
                return false;
            }
        }
    }
}
