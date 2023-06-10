using Amnesia.Data;
using Amnesia.Handlers;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace Amnesia
{
    internal class ModApi : IModApi
    {
        public static bool DebugMode { get; set; } = true; // TODO: set to false before release
        public static Dictionary<int, bool> Obituary { get; private set; } = new Dictionary<int, bool>();

        public void InitMod(Mod _modInstance)
        {
            var harmony = new Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ModEvents.GameStartDone.RegisterHandler(Config.Load);
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld.Handle);
            ModEvents.GameMessage.RegisterHandler(GameMessage.Handle);
            ModEvents.SavePlayerData.RegisterHandler(SavePlayerData.Handle);
            ModEvents.EntityKilled.RegisterHandler(EntityKilled.Handle);
            ModEvents.PlayerDisconnected.RegisterHandler(PlayerDisconnected.Handle);
        }
    }
}
