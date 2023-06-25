using Amnesia.Data;
using Amnesia.Utilities;
using HarmonyLib;
using System;

namespace Amnesia.Patches
{
    /// <summary>
    /// Monitor added buffs.
    /// </summary>
    /// <remarks>Supports: local and remote</remarks>
    [HarmonyPatch(typeof(EntityBuffs), "AddBuff")]
    internal class EntityBuffs_AddBuff_Patches
    {
        private static readonly ModLog<EntityBuffs_AddBuff_Patches> _log = new ModLog<EntityBuffs_AddBuff_Patches>();

        public static void Postfix(EntityBuffs __instance, string _name)
        {
            try
            {
                if (Values.BuffTryBuyTreatment.Equals(_name))
                {
                    DialogShop.TryBuyTreatment(__instance.parent);
                    return;
                }
                if (Values.BuffTryBuyTherapy.Equals(_name))
                {
                    DialogShop.TryBuyTherapy(__instance.parent);
                    return;
                }
                if (Values.BuffRequestChangeCallback.Equals(_name))
                {
                    DialogShop.TryGiveChange(__instance.parent);
                    return;
                }
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
