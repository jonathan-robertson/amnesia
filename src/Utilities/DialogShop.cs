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

        public static Dictionary<int, int> BltMoney { get; private set; } = new Dictionary<int, int>();
        public static Dictionary<int, int> BagMoney { get; private set; } = new Dictionary<int, int>();
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
            if (changed && ModApi.DebugMode)
            {
                _log.Debug($"Held Money changed for player {entityId} (blt: {blt}, bag: {bag}, total: {blt + bag})");
            }
        }

        public static void ClearMoneyReferences(ClientInfo clientInfo)
        {
            if (BltMoney.ContainsKey(clientInfo.entityId))
            {
                _ = BltMoney.Remove(clientInfo.entityId);
            }
            if (BagMoney.ContainsKey(clientInfo.entityId))
            {
                _ = BagMoney.Remove(clientInfo.entityId);
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

                var playerName = player.GetDebugName();
                var userId = ClientInfoHelper.GetUserIdentifier(clientInfo);

                if (!player.Buffs.HasBuff(Values.BuffFragileMemory))
                {
                    _log.Info($"player {player.entityId} ({playerName} | {userId}) requested Treatment from trader but doesn't have a Fragile Memory to heal.");
                    PlayerHelper.OpenWindow(clientInfo, Values.WindowShopTreatmentUnnecessary);
                    PlayerHelper.TriggerGameEvent(clientInfo, player, Values.GameEventShopUnnecessary);
                    return;
                }

                _ = BltMoney.TryGetValue(player.entityId, out var blt);
                _ = BagMoney.TryGetValue(player.entityId, out var bag);
                var cost = GetCost(player.Progression.Level, Product.Treatment);
                if (TryPurchase(clientInfo, player, blt, bag, cost, out var change))
                {
                    _log.Info($"player {player.entityId} ({playerName} | {userId}) purchased Treatment from trader: {blt}+{bag} = {blt + bag} - {cost} = {change}");
                    player.Buffs.RemoveBuff(Values.BuffFragileMemory);
                    PlayerHelper.OpenWindow(clientInfo, Values.WindowShopTreatmentComplete);
                    PlayerHelper.TriggerGameEvent(clientInfo, player, Values.GameEventShopPurchased);
                }
                else
                {
                    _log.Info($"player {player.entityId} ({playerName} | {userId}) requested Treatment from trader but doesn't have enough money: {blt}+{bag} = {blt + bag} < {cost}");
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

                var playerName = player.GetDebugName();
                var userId = ClientInfoHelper.GetUserIdentifier(clientInfo);

                _ = BltMoney.TryGetValue(player.entityId, out var blt);
                _ = BagMoney.TryGetValue(player.entityId, out var bag);
                var cost = GetCost(player.Progression.Level, Product.Therapy);
                if (TryPurchase(clientInfo, player, blt, bag, cost, out var change))
                {
                    _log.Info($"player {player.entityId} ({playerName} | {userId}) purchased Therapy from trader: {blt}+{bag} = {blt + bag} - {cost} = {change}");
                    PlayerHelper.Respec(player);
                    PlayerHelper.OpenWindow(clientInfo, Values.WindowShopTherapyComplete);
                    PlayerHelper.TriggerGameEvent(clientInfo, player, Values.GameEventShopPurchased);
                }
                else
                {
                    _log.Info($"player {player.entityId} ({playerName} | {userId}) requested Therapy from trader but doesn't have enough money: {blt}+{bag} = {blt + bag} < {cost}");
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
                    return Config.TreatmentCostBase + (Config.TreatmentCostMultiplier * level);
                case Product.Therapy:
                    return Config.TherapyCostBase + (Config.TherapyCostMultiplier * level);
            }
            return -1;
        }

        private static bool TryPurchase(ClientInfo clientInfo, EntityPlayer player, int _blt, int _bag, int _cost, out int change)
        {
            if (!CanAfford(player.entityId, _cost))
            {
                _log.Trace($"player {player.GetDebugName()} could NOT afford {_cost}");
                change = -1;
                return false;
            }

            _log.Trace($"player {player.GetDebugName()} could afford {_cost}");
            string eventName;
            if (_bag >= _cost)
            {
                change = _bag - _cost;
                eventName = Values.GameEventPayFromBag;
            }
            else
            {
                change = _bag + _blt - _cost;
                eventName = Values.GameEventPayFromAll;
            }

            if (change > 0)
            {
                Change.Add(player.entityId, change);
                eventName += "_with_change";
            }
            PlayerHelper.TriggerGameEvent(clientInfo, player, eventName);
            return true;
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
