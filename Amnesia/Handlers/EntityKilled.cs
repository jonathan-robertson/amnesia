using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers {
    internal class EntityKilled {
        private static readonly ModLog log = new ModLog(typeof(EntityKilled));

        internal static void Handle(Entity killedEntity, Entity killerEntity) {
            try {
                if (killerEntity == null || killerEntity.entityType != EntityType.Player) { return; }
                switch (killedEntity.GetDebugName()) {
                    case "ZombieJuggernaut":
                        TriggerKillAnnouncementAndBonus(killerEntity.GetDebugName(), "[ff4500]Juggernaut", 15);
                        break;
                    case "ZombieJuggernautGolden":
                        TriggerKillAnnouncementAndBonus(killerEntity.GetDebugName(), "[ffff00]Golden Juggernaut", 15);
                        break;
                    case "zombieScorcher":
                        TriggerKillAnnouncementAndBonus(killerEntity.GetDebugName(), "[e32636]Scorcher", 5);
                        break;
                    case "zombieDemolition":
                        TriggerKillAnnouncementAndBonus(killerEntity.GetDebugName(), "[ffbf00]Demolition", 1);
                        break;
                }
            } catch (Exception e) {
                log.Error("HandleEntityKilled", e);
            }
        }

        internal static void TriggerKillAnnouncementAndBonus(string playerName, string zombieName, int minutes) {
            MessagingSystem.Broadcast($"[007fff]{playerName} just killed a {zombieName}[007fff]!");
            MessagingSystem.Broadcast($"[007fff]Relief washes over each survivor as a newfound confidence takes hold: [00ff80]all online players receive Double XP for {minutes} Minutes!");
            GameManager.Instance.World.Players.list.ForEach(player => player.Buffs.AddBuff($"triggerAmnesiaPositiveOutlookBoost{minutes}"));
        }
    }
}
