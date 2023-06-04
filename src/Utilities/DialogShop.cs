using System;
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
        private const string GAME_EVENT_PAY_FROM_BAG = "amnesia_pay_from_bag";
        private const string GAME_EVENT_PAY_FROM_BLT = "amnesia_pay_from_blt";
        public const string GAME_EVENT_REQUEST_CHANGE = "amnesia_request_change";

        private const string CVAR_TREATMENT_PRICE_NAME = "amnesiaTreatmentPrice";
        private const string CVAR_THERAPY_PRICE_NAME = "amnesiaTherapyPrice";

        private static readonly ModLog<DialogShop> _log = new ModLog<DialogShop>();
        private static readonly ItemValue CASINO_COIN_ITEM_VALUE = ItemClass.GetItem("casinoCoin", false);
        private static readonly FastTags moneyTag = FastTags.Parse("amnesiaCurrency");

        public static Dictionary<int, int> BagMoney { get; private set; } = new Dictionary<int, int>();
        public static Dictionary<int, int> BltMoney { get; private set; } = new Dictionary<int, int>();
        public static Dictionary<int, int> Change { get; private set; } = new Dictionary<int, int>();

        public static void UpdateMoneyTracker(int entityId, ItemStack[] ___toolbelt = null, ItemStack[] ___bag = null)
        {
            if (___toolbelt != null) { BltMoney[entityId] = CountCoins(___toolbelt); }
            if (___bag != null) { BagMoney[entityId] = CountCoins(___bag); }

            _ = BltMoney.TryGetValue(entityId, out var blt);
            _ = BagMoney.TryGetValue(entityId, out var bag);
            _log.Debug($"blt: {blt}, bag: {bag}, total: {blt + bag}");
        }

        public static void UpdatePrices(EntityPlayer player)
        {
            player.SetCVar(CVAR_TREATMENT_PRICE_NAME, GetCost(player.Progression.Level, Product.Treatment));
        }

        public static void GiveChange(ClientInfo clientInfo, EntityPlayer player)
        {
            if (!Change.TryGetValue(clientInfo.entityId, out var value))
            {
                _log.Error($"Client requested change for {clientInfo.entityId}, but there is no change cached to return; this is unexpected");
                return;
            }
            AddCoins(clientInfo, player.position, value);
            _log.Trace($"change returned to {player.GetDebugName()} ({player.entityId}) in the amount of {value}");
        }

        public static void GiveChange(EntityPlayerLocal player)
        {
            if (!Change.TryGetValue(player.entityId, out var value))
            {
                _log.Error($"Client requested change for {player.entityId}, but there is no change cached to return; this is unexpected");
                return;
            }
            AddCoins(player, value);
        }

        public static int GetCost(int level, Product product)
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

        public static bool TryPurchase(ClientInfo clientInfo, EntityPlayer player, int cost)
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

        internal static bool TryPurchase(EntityPlayerLocal player, int cost)
        {
            throw new NotImplementedException(); // TODO: complete for local player support
        }

        private static bool CanAfford(int entityId, int amount)
        {
            _ = BagMoney.TryGetValue(entityId, out var bag);
            _ = BltMoney.TryGetValue(entityId, out var blt);
            return bag + blt - amount >= 0;
        }

        public static void Pay(ClientInfo clientInfo, EntityPlayer player, int cost)
        {
            _log.Trace($"player {player.GetDebugName()} charge attempt for {cost}");
            if (BagMoney.TryGetValue(clientInfo.entityId, out var bag))
            {
                TriggerGameEvent(clientInfo, player, GAME_EVENT_PAY_FROM_BAG);
                if (bag >= cost)
                {
                    if (bag > cost)
                    {
                        //AddCoins(clientInfo, player.position, bag - cost);
                        //BagMoney[player.entityId] = bag - cost;
                        Change.Add(player.entityId, bag - cost);
                        TriggerGameEvent(clientInfo, player, GAME_EVENT_REQUEST_CHANGE);
                    }
                    else
                    {
                        //BagMoney[player.entityId] = 0;
                    }
                    return;
                }
                cost -= bag;
            }
            if (BltMoney.TryGetValue(clientInfo.entityId, out var blt))
            {
                TriggerGameEvent(clientInfo, player, GAME_EVENT_PAY_FROM_BLT);
                if (blt > cost)
                {
                    //AddCoins(clientInfo, player.position, blt - cost);
                    //BltMoney[player.entityId] = blt - cost;
                    Change.Add(player.entityId, blt - cost);
                    TriggerGameEvent(clientInfo, player, GAME_EVENT_REQUEST_CHANGE);
                }
                else
                {
                    //BltMoney[player.entityId] = 0;
                }
            }
        }

        private static void TriggerGameEvent(ClientInfo clientInfo, EntityPlayer player, string eventName)
        {
            // TODO: handle return value? (false may mean failure)
            _ = GameEventManager.Current.HandleAction(eventName, null, player, false);
            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageGameEventResponse>().Setup(eventName, clientInfo.playerName, "", "", NetPackageGameEventResponse.ResponseTypes.Approved));
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
                _log.Trace($"{itemValue.GetItemId()}");
                var itemClass = itemValue.ItemClass;
                if (itemClass == null) { continue; } // TODO: this sanity check is probably not necessary
                _log.Trace($"name: {itemClass.Name}");
                if (itemClass.HasAnyTags(moneyTag))
                {
                    _count += stack[i].count;
                }
            }
            return _count;
        }
    }
}
