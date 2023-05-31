using Amnesia.Data;
using Amnesia.Utilities;
using HarmonyLib;
using System;

namespace Amnesia.Patches
{
    [HarmonyPatch(typeof(AIDirectorBloodMoonComponent), "StartBloodMoon")]
    internal class AIDirectorBloodMoonComponent_StartBloodMoon_Patches
    {
        private static readonly ModLog<AIDirectorBloodMoonComponent_StartBloodMoon_Patches> _log = new ModLog<AIDirectorBloodMoonComponent_StartBloodMoon_Patches>();

        public static void Postfix()
        {
            try
            {
                _log.Trace("Prefix: StartBloodMoon triggered");
                var players = GameManager.Instance.World.Players.list;
                for (var i = 0; i < players.Count; i++)
                {
                    _ = players[i].Buffs.AddBuff(Values.BuffBloodmoonLifeProtection);
                }
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }

    [HarmonyPatch(typeof(AIDirectorBloodMoonComponent), "EndBloodMoon")]
    internal class AIDirectorBloodMoonComponent_EndBloodMoon_Patches
    {
        private static readonly ModLog<AIDirectorBloodMoonComponent_EndBloodMoon_Patches> _log = new ModLog<AIDirectorBloodMoonComponent_EndBloodMoon_Patches>();

        public static void Postfix()
        {
            try
            {
                _log.Trace("Prefix: EndBloodMoon triggered");
                var players = GameManager.Instance.World.Players.list;
                for (var i = 0; i < players.Count; i++)
                {
                    _ = players[i].Buffs.AddBuff(Values.BuffPostBloodmoonLifeProtection);
                    players[i].Buffs.RemoveBuff(Values.BuffBloodmoonLifeProtection);
                }
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
