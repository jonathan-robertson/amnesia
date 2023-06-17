using Amnesia.Data;
using Amnesia.Utilities;
using HarmonyLib;
using System;

namespace Amnesia.Patches
{
    /// <summary>
    /// Track player skill levels as they change over time.
    /// </summary>
    /// <remarks>Supports: Remote</remarks>
    [HarmonyPatch(typeof(NetPackageEntitySetSkillLevelServer), "ProcessPackage")]
    internal class NetPackageEntitySetSkillLevelServer_ProcessPackage_Patches
    {
        private static readonly ModLog<NetPackageEntitySetSkillLevelServer_ProcessPackage_Patches> _log = new ModLog<NetPackageEntitySetSkillLevelServer_ProcessPackage_Patches>();

        public static void Postfix(NetPackageEntitySetSkillLevelServer __instance, World _world, int ___entityId, string ___skill, int ___level)
        {
            try
            {
                _log.Trace($"___entityId: {___entityId}, ___skill: {___skill}, ___level: {___level}");
                if (!PlayerRecord.Entries.TryGetValue(___entityId, out var record))
                {
                    _log.Error($"Unable to retrieve player record for entityId {___entityId}");
                    return;
                }
                if (!_world.Players.dict.TryGetValue(___entityId, out var player))
                {
                    _log.Error($"Unable to retrieve player for entityId {___entityId}");
                    return;
                }
                _log.Trace($"Player {___entityId} {player.GetDebugName()} increased in level: {___skill} >> {___level}");
                var progressionClass = player.Progression.GetProgressionValue(___skill).ProgressionClass;
                if (progressionClass.IsAttribute || progressionClass.IsPerk) // don't track action skills or books
                {
                    record.PurchaseSkill(___skill, ___level, progressionClass.CalculatedCostForLevel(___level));

                    // TODO: tell client to refresh server's skillPoints value now?
                    //__instance.Sender.SendPackage(NetPackageManager.GetPackage<NetPackageConsoleCmdClient>().Setup("sm", true));
                    //__instance.Sender.SendPackage(NetPackageManager.GetPackage<NetPackageConsoleCmdClient>().Setup("sm", true));
                }
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
