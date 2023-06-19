using Amnesia.Data;
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
                    TryBuyTreatment(__instance.parent);
                    return;
                }
                if (Values.BuffTryBuyTherapy.Equals(_name))
                {
                    TryBuyTherapy(__instance.parent);
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

        private static void TryBuyTreatment(EntityAlive entity)
        {
            // TODO: test local
            if (!entity.isEntityRemote)
            {
                var entityPlayerLocal = entity.world.GetPrimaryPlayer();
                entityPlayerLocal.Buffs.RemoveBuff(Values.BuffTryBuyTreatment);
                if (!entityPlayerLocal.Buffs.HasBuff(Values.BuffFragileMemory))
                {
                    _log.Trace($"player {entityPlayerLocal.GetDebugName()} requested Treatment from trader but doesn't have {Values.BuffFragileMemory}.");
                    _ = entityPlayerLocal.Buffs.AddBuff(Values.BuffTreatmentUnnecessary);
                }
                else if (DialogShop.TryPurchase(entityPlayerLocal, DialogShop.GetCost(entityPlayerLocal.Progression.Level, Product.Treatment)))
                {
                    _log.Trace($"player {entityPlayerLocal.GetDebugName()} purchased Treatment from trader.");
                    entityPlayerLocal.Buffs.RemoveBuff(Values.BuffFragileMemory);
                    _ = entityPlayerLocal.Buffs.AddBuff(Values.BuffTreatmentComplete);
                }
                else
                {
                    _log.Trace($"player {entityPlayerLocal.GetDebugName()} requested Treatment from trader but doesn't have enough money.");
                    _ = entityPlayerLocal.Buffs.AddBuff(Values.BuffTreatmentUnaffordable);
                }
                return;
            }

            // TODO: test remote
            if (PlayerHelper.TryGetClientInfoAndEntityPlayer(entity.world, entity.entityId, out var clientInfo, out var player))
            {
                player.Buffs.RemoveBuff(Values.BuffTryBuyTreatment);
                if (!player.Buffs.HasBuff(Values.BuffFragileMemory))
                {
                    _log.Trace($"player {player.GetDebugName()} requested Treatment from trader but doesn't have the necessary money {Values.BuffFragileMemory}.");
                    _ = player.Buffs.AddBuff(Values.BuffTreatmentUnnecessary);
                }
                else if (DialogShop.TryPurchase(clientInfo, player, DialogShop.GetCost(player.Progression.Level, Product.Treatment)))
                {
                    _log.Trace($"player {player.GetDebugName()} purchased Treatment from trader.");
                    player.Buffs.RemoveBuff(Values.BuffFragileMemory);
                    _ = player.Buffs.AddBuff(Values.BuffTreatmentComplete);
                }
                else
                {
                    _log.Trace($"player {player.GetDebugName()} requested Treatment from trader but doesn't have enough money.");
                    _ = player.Buffs.AddBuff(Values.BuffTreatmentUnaffordable);
                }
            }
        }

        private static void TryBuyTherapy(EntityAlive entity)
        {
            // TODO: support local as well

            if (PlayerHelper.TryGetClientInfoAndEntityPlayer(entity.world, entity.entityId, out var clientInfo, out var player))
            {
                player.Buffs.RemoveBuff(Values.BuffTryBuyTherapy);
                if (DialogShop.TryPurchase(clientInfo, player, DialogShop.GetCost(player.Progression.Level, Product.Therapy)))
                {
                    _log.Trace($"player {player.GetDebugName()} purchased Therapy from trader.");
                    PlayerHelper.Respec(player);
                    player.Buffs.AddBuff(Values.BuffTherapyComplete);
                }
                else
                {
                    _log.Trace($"player {player.GetDebugName()} requested Therapy from trader but doesn't have enough money.");
                    _ = player.Buffs.AddBuff(Values.BuffTreatmentUnaffordable);
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
