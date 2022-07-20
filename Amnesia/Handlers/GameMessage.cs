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
