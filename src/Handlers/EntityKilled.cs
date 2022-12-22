using Amnesia.Data;
using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers {
    internal class EntityKilled {
        private static readonly ModLog<EntityKilled> log = new ModLog<EntityKilled>();

        internal static void Handle(Entity killedEntity, Entity killerEntity) {
            try {
                if (killerEntity == null || killerEntity.entityType != EntityType.Player) { return; }
                if (!Config.PositiveOutlookTimeOnKill.TryGetValue(killedEntity.GetDebugName(), out var entry)) {
                    return;
                }

                var minutes = entry.value / 60f;
                MessagingSystem.Broadcast($"[007fff]{killerEntity.GetDebugName()} just took down a {entry.name}!");
                MessagingSystem.Broadcast($"[007fff]Relief washes over each survivor as a newfound confidence takes hold: [00ff80]all online players receive Double XP for {(minutes > 1 ? minutes + " Minutes!" : entry.value + " Seconds!")}");
                foreach (var player in GameManager.Instance.World.Players.list) {
                    _ = PlayerHelper.AddPositiveOutlookTime(player, entry.value);
                }
            } catch (Exception e) {
                log.Error("HandleEntityKilled", e);
            }
        }
    }
}
