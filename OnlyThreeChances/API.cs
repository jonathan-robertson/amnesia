using OnlyThreeChances.Data;

namespace OnlyThreeChances {
    internal class API : IModApi {
        public void InitMod(Mod _modInstance) {
            if (Config.Load()) {
                // TODO: add hooks
                Log.Out($"[OnlyThreeChances] MaxLives: {Config.MaxLives}");
            }
            //throw new NotImplementedException();
        }
    }
}
