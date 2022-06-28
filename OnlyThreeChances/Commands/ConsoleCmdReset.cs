using System.Collections.Generic;

namespace OnlyThreeChances.Commands {
    internal class ConsoleCmdReset : ConsoleCmdAbstract {
        private static readonly string[] Commands = new string[] { "resetTest", "rt" };
        private const string Description = "Reset personal stats back to zero";
        private const string Help = "TODO";

        public override string[] GetCommands() {
            return Commands;
        }

        public override string GetDescription() {
            return Description;
        }

        public override string GetHelp() {
            return Help;
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo) {
            if (_senderInfo.RemoteClientInfo == null) {
                SdtdConsole.Instance.Output("Remote Client Info is missing.");
                return;
            }

            if (!GameManager.Instance.World.Players.dict.TryGetValue(_senderInfo.RemoteClientInfo.entityId, out var player) || player == null) {
                SdtdConsole.Instance.Output("Player not detected as being online.");
                return;
            }

            var resetLevels = true; // TODO: could be a param

            // The following was taken from ActionResetPlayerData.PerformTargetAction
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

                SdtdConsole.Instance.Output("player level, books, skills, xp, skill points, xp deficit, and recipes reset.");
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
