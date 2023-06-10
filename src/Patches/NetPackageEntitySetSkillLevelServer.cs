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

        public static void Postfix(int ___entityId, string ___skill, int ___level)
        {
            try
            {
                _log.Trace($"___entityId: {___entityId}, ___skill: {___skill}, ___level: {___level}");
                if (!PlayerRecord.Entries.TryGetValue(___entityId, out var playerStats))
                {
                    _log.Error($"Unable to retrieve player record for entityId {___entityId}");
                    return;
                }
                playerStats.Changes.Add((___skill, ___level));
                playerStats.Save();
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
