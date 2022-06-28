using OnlyThreeChances.Data;
using OnlyThreeChances.Utilities;

namespace OnlyThreeChances {
    internal class API : IModApi {
        private static readonly ModLog log = new ModLog(typeof(API));

        public void InitMod(Mod _modInstance) {
            if (Config.Load()) {
                ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawnedInWorld);
            } else {
                log.Error("Unable to load or recover from configuration issue; this mod will not activate.");
            }
        }

        /**
         * <summary>Handle player spawning into world.</summary>
         * <param name="clientInfo">The client currently spawning in.</param>
         * <param name="respawnType">The type of respawn.</param>
         * <param name="pos">The position this player is respawning to.</param>
         * <remarks>This mod supports being dropped into an existing game, thanks to how we handle this process.</remarks>
         */
        private void OnPlayerSpawnedInWorld(ClientInfo clientInfo, RespawnType respawnType, Vector3i pos) {
            if (respawnType != RespawnType.JoinMultiplayer && respawnType != RespawnType.EnterMultiplayer) {
                return;
            }
            if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player)) {
                return;
            }

            var playerMaxLives = player.GetCVar(Values.MaxLivesCVar);
            if (playerMaxLives == 0) { // initialize player
                player.SetCVar(Values.LivesRemainingCvar, Config.MaxLives);
            }
            if (playerMaxLives != Config.MaxLives) { // update maxLives
                player.SetCVar(Values.MaxLivesCVar, Config.MaxLives);
            }
        }
    }
}
