using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers {
    internal class GameMessage {
        private static readonly ModLog log = new ModLog(typeof(GameMessage));

        public static bool Handle(ClientInfo clientInfo, EnumGameMessages messageType, string message, string mainName, bool localizeMain, string secondaryName, bool localizeSecondary) {
            try {
                switch (messageType) {
                    case EnumGameMessages.EntityWasKilled:
                        if (!GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player)) {
                            return true; // player not present; skip
                        }
                        log.Debug($"EnumGameMessages.EntityWasKilled {clientInfo.entityId}");

                        // TODO: add admin option for this
                        var killerClient = ConnectionManager.Instance.Clients.GetForNameOrId(secondaryName);
                        if (killerClient != null) {
                            log.Trace($"{mainName} was killed by {secondaryName}, so {mainName} will NOT lose a life.");
                            return true; // being killed in pvp doesn't count against player
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
