using Amnesia.Data;
using Amnesia.Utilities;
using System;
using System.Collections.Generic;
using static EntityBuffs;

namespace Amnesia {
    internal class API : IModApi {
        private static readonly ModLog log = new ModLog(typeof(API));
        private static readonly List<RespawnType> respawnTypesToMonitor = new List<RespawnType>{
            RespawnType.JoinMultiplayer,
            RespawnType.EnterMultiplayer
        };
        private static readonly List<EnumGameMessages> gameMessageTypesToMonitor = new List<EnumGameMessages>{
            EnumGameMessages.EntityWasKilled
        };

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
                if (!respawnTypesToMonitor.Contains(respawnType)) {
                    return; // exit early if we don't care about the presented respawnType
                }

                // Fetch player if possible
                if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out EntityPlayer player)) {
                    log.Warn("JoinMultiplayer/EnterMultiplayer event sent from a non-player client... may want to investigate");
                    return; // exit early if player cannot be found in active world
                }

                AdjustToMaxOrRemainingLivesChange(player);

                // Remove Positive Outlook if admin disabled it since player's last login
                if (!Config.EnablePositiveOutlook) {
                    player.Buffs.RemoveBuff("buffAmnesiaPositiveOutlook");
                }

                // Apply the appropriate buff to reflect the player's situation
                if (UpdateAmnesiaBuff(player) != BuffStatus.Added) {
                    log.Error($"Failed to add buff to player {player.GetDebugName()}");
                }
            } catch (Exception e) {
                log.Error("Failed to handle PlayerSpawnedInWorld event.", e);
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

        private bool OnGameMessage(ClientInfo clientInfo, EnumGameMessages messageType, string message, string mainName, bool localizeMain, string secondaryName, bool localizeSecondary) {
            try {
                if (!gameMessageTypesToMonitor.Contains(messageType)) {
                    return true; // exit early, do not interrupt other mods from processing event
                }

                if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player)) {
                    log.Warn("EntityWasKilled event sent from a non-player client... may want to investigate");
                    return true; // exit early, do not interrupt other mods from processing event
                }

                var livesRemaining = player.GetCVar(Values.RemainingLivesCVar);

                // cap lives to maximum(sanity check)
                if (livesRemaining > Config.MaxLives) {
                    // "shouldn't" have to do this since we auto-push changes as they're made and on login... but just in case:
                    player.SetCVar(Values.MaxLivesCVar, Config.MaxLives);
                    livesRemaining = Config.MaxLives;
                }

                // Calculate and apply remaining lives
                if (livesRemaining > 0) {
                    player.SetCVar(Values.RemainingLivesCVar, livesRemaining - 1);
                } else if (livesRemaining == 0) {
                    ResetPlayer(player);
                    player.SetCVar(Values.RemainingLivesCVar, Config.MaxLives);
                    player.Buffs.AddBuff("buffAmnesiaMemoryLoss");
                    if (Config.EnablePositiveOutlook) {
                        player.Buffs.AddBuff("buffAmnesiaPositiveOutlook");
                    }
                }

                if (UpdateAmnesiaBuff(player) != BuffStatus.Added) {
                    log.Error($"Failed to add buff to player {player.GetDebugName()}");
                }
            } catch (Exception e) {
                log.Error("Failed to handle GameMessage event.", e);
            }
            return true; // do not interrupt other mods from processing event
        }

        public static BuffStatus UpdateAmnesiaBuff(EntityPlayer player) {
            var remainingLives = player.GetCVar(Values.RemainingLivesCVar);
            if (remainingLives == 0) {
                return player.Buffs.AddBuff("buffAmnesiaMentallyUnhinged");
            } else if (remainingLives <= Config.WarnAtLife) {
                return player.Buffs.AddBuff("buffAmnesiaMentallyUneasy");
            } else {
                return player.Buffs.AddBuff("buffAmnesiaMentallyStable");
            }
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
