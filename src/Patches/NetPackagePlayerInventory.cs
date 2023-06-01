using Amnesia.Utilities;
using HarmonyLib;
using System;

namespace Amnesia.Patches
{
    [HarmonyPatch(typeof(NetPackagePlayerInventory), "ProcessPackage")]
    internal class NetPackagePlayerInventory_ProcessPackage_Patch
    {
        private static readonly ModLog<NetPackagePlayerInventory_ProcessPackage_Patch> _log = new ModLog<NetPackagePlayerInventory_ProcessPackage_Patch>();

        public static void Postfix(NetPackagePlayerInventory __instance, World _world, ItemStack[] ___toolbelt, ItemStack[] ___bag)
        {
            try
            {
                if (___toolbelt == null && ___bag == null) { return; }
                var entityId = __instance.Sender.entityId; // TODO: refactor to support local player
                DialogShop.UpdateMoney(entityId, ___toolbelt, ___bag);
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
