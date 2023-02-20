using Amnesia.Data;
using Amnesia.Handlers;
using System.Collections.Generic;

namespace Amnesia
{
    internal class ModApi : IModApi
    {
        public static Dictionary<int, bool> Obituary { get; private set; } = new Dictionary<int, bool>();

        public void InitMod(Mod _modInstance)
        {
            ModEvents.GameStartDone.RegisterHandler(Config.Load);
            ModEvents.GameUpdate.RegisterHandler(GameUpdate.Handle);
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld.Handle);
            ModEvents.GameMessage.RegisterHandler(GameMessage.Handle);
            ModEvents.SavePlayerData.RegisterHandler(SavePlayerData.Handle);
            ModEvents.EntityKilled.RegisterHandler(EntityKilled.Handle);
        }
    }
}
