using Amnesia.Data;
using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers {
    internal class EntityKilled {
        private static readonly ModLog log = new ModLog(typeof(EntityKilled));

        internal static void Handle(Entity killedEntity, Entity killerEntity) {
            try {
                if (killerEntity == null || killerEntity.entityType != EntityType.Player) { return; }
                if (!Config.PositiveOutlookTimeOnKill.TryGetValue(killedEntity.GetDebugName(), out var seconds)) {
                    return;
                }

                var minutes = seconds / 60f;
                MessagingSystem.Broadcast($"[007fff]{killerEntity.GetDebugName()} just killed a {killedEntity.GetDebugName()}!");
                MessagingSystem.Broadcast($"[007fff]Relief washes over each survivor as a newfound confidence takes hold: [00ff80]all online players receive Double XP for {(minutes > 1 ? minutes + " Minutes!" : seconds + " Seconds!")}");
                foreach (var player in GameManager.Instance.World.Players.list) {
                    PlayerHelper.AddPositiveOutlookTime(player, seconds);
                }
            } catch (Exception e) {
                log.Error("HandleEntityKilled", e);
            }
        }
    }
}
