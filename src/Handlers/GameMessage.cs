using Amnesia.Data;
using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers {
    internal class GameMessage {
        private static readonly ModLog log = new ModLog(typeof(GameMessage));

        public static bool Handle(ClientInfo clientInfo, EnumGameMessages messageType, string message, string mainName, bool localizeMain, string secondaryName, bool localizeSecondary) {
            if (!Config.Loaded) { return true; } // do not interrupt other mods from processing event
            try {
                switch (messageType) {
                    case EnumGameMessages.EntityWasKilled:
                        if (!GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player)) {
                            return true; // player not present; skip
                        }

                        if (player.Buffs.HasBuff(Values.BloodmoonLifeProtectionBuff) || player.Buffs.HasBuff(Values.PostBloodmoonLifeProtectionBuff)) {
                            log.Trace($"{clientInfo.InternalId.CombinedString} ({player.GetDebugName()}) died but had bloodmoon life protection.");
                            return true; // player had protection
                        } else {
                            log.Trace($"{clientInfo.InternalId.CombinedString} ({player.GetDebugName()}) died and did not have bloodmoon life protection.");
                        }

                        // TODO: add admin option for this
                        if (mainName != secondaryName) {
                            var killerClient = ConnectionManager.Instance.Clients.GetForNameOrId(secondaryName);
                            if (killerClient != null) {
                                log.Trace($"{clientInfo.InternalId.CombinedString} ({player.GetDebugName()}) was killed by {secondaryName} but this server has pvp deaths set to not remove lives.");
                                return true; // being killed in pvp doesn't count against player
                            }
                        }

                        if (!API.Obituary.ContainsKey(clientInfo.entityId)) {
                            API.Obituary.Add(clientInfo.entityId, true);
                        }
                        break;
                }
            } catch (Exception e) {
                log.Error("Failed to handle GameMessage event.", e);
            }
            return true; // do not interrupt other mods from processing event
        }
    }
}
