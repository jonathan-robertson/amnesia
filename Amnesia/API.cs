using Amnesia.Data;
using Amnesia.Handlers;
using Amnesia.Utilities;
using System.Collections.Generic;

namespace Amnesia {
    internal class API : IModApi {
        private static readonly ModLog log = new ModLog(typeof(API));

        public static string QuestResetKickReason { get; private set; } = "This server is configured to erase some settings from your player file when you die for the final time. Please reconnect whenever you're ready.";

        public static Dictionary<int, bool> Obituary { get; private set; } = new Dictionary<int, bool>();

        public void InitMod(Mod _modInstance) {
            ModEvents.GameStartDone.RegisterHandler(Config.Load);
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld.Handle);
            ModEvents.GameMessage.RegisterHandler(GameMessage.Handle);
            ModEvents.SavePlayerData.RegisterHandler(SavePlayerData.Handle);
        }
    }
}
