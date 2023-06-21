using Amnesia.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Amnesia.Utilities
{
    internal enum Product
    {
        Treatment, Therapy
    }

    internal class DialogShop
    {
        private static readonly ModLog<DialogShop> _log = new ModLog<DialogShop>();
        private static readonly ItemValue CASINO_COIN_ITEM_VALUE = ItemClass.GetItem("casinoCoin", false);
        private static readonly FastTags moneyTag = FastTags.Parse("amnesiaCurrency");

        public static Dictionary<int, int> BagMoney { get; private set; } = new Dictionary<int, int>();
        public static Dictionary<int, int> BltMoney { get; private set; } = new Dictionary<int, int>();
        public static Dictionary<int, int> Change { get; private set; } = new Dictionary<int, int>();

        public static void UpdateMoneyTracker(int entityId, ItemStack[] ___toolbelt = null, ItemStack[] ___bag = null)
        {
            _ = BltMoney.TryGetValue(entityId, out var blt);
            _ = BagMoney.TryGetValue(entityId, out var bag);

            var changed = false;
            if (___toolbelt != null)
            {
                var bltMoney = CountCoins(___toolbelt);
                changed = changed || bltMoney != blt;
                BltMoney[entityId] = bltMoney;
                blt = bltMoney;
            }
            if (___bag != null)
            {
                var bagMoney = CountCoins(___bag);
                changed = changed || bagMoney != bag;
                BagMoney[entityId] = bagMoney;
                bag = bagMoney;
            }
            if (changed)
            {
                _log.Debug($"blt: {blt}, bag: {bag}, total: {blt + bag}");
            }
        }

        public static void UpdatePrices(EntityPlayer player)
        {
            player.SetCVar(Values.CVarTreatmentPrice, GetCost(player.Progression.Level, Product.Treatment));
            player.SetCVar(Values.CVarTherapyPrice, GetCost(player.Progression.Level, Product.Therapy));
        }

        public static void TryBuyTreatment(EntityAlive entity)
        {
            // TODO: test local
            //if (!entity.isEntityRemote)
            //{
            //    var entityPlayerLocal = entity.world.GetPrimaryPlayer();
            //    entityPlayerLocal.Buffs.RemoveBuff(Values.BuffTryBuyTreatment);
            //    if (!entityPlayerLocal.Buffs.HasBuff(Values.BuffFragileMemory))
            //    {
            //        _log.Trace($"player {entityPlayerLocal.GetDebugName()} requested Treatment from trader but doesn't have {Values.BuffFragileMemory}.");
            //        _ = entityPlayerLocal.Buffs.AddBuff(Values.BuffTreatmentUnnecessary);
            //    }
            //    else if (DialogShop.TryPurchase(entityPlayerLocal, DialogShop.GetCost(entityPlayerLocal.Progression.Level, Product.Treatment)))
            //    {
            //        _log.Trace($"player {entityPlayerLocal.GetDebugName()} purchased Treatment from trader.");
            //        entityPlayerLocal.Buffs.RemoveBuff(Values.BuffFragileMemory);
            //        _ = entityPlayerLocal.Buffs.AddBuff(Values.BuffTreatmentComplete);
            //    }
            //    else
            //    {
            //        _log.Trace($"player {entityPlayerLocal.GetDebugName()} requested Treatment from trader but doesn't have enough money.");
            //        _ = entityPlayerLocal.Buffs.AddBuff(Values.BuffTreatmentUnaffordable);
            //    }
            //    return;
            //}

            if (PlayerHelper.TryGetClientInfoAndEntityPlayer(entity.world, entity.entityId, out var clientInfo, out var player))
            {
                player.Buffs.RemoveBuff(Values.BuffTryBuyTreatment);
                if (!player.Buffs.HasBuff(Values.BuffFragileMemory))
                {
                    _log.Trace($"player {player.GetDebugName()} requested Treatment from trader but doesn't have the necessary money {Values.BuffFragileMemory}.");
                    PlayerHelper.OpenWindow(clientInfo, Values.WindowShopTreatmentUnnecessary);
                    PlayerHelper.TriggerGameEvent(clientInfo, player, Values.GameEventShopUnnecessary);
                }
                else if (DialogShop.TryPurchase(clientInfo, player, DialogShop.GetCost(player.Progression.Level, Product.Treatment)))
                {
                    _log.Trace($"player {player.GetDebugName()} purchased Treatment from trader.");
                    player.Buffs.RemoveBuff(Values.BuffFragileMemory);
                    PlayerHelper.OpenWindow(clientInfo, Values.WindowShopTreatmentComplete);
                    PlayerHelper.TriggerGameEvent(clientInfo, player, Values.GameEventShopPurchased);
                }
                else
                {
                    _log.Trace($"player {player.GetDebugName()} requested Treatment from trader but doesn't have enough money.");
                    PlayerHelper.OpenWindow(clientInfo, Values.WindowShopCannotAfford);
                    PlayerHelper.TriggerGameEvent(clientInfo, player, Values.GameEventShopCannotAfford);
                }
            }
        }

        public static void TryBuyTherapy(EntityAlive entity)
        {
            // TODO: support local as well

            if (PlayerHelper.TryGetClientInfoAndEntityPlayer(entity.world, entity.entityId, out var clientInfo, out var player))
            {
                player.Buffs.RemoveBuff(Values.BuffTryBuyTherapy);
                if (DialogShop.TryPurchase(clientInfo, player, DialogShop.GetCost(player.Progression.Level, Product.Therapy)))
                {
                    _log.Trace($"player {player.GetDebugName()} purchased Therapy from trader.");
                    PlayerHelper.Respec(player);
                    PlayerHelper.OpenWindow(clientInfo, Values.WindowShopTherapyComplete);
                    PlayerHelper.TriggerGameEvent(clientInfo, player, Values.GameEventShopPurchased);
                }
                else
                {
                    _log.Trace($"player {player.GetDebugName()} requested Therapy from trader but doesn't have enough money.");
                    PlayerHelper.OpenWindow(clientInfo, Values.WindowShopCannotAfford);
                    PlayerHelper.TriggerGameEvent(clientInfo, player, Values.GameEventShopCannotAfford);
                }
            }
        }

        public static void TryGiveChange(EntityAlive entity)
        {
            if (!entity.isEntityRemote)
            {
                var entityPlayerLocal = entity.world.GetPrimaryPlayer();
                entityPlayerLocal.Buffs.RemoveBuff(Values.BuffRequestChangeCallback);
                GiveChange(entityPlayerLocal);
                return;
            }

            if (PlayerHelper.TryGetClientInfoAndEntityPlayer(entity.world, entity.entityId, out var clientInfo, out var player))
            {
                player.Buffs.RemoveBuff(Values.BuffRequestChangeCallback);
                GiveChange(clientInfo, player);
            }
        }

        private static int GetCost(int level, Product product)
        {
            switch (product)
            {
                case Product.Treatment:
                    return 1200 * level;
                case Product.Therapy:
                    return 600 * level;
            }
            return -1;
        }

        private static bool TryPurchase(ClientInfo clientInfo, EntityPlayer player, int cost)
        {
            if (CanAfford(player.entityId, cost))
            {
                _log.Trace($"player {player.GetDebugName()} could afford {cost}");
                Pay(clientInfo, player, cost);
                return true;
            }
            _log.Trace($"player {player.GetDebugName()} could NOT afford {cost}");
            return false;
        }

        private static void Pay(ClientInfo clientInfo, EntityPlayer player, int cost)
        {
            _log.Trace($"player {player.GetDebugName()} charge attempt for {cost}");
            if (BagMoney.TryGetValue(clientInfo.entityId, out var bag))
            {
                PlayerHelper.TriggerGameEvent(clientInfo, player, Values.GameEventPayFromBag);
                if (bag >= cost)
                {
                    if (bag > cost)
                    {
                        Change.Add(player.entityId, bag - cost);
                        PlayerHelper.TriggerGameEvent(clientInfo, player, Values.GameEventRequestChg);
                    }
                    return;
                }
                cost -= bag;
            }
            if (BltMoney.TryGetValue(clientInfo.entityId, out var blt))
            {
                PlayerHelper.TriggerGameEvent(clientInfo, player, Values.GameEventPayFromBlt);
                if (blt > cost)
                {
                    Change.Add(player.entityId, blt - cost);
                    PlayerHelper.TriggerGameEvent(clientInfo, player, Values.GameEventRequestChg);
                }
            }
        }

        private static bool CanAfford(int entityId, int amount)
        {
            _ = BagMoney.TryGetValue(entityId, out var bag);
            _ = BltMoney.TryGetValue(entityId, out var blt);
            return bag + blt - amount >= 0;
        }

        private static void GiveChange(ClientInfo clientInfo, EntityPlayer player)
        {
            if (!Change.TryGetValue(clientInfo.entityId, out var value))
            {
                _log.Error($"Client requested change for {clientInfo.entityId}, but there is no change cached to return; this is unexpected");
                return;
            }
            _ = Change.Remove(player.entityId);
            AddCoins(clientInfo, player.position, value);
            _log.Trace($"change returned to {player.GetDebugName()} ({player.entityId}) in the amount of {value}");
        }

        private static void GiveChange(EntityPlayerLocal player)
        {
            if (!Change.TryGetValue(player.entityId, out var value))
            {
                _log.Error($"Client requested change for {player.entityId}, but there is no change cached to return; this is unexpected");
                return;
            }
            _ = Change.Remove(player.entityId);
            AddCoins(player, value);
        }

        private static void AddCoins(ClientInfo clientInfo, Vector3 pos, int amount)
        {
            var itemStack = new ItemStack(CASINO_COIN_ITEM_VALUE, amount);
            var entityId = EntityFactory.nextEntityID++;
            GameManager.Instance.World.SpawnEntityInWorld((EntityItem)EntityFactory.CreateEntity(new EntityCreationData
            {
                entityClass = EntityClass.FromString("item"),
                id = entityId,
                itemStack = itemStack,
                pos = pos,
                rot = new Vector3(20f, 0f, 20f),
                lifetime = 60f,
                belongsPlayerId = clientInfo.entityId
            }));
            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageEntityCollect>().Setup(entityId, clientInfo.entityId));
            _ = GameManager.Instance.World.RemoveEntity(entityId, EnumRemoveEntityReason.Despawned);
        }

        private static void AddCoins(EntityPlayerLocal player, int amount)
        {
            player.AddUIHarvestingItem(new ItemStack(CASINO_COIN_ITEM_VALUE, amount));
        }

        private static int CountCoins(ItemStack[] stack)
        {
            var _count = 0;
            for (var i = 0; i < stack.Length; i++)
            {
                if (stack[i].IsEmpty())
                {
                    continue;
                }
                var itemValue = stack[i].itemValue;
                if (itemValue == null) { continue; } // TODO: this sanity check is probably not necessary
                //_log.Trace($"{itemValue.GetItemId()}");
                var itemClass = itemValue.ItemClass;
                if (itemClass == null) { continue; } // TODO: this sanity check is probably not necessary
                //_log.Trace($"name: {itemClass.Name}");
                if (itemClass.HasAnyTags(moneyTag))
                {
                    _count += stack[i].count;
                }
            }
            return _count;
        }
    }
}
