using StardewModdingAPI;

namespace EideeEasyFishing
{
    internal class ModConfigKeys
    {
        public SButton ReloadConfig { get; }
        public SButton ToggleMod { get; }
        public SButton StopAutoRecast { get; }

        public ModConfigKeys(SButton reloadConfig, SButton toggleMod, SButton stopAutoRecast)
        {
            ReloadConfig = reloadConfig;
            ToggleMod = toggleMod;
            StopAutoRecast = stopAutoRecast;
        }
    }
}
