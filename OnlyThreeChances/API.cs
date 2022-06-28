using OnlyThreeChances.Data;
using OnlyThreeChances.Utilities;

namespace OnlyThreeChances {
    internal class API : IModApi {
        private static readonly ModLog log = new ModLog(typeof(API));

        public void InitMod(Mod _modInstance) {
            if (Config.Load()) {
                log.Info($"[OnlyThreeChances] MaxLives: {Config.MaxLives}");
                // TODO: add hooks
            } else {
                log.Error("Unable to load or recover from configuration issue; this mod will not activate.");
            }
            //throw new NotImplementedException();
        }
    }
}
