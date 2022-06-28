using OnlyThreeChances.Data;

namespace OnlyThreeChances {
    internal class API : IModApi {
        public void InitMod(Mod _modInstance) {
            if (Config.Load()) {
                Log.Out($"[OnlyThreeChances] MaxLives: {Config.MaxLives}");
                // TODO: add hooks
            } else {
                Log.Error("[OnlyThreeChances] Unable to load or recover from configuration issue; leaving this mod inactive.");
            }
            //throw new NotImplementedException();
        }
    }
}
