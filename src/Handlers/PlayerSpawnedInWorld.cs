using Amnesia.Data;
using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers
{
    internal class PlayerSpawnedInWorld
    {
        private static readonly ModLog<PlayerSpawnedInWorld> _log = new ModLog<PlayerSpawnedInWorld>();

        /// <summary>
        /// Handle player spawning into world.
        /// </summary>
        /// <param name="clientInfo">The client currently spawning in.</param>
        /// <param name="respawnType">The type of respawn.</param>
        /// <param name="pos">The position this player is respawning to.</param>
        /// <remarks>This mod supports being dropped into an existing game, thanks to how we handle this process.</remarks>
        public static void Handle(ClientInfo clientInfo, RespawnType respawnType, Vector3i pos)
        {
            if (!Config.Loaded) { return; }
            try
            {
                if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player) || !player.IsAlive())
                {
                    return; // exit early if player cannot be found in active world or is dead
                }
                switch (respawnType)
                {
                    // TODO: case RespawnType.NewGame: // local player creating a new game
                    case RespawnType.EnterMultiplayer: // first-time login for new player
                        _ = PlayerHelper.AddPositiveOutlookTime(player, Config.PositiveOutlookTimeOnFirstJoin);
                        HandleStandardRespawnSteps(player);
                        DialogShop.UpdatePrices(player);
                        DialogShop.UpdateMoneyTracker(player.entityId, player.inventory.GetSlots(), player.bag.GetSlots());
                        _ = PlayerRecord.TryLoad(clientInfo, out _, player);
                        break;
                    // TODO: case RespawnType.LoadedGame: // local player loading existing game
                    case RespawnType.JoinMultiplayer: // existing player rejoining
                        // grace period should continue only so long as you don't disconnect
                        player.Buffs.RemoveBuff(Values.BuffPostBloodmoonLifeProtection);
                        HandleStandardRespawnSteps(player);
                        DialogShop.UpdatePrices(player);
                        DialogShop.UpdateMoneyTracker(player.entityId, player.inventory.GetSlots(), player.bag.GetSlots());
                        if (PlayerRecord.TryLoad(clientInfo, out var record, player))
                        {
                            PlayerHelper.SkillPointIntegrityCheck(clientInfo, player, clientInfo.latestPlayerData, record);
                        }
                        break;
                    case RespawnType.Died: // existing player returned from death
                        _ = PlayerHelper.AddPositiveOutlookTime(player, Config.PositiveOutlookTimeOnMemoryLoss);
                        HandleStandardRespawnSteps(player);
                        break;
                }
            }
            catch (Exception e)
            {
                _log.Error("Failed to handle PlayerSpawnedInWorld event.", e);
            }
        }

        /// <summary>
        /// Process steps common to enter/join/death.
        /// </summary>
        /// <param name="player">Player to process steps for.</param>
        /// <remarks>This method also handles cleanup when player was already dead on Enter/Join (happens if player logged out while dead).</remarks>
        private static void HandleStandardRespawnSteps(EntityPlayer player)
        {

            // Ensure joining/respawning players have their constants updated
            if (player.GetCVar(Values.CVarLongTermMemoryLevel) != Config.LongTermMemoryLevel)
            {
                player.SetCVar(Values.CVarLongTermMemoryLevel, Config.LongTermMemoryLevel);
            }
            if (player.GetCVar(Values.CVarLevelPenalty) != Config.LevelPenalty)
            {
                player.SetCVar(Values.CVarLevelPenalty, Config.LevelPenalty);
            }

            // Remove Positive Outlook if admin disabled it since player's last login
            if (Config.PositiveOutlookTimeOnMemoryLoss == 0 && player.Buffs.HasBuff(Values.BuffPositiveOutlook))
            {
                player.Buffs.RemoveBuff(Values.BuffPositiveOutlook);
            }

            // Apply/Remove memory protection based on configuration
            if (Config.ProtectMemoryDuringBloodmoon)
            {
                // add or remove protection based on whether BM is active
                if (GameManager.Instance.World.aiDirector.BloodMoonComponent.BloodMoonActive)
                {
                    _ = player.Buffs.AddBuff(Values.BuffBloodmoonLifeProtection);
                }
                else
                {
                    player.Buffs.RemoveBuff(Values.BuffBloodmoonLifeProtection);
                }
            }
            else
            {
                // remove/clean up since protection is inactive
                player.Buffs.RemoveBuff(Values.BuffBloodmoonLifeProtection);
                player.Buffs.RemoveBuff(Values.BuffPostBloodmoonLifeProtection);
            }
        }
    }
}
