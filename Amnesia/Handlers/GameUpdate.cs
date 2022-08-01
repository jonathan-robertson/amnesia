using Amnesia.Data;
using Amnesia.Utilities;
using System;

namespace Amnesia.Handlers {
    internal class GameUpdate {
        private static readonly ModLog log = new ModLog(typeof(GameUpdate));
        private static readonly uint _ceiling = 100;
        private static uint _counter = 0;
        private static bool isBloodmoon = false;

        internal static void Handle() {
            if (!Config.Loaded || !Config.ProtectMemoryDuringBloodmoon) { return; }
            try {
                _counter++;
                if (_counter > _ceiling) {
                    HandleBloodMoon();
                    _counter = 0;
                }
            } catch (Exception e) {
                log.Error("Failed to handle GameUpdate event.", e);
            }
        }

        private static void HandleBloodMoon() {
            try {
                if (isBloodmoon == GameManager.Instance.World.aiDirector.BloodMoonComponent.BloodMoonActive) {
                    return;
                }
                isBloodmoon = !isBloodmoon;

                if (isBloodmoon) {
                    GameManager.Instance.World.Players.list.ForEach(p => p.Buffs.AddBuff(Values.BloodmoonLifeProtectionBuff));
                } else {
                    GameManager.Instance.World.Players.list.ForEach(p => p.Buffs.RemoveBuff(Values.BloodmoonLifeProtectionBuff));
                }
            } catch (Exception e) {
                log.Error("Failed to handle bloodmoon.", e);
            }
        }
    }
}
