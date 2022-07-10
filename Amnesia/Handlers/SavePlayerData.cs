using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers {
    internal class SavePlayerData {
        private static readonly ModLog log = new ModLog(typeof(SavePlayerData));

        public static void Handle(ClientInfo clientInfo, PlayerDataFile playerDataFile) {
            try {
                // [!] ALTERNATIVE DIALOG OPTION PLANS
                // One could use this requirement with PlayerItemCount item_name="<item name>" (An expensive function that should be used sparingly.)
                // this would be used to confirm if the player has enough money for the dialog option. Actual money removal can be handled later.
                // <requirement name="PlayerItemCount" item_name="casinoCoin" operation="GTE" value="5000"/>

                /*
                if (clientInfo != null && GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player)) {
                    NetPackageEntityAddExpClient package = NetPackageManager.GetPackage<NetPackageEntityAddExpClient>().Setup(player.entityId, 1, Progression.XPTypes.Kill);
                    SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(package, _onlyClientsAttachedToAnEntity: false, player.entityId);
                    //GameManager.Instance.SharedKillServer(killedEntity.entityId, entityId, xpModifier);

                    log.Info($"OnSavePlayerData: {player.GetDebugName()} - SkillPoints: {player.Progression.SkillPoints}");
                }
                */

                //ProcessRecovery(clientInfo);
            } catch (Exception e) {
                log.Error("Failed to handle OnSavePlayerData", e);
            }
        }
    }
}
