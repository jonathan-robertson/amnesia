using Amnesia.Utilities;
using HarmonyLib;
using System;

namespace Amnesia.Patches
{
    [HarmonyPatch(typeof(NetPackagePlayerInventory), "ProcessPackage")]
    internal class NetPackagePlayerInventory_ProcessPackage_Patches
    {
        private static readonly ModLog<NetPackagePlayerInventory_ProcessPackage_Patches> _log = new ModLog<NetPackagePlayerInventory_ProcessPackage_Patches>();

        public static void Postfix(NetPackagePlayerInventory __instance, World _world, ItemStack[] ___toolbelt, ItemStack[] ___bag)
        {
            try
            {
                if (___toolbelt == null && ___bag == null) { return; }
                var entityId = __instance.Sender.entityId; // TODO: refactor to support local player
                DialogShop.UpdateMoneyTracker(entityId, ___toolbelt, ___bag);
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
