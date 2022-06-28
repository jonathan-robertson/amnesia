using OnlyThreeChances.Data;
using OnlyThreeChances.Utilities;
using System;

namespace OnlyThreeChances {
    internal class API : IModApi {
        private static readonly ModLog log = new ModLog(typeof(API));

        public void InitMod(Mod _modInstance) {
            if (Config.Load()) {
                ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawnedInWorld);
                ModEvents.GameMessage.RegisterHandler(OnGameMessage);
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
            try {
                if (respawnType != RespawnType.JoinMultiplayer && respawnType != RespawnType.EnterMultiplayer) {
                    return;
                }
                if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player)) {
                    return;
                }

                var playerMaxLives = player.GetCVar(Values.MaxLivesCVar);
                if (playerMaxLives == 0) { // initialize player
                    // TODO: add buff for tracking/boosting player based on remaining lives
                    player.SetCVar(Values.RemainingLivesCVar, Config.MaxLives);
                }
                if (playerMaxLives != Config.MaxLives) { // update maxLives
                    player.SetCVar(Values.MaxLivesCVar, Config.MaxLives);
                }
            } catch (Exception e) {
                log.Error("Failed to handle PlayerSpawnedInWorld event.", e);
            }
        }

        private bool OnGameMessage(ClientInfo clientInfo, EnumGameMessages messageType, string message, string mainName, bool localizeMain, string secondaryName, bool localizeSecondary) {
            try {
                if (messageType != EnumGameMessages.EntityWasKilled) {
                    return true;
                }

                if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var killedPlayer)) {
                    log.Warn("Message sent from a non-player client... that's odd... someone hacking?");
                    return true;
                }

                // Note: if killed by another player, secondaryName will be populated with the name of a player

                var livesRemaining = killedPlayer.GetCVar(Values.RemainingLivesCVar);
                if (livesRemaining > Config.MaxLives) {
                    // "shouldn't" have to do this since we auto-push changes as they're made and on login... but just in case:
                    killedPlayer.SetCVar(Values.MaxLivesCVar, Config.MaxLives);
                    livesRemaining = Config.MaxLives;
                }

                if (livesRemaining > 0) {
                    killedPlayer.SetCVar(Values.RemainingLivesCVar, livesRemaining - 1);
                } else if (livesRemaining == 0) {
                    // TODO: wipe character
                }
            } catch (Exception e) {
                log.Error("Failed to handle GameMessage event.", e);
            }
            return true;
        }
    }
}
