﻿using Amnesia.Data;
using Amnesia.Utilities;
using HarmonyLib;
using System;

namespace Amnesia.Patches
{
    /// <summary>
    /// Monitor added buffs.
    /// </summary>
    /// <remarks>Supports: local and remote</remarks>
    [HarmonyPatch(typeof(EntityBuffs), "AddBuff")]
    internal class EntityBuffs_AddBuff_Patches
    {
        private static readonly ModLog<EntityBuffs_AddBuff_Patches> _log = new ModLog<EntityBuffs_AddBuff_Patches>();

        public static void Postfix(EntityBuffs __instance, string _name)
        {
            try
            {
                if (Values.BuffTryBuyTreatment.Equals(_name))
                {
                    TryBuy(__instance.parent);
                    return;
                }
                if (Values.BuffRequestChangeCallback.Equals(_name))
                {
                    TryGiveChange(__instance.parent);
                    return;
                }
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }

        private static void TryBuy(EntityAlive entity)
        {
            if (!entity.isEntityRemote)
            {
                var entityPlayerLocal = entity.world.GetPrimaryPlayer();
                entityPlayerLocal.Buffs.RemoveBuff(Values.BuffTryBuyTreatment);
                if (!entityPlayerLocal.Buffs.HasBuff(Values.BuffFragileMemory))
                {
                    _log.Trace($"player {entityPlayerLocal.GetDebugName()} doesn't have {Values.BuffFragileMemory}");
                    _log.Debug("TODO: {tipTreatmentNotNecessary}: You have no need of Treatment since [00ff80]your memory is already healthy[-]. Please visit again if you acquire a [ff8000]Fragile Memory[-].");
                    // TODO: play sad/cancel sound
                }
                else if (DialogShop.TryPurchase(entityPlayerLocal, DialogShop.GetCost(entityPlayerLocal.Progression.Level, Product.Treatment)))
                {
                    _log.Trace("success");
                    entityPlayerLocal.Buffs.RemoveBuff(Values.BuffFragileMemory);
                    _log.Debug("TODO: {tipTreatmentComplete}: [00ff80]Your treatment is now complete[-] and your memory has returned to a health state. Please come again!");
                    // TODO: play trader sound ui_trader_purchase in head?
                }
                else
                {
                    _log.Trace("failure");
                    _log.Debug("TODO: {tipCannotAfford}: [ff8000]You do not have the necessary funds[-] for this procedure. Please return when you do.");
                    // TODO: play sad/cancel sound
                }
                return;
            }

            if (PlayerHelper.TryGetClientInfoAndEntityPlayer(entity.world, entity.entityId, out var clientInfo, out var player))
            {
                player.Buffs.RemoveBuff(Values.BuffTryBuyTreatment);
                if (!player.Buffs.HasBuff(Values.BuffFragileMemory))
                {
                    _log.Trace($"player {player.GetDebugName()} doesn't have {Values.BuffFragileMemory}");
                    _log.Debug("TODO: {tipTreatmentNotNecessary}: You have no need of Treatment since [00ff80]your memory is already healthy[-]. Please visit again if you acquire a [ff8000]Fragile Memory[-].");
                    // TODO: play sad/cancel sound
                }
                else if (DialogShop.TryPurchase(clientInfo, player, DialogShop.GetCost(player.Progression.Level, Product.Treatment)))
                {
                    _log.Trace("success");
                    player.Buffs.RemoveBuff(Values.BuffFragileMemory);
                    _log.Debug("TODO: {tipTreatmentComplete}: [00ff80]Your treatment is now complete[-] and your memory has returned to a health state. Please come again!");
                    // TODO: play trader sound ui_trader_purchase in head?
                }
                else
                {
                    _log.Trace("failure");
                    _log.Debug("TODO: {tipCannotAfford}: [ff8000]You do not have the necessary funds[-] for this procedure. Please return when you do.");
                    // TODO: play sad/cancel sound
                }
            }
        }

        private static void TryGiveChange(EntityAlive entity)
        {
            if (!entity.isEntityRemote)
            {
                var entityPlayerLocal = entity.world.GetPrimaryPlayer();
                entityPlayerLocal.Buffs.RemoveBuff(Values.BuffRequestChangeCallback);
                DialogShop.GiveChange(entityPlayerLocal);
                return;
            }

            if (PlayerHelper.TryGetClientInfoAndEntityPlayer(entity.world, entity.entityId, out var clientInfo, out var player))
            {
                player.Buffs.RemoveBuff(Values.BuffRequestChangeCallback);
                DialogShop.GiveChange(clientInfo, player);
            }
        }
    }

    /// <summary>
    /// Monitor setting of cvars.
    /// </summary>
    /// <remarks>Supports: local and remote</remarks>
    //[HarmonyPatch(typeof(EntityBuffs), "SetCustomVar")]
    // TODO: remove, so long as everything else continues to work
    internal class EntityBuffs_SetCustomVar_Patches
    {
        private static readonly ModLog<EntityBuffs_SetCustomVar_Patches> _log = new ModLog<EntityBuffs_SetCustomVar_Patches>();

        public static void Postfix(EntityBuffs __instance, string _name, float _value)
        {
            try
            {
                if (!Values.GameEventRequestChg.EqualsCaseInsensitive(_name)
                    || _value != 1
                    || !__instance.parent.world.Players.dict.TryGetValue(__instance.parent.entityId, out var player))
                {
                    return;
                }

                if (!__instance.parent.isEntityRemote)
                {
                    player.SetCVar(_name, 0); // unset value
                    DialogShop.GiveChange(player.world.GetPrimaryPlayer());
                    return;
                }

                var clientInfo = ConnectionManager.Instance.Clients.ForEntityId(__instance.parent.entityId);
                if (clientInfo == null)
                {
                    _log.Error($"Remote player {__instance.parent.GetDebugName()} ({__instance.parent.entityId}) did not seem to have an active connection; failing to return change.");
                    return;
                }

                player.SetCVar(_name, 0); // unset value
                DialogShop.GiveChange(clientInfo, player);
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
