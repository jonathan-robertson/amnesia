using Amnesia.Data;
using Amnesia.Handlers;
using Amnesia.Utilities;
using System.Collections.Generic;

namespace Amnesia {
    internal class API : IModApi {
        private static readonly ModLog log = new ModLog(typeof(API));

        public static Dictionary<int, bool> ResetAfterDisconnectMap { get; private set; } = new Dictionary<int, bool>();

        public void InitMod(Mod _modInstance) {
            if (Config.Load()) {
                ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld.Handle);
                ModEvents.GameMessage.RegisterHandler(GameMessage.Handle);
                //ModEvents.PlayerDisconnected.RegisterHandler(PlayerDisconnected.Handle);
                //ModEvents.SavePlayerData.RegisterHandler(SavePlayerData.Handle);
                //ModEvents.GameStartDone.RegisterHandler(GameStartDone.Handle);
            } else {
                log.Error("Unable to load or recover from configuration issue; this mod will not activate.");
            }
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
    }
}
