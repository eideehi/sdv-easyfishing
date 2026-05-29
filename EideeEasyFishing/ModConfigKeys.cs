using StardewModdingAPI;

namespace EideeEasyFishing
{
    internal class ModConfigKeys
    {
        public SButton ReloadConfig { get; }
        public SButton ToggleMod { get; }

        public ModConfigKeys(SButton reloadConfig, SButton toggleMod)
        {
            ReloadConfig = reloadConfig;
            ToggleMod = toggleMod;
        }
    }
}