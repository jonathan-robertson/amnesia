using Amnesia.Data;
using Amnesia.Utilities;
using System;
using System.Collections;
using UnityEngine;

namespace Amnesia.Handlers {
    internal class GameStartDone {
        private static readonly ModLog log = new ModLog(typeof(GameStartDone));
        public static WaitForSeconds Wait { get; private set; } = new WaitForSeconds(2f);

        public static void Handle() {
            try {
                log.Trace("OnGameStartDone");
                ThreadManager.StartCoroutine(MonitorSchedule());
            } catch (Exception e) {
                log.Error("Failed to handle OnGameStartDone", e);
            }
        }

        private static IEnumerator MonitorSchedule() {
            log.Trace("MonitorSchedule start");
            while (true) {
                foreach (var clientInfo in ConnectionManager.Instance.Clients.List) {
                    ProcessRecovery(clientInfo);
                }
                yield return Wait;
            }
        }


        // TODO: should probably delete this because logic for swapping buffs has been offloaded to client buffs update cycle (yay)
        private static void ProcessRecovery(ClientInfo clientInfo) {
            // Fetch player if possible
            if (clientInfo == null || !GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out EntityPlayer player)) {
                return; // exit early if player cannot be found in active world; probably is in the process of logging in right now
            }

            if (player.Buffs.HasBuff("buffAmnesiaRecoverLife")) {

                // ensure player is in sync with max lives
                API.AdjustToMaxOrRemainingLivesChange(player);
                var remainingLives = player.GetCVar(Values.RemainingLivesCVar);
                if (remainingLives >= Config.MaxLives) {
                    player.Buffs.AddBuff("buffAmnesiaRecoverLifeMaxed");
                    GiveItem(clientInfo, player, "amnesiaSmellingSalts", 1);
                    return; // at max
                }

                // ensure skill point is available
                if (player.Progression.SkillPoints == 0) {
                    player.Buffs.AddBuff("buffAmnesiaRecoverLifeMissingPoint");
                    GiveItem(clientInfo, player, "amnesiaSmellingSalts", 1);
                    return; // not enough skill points
                }

                // restore 1 life
                player.Buffs.AddBuff("buffAmnesiaRecoverLifeSuccess");
                player.SetCVar(Values.RemainingLivesCVar, remainingLives + 1);

                player.Progression.SkillPoints--;
                player.Progression.bProgressionStatsChanged = true;
                //player.bPlayerStatsChanged = true; // TODO: remove? test this
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerStats>().Setup(player), false, player.entityId);
            }
        }

        private static void GiveItem(ClientInfo clientInfo, EntityPlayer player, string name, int count) {
            ItemValue _itemValue = ItemClass.GetItem(name, false);
            ItemStack itemStack = new ItemStack(_itemValue, count);

            var entityItem = (EntityItem)EntityFactory.CreateEntity(new EntityCreationData {
                entityClass = EntityClass.FromString("item"),
                id = EntityFactory.nextEntityID++,
                itemStack = itemStack,
                pos = player.position,
                rot = new Vector3(20f, 0f, 20f),
                lifetime = 60f,
                belongsPlayerId = clientInfo.entityId
            });
            GameManager.Instance.World.SpawnEntityInWorld(entityItem);
            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageEntityCollect>().Setup(entityItem.entityId, clientInfo.entityId));
            GameManager.Instance.World.RemoveEntity(entityItem.entityId, EnumRemoveEntityReason.Despawned);
        }
    }
}
