using Amnesia.Data;
using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers
{
    internal class PlayerSpawnedInWorld
    {
        private static readonly ModLog<PlayerSpawnedInWorld> log = new ModLog<PlayerSpawnedInWorld>();

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
                    case RespawnType.EnterMultiplayer: // first-time login for new player
                        _ = PlayerHelper.AddPositiveOutlookTime(player, Config.PositiveOutlookTimeOnFirstJoin);
                        RefundHardenedMemory(clientInfo, player); // TODO: deprecated; remove in 2.0.0
                        HandleStandardRespawnSteps(player);
                        break;
                    case RespawnType.JoinMultiplayer: // existing player rejoining
                        // grace period should continue only so long as you don't disconnect
                        player.Buffs.RemoveBuff(Values.BuffPostBloodmoonLifeProtection);
                        RefundHardenedMemory(clientInfo, player); // TODO: deprecated; remove in 2.0.0
                        HandleStandardRespawnSteps(player);
                        break;
                    case RespawnType.Died: // existing player returned from death
                        _ = PlayerHelper.AddPositiveOutlookTime(player, Config.PositiveOutlookTimeOnMemoryLoss);
                        HandleStandardRespawnSteps(player);
                        break;
                }
            }
            catch (Exception e)
            {
                log.Error("Failed to handle PlayerSpawnedInWorld event.", e);
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

        /// <summary>
        /// Temporary method to automatically refund any players with the Hardened Memory buff from version 1.0.0.
        /// </summary>
        /// <param name="player">EntityPlayer to check buffs for and refund if hardened.</param>
        private static void RefundHardenedMemory(ClientInfo clientInfo, EntityPlayer player) // TODO: deprecated; remove in 2.0.0
        {
            if (player.Buffs.HasBuff(Values.BuffHardenedMemory))
            {
                PlayerHelper.GiveItem(clientInfo, player, Values.NameMemoryBoosters);
                player.Buffs.RemoveBuff(Values.BuffHardenedMemory);
            }
        }
    }
}
