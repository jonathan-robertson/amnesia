using OnlyThreeChances.Data;
using OnlyThreeChances.Utilities;
using System;
using System.Collections.Generic;

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

                // TODO: possibly include logic here if RespawnType.Died

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

                // TODO: this logic might be better performed onRespawn (from death)

                var livesRemaining = killedPlayer.GetCVar(Values.RemainingLivesCVar);

                // cap lives to maximum(sanity check)
                if (livesRemaining > Config.MaxLives) {
                    // "shouldn't" have to do this since we auto-push changes as they're made and on login... but just in case:
                    killedPlayer.SetCVar(Values.MaxLivesCVar, Config.MaxLives);
                    livesRemaining = Config.MaxLives;
                }

                // Calculate and apply remaining lives
                if (livesRemaining > 0) {
                    killedPlayer.SetCVar(Values.RemainingLivesCVar, livesRemaining - 1);
                } else if (livesRemaining == 0) {
                    ResetPlayer(killedPlayer);
                    killedPlayer.SetCVar(Values.RemainingLivesCVar, Config.MaxLives);
                }
            } catch (Exception e) {
                log.Error("Failed to handle GameMessage event.", e);
            }
            return true;
        }

        /**
         * <summary></summary>
         * <remarks>Most of the following core logic was lifted from ActionResetPlayerData.PerformTargetAction</remarks>
         */
        private void ResetPlayer(EntityPlayer player) {
            var resetLevels = true; // TODO: could be config value

            if (resetLevels) {
                player.Progression.ResetProgression(true);
                player.Progression.Level = 1;
                player.Progression.ExpToNextLevel = player.Progression.GetExpForNextLevel();
                player.Progression.SkillPoints = 0;
                player.Progression.ExpDeficit = 0;
                List<Recipe> recipes = CraftingManager.GetRecipes();
                for (int i = 0; i < recipes.Count; i++) {
                    if (recipes[i].IsLearnable) {
                        player.Buffs.RemoveCustomVar(recipes[i].GetName());
                    }
                }

                // Inform client cycles of level adjustment for health/stamina/food/water max values
                player.SetCVar("$LastPlayerLevel", player.Progression.Level);

                // Flush xp tracking counters
                player.SetCVar("_xpFromLoot", player.Progression.Level);
                player.SetCVar("_xpFromHarvesting", player.Progression.Level);
                player.SetCVar("_xpFromKill", player.Progression.Level);
                player.SetCVar("$xpFromLootLast", player.Progression.Level);
                player.SetCVar("$xpFromHarvestingLast", player.Progression.Level);
                player.SetCVar("$xpFromKillLast", player.Progression.Level);

                // Set flags to trigger incorporation of new stats/values into update cycle
                player.Progression.bProgressionStatsChanged = true;
                player.bPlayerStatsChanged = true;
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerStats>().Setup(player), false, player.entityId);

                // TODO: give short buff to display Tooltip notice on client screen explaining that a reset just happened
            }

            /* TODO: Maybe this can be added later. Does not work as is written; probably needs to send some net packages to update the client
            var removeLandclaims = true; // TODO: could be a param
            var removeSleepingBag = true; // TODO: could be a param

            if (removeLandclaims) {
                PersistentPlayerData playerDataFromEntityID = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(player.entityId);
                if (playerDataFromEntityID.LPBlocks != null) {
                    playerDataFromEntityID.LPBlocks.Clear();
                }
                NavObjectManager.Instance.UnRegisterNavObjectByOwnerEntity(player, "land_claim");
                SdtdConsole.Instance.Output("removed land claims.");
            }
            if (removeSleepingBag) {
                PersistentPlayerData playerDataFromEntityID = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(player.entityId);
                player.SpawnPoints.Clear();
                playerDataFromEntityID.ClearBedroll();
                SdtdConsole.Instance.Output("removed sleeping bag and respawn target.");
            }
            */
        }
    }
}
