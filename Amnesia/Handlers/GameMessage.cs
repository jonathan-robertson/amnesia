using Amnesia.Data;
using Amnesia.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amnesia.Handlers {
    internal class GameMessage {
        private static readonly ModLog log = new ModLog(typeof(GameMessage));

        public static bool Handle(ClientInfo clientInfo, EnumGameMessages messageType, string message, string mainName, bool localizeMain, string secondaryName, bool localizeSecondary) {
            try {
                switch (messageType) {
                    case EnumGameMessages.EntityWasKilled:
                        if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player)) {
                            log.Warn("EntityWasKilled event sent from a non-player client... may want to investigate");
                            return true; // exit early, do not interrupt other mods from processing event
                        }

                        /*
                         * TODO: add mechanic to handle final death differently for kill by zombie (or natural death) vs kill by player
                         * Perhaps "Total Bag/Equipment Deletion if not killed by player or Total Bag/Equipment drop if killed by player"
                         */

                        var livesRemaining = player.GetCVar(Values.RemainingLivesCVar);
                        log.Debug($"{player.GetDebugName()} livesRemaining: {livesRemaining}");

                        // cap lives to maximum(sanity check)
                        if (livesRemaining > Config.MaxLives) {
                            // "shouldn't" have to do this since we auto-push changes as they're made and on login... but just in case:
                            player.SetCVar(Values.MaxLivesCVar, Config.MaxLives);
                            livesRemaining = Config.MaxLives;
                        }

                        // TODO: update cvar, but... is this really necessary?
                        player.SetCVar(Values.WarnAtLifeCVar, Config.WarnAtLife);

                        // Calculate and apply remaining lives
                        if (livesRemaining > 0) {
                            log.Trace($"more lives remaining for {player.GetDebugName()}");
                            player.SetCVar(Values.RemainingLivesCVar, livesRemaining - 1);
                        } else if (livesRemaining == 0) {
                            log.Trace($"lives have expired for {player.GetDebugName()}");
                            ResetPlayer(player);
                            log.Trace($"returning lives to {Config.MaxLives} for {player.GetDebugName()}");
                            player.SetCVar(Values.RemainingLivesCVar, Config.MaxLives);
                            player.Buffs.AddBuff("buffAmnesiaMemoryLoss");
                            if (Config.EnablePositiveOutlook) {
                                log.Trace($"triggered: apply positive outlook for {player.GetDebugName()}");
                                player.Buffs.AddBuff("buffAmnesiaPositiveOutlook");
                            } else {
                                log.Trace($"skipped: apply positive outlook for {player.GetDebugName()}");
                            }
                            if ((Config.ResetQuests || Config.RemoveSharedQuests || Config.ResetFactionPoints) && !API.ResetAfterDisconnectMap.ContainsKey(player.entityId)) {
                                log.Trace($"triggered: queue reset of faction points for {player.GetDebugName()}");
                                API.ResetAfterDisconnectMap.Add(player.entityId, true);
                            } else {
                                log.Trace($"skipped: queue reset of faction points for {player.GetDebugName()}");
                            }
                        }
                        break;
                }
            } catch (Exception e) {
                log.Error("Failed to handle GameMessage event.", e);
            }
            return true; // do not interrupt other mods from processing event
        }

        /**
         * <summary></summary>
         * <remarks>Most of the following core logic was lifted from ActionResetPlayerData.PerformTargetAction</remarks>
         */
        public static void ResetPlayer(EntityPlayer player) { // TODO: make private

            // TODO: setting maxLives at 0 screws everything up; fix it?

            log.Info($"resetting {player.GetDebugName()}"); // TODO: remove
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

            /*
            if (resetQuests && !resetAfterDisconnectMap.ContainsKey(player.entityId)) {
                resetAfterDisconnectMap.Add(player.entityId, true);
                // TODO: KICKING DOES NOT TRIGGER OnPlayerDisconnected
                //GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default, factionResetKickReason));
                //clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerDisconnect>().Setup(player));
                ConnectionManager.Instance.DisconnectClient(clientInfo);
            }
            */
            /*
               GameManager.Instance.World.RemoveEntity(clientInfo.entityId, EnumRemoveEntityReason.Despawned);

               log.Debug($"{player.GetDebugName()} -> dead: {player.IsDead()}");

               var playerDataFile = new PlayerDataFile();
               playerDataFile.FromPlayer(player);
               playerDataFile.questJournal.QuestFactionPoints.Clear();



               SingletonMonoBehaviour<ConnectionManager>.Instance.SetClientEntityId(clientInfo, clientInfo.entityId, playerDataFile);
               // TODO: get/store chunkViewDim instead?
               clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerId>().Setup(player.entityId, player.TeamNumber, playerDataFile, 12));


               */
            /*
             * make sure bloaded = true before sending
            SingletonMonoBehaviour<ConnectionManager>.Instance.SetClientEntityId(_cInfo, num4, playerDataFile);
            _cInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerId>().Setup(num4, num2, playerDataFile, _chunkViewDim));
             */



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
