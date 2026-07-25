using System;
using System.Linq;
using StardewModdingAPI;

namespace EideeEasyFishing
{
    internal class ModConfigRawKeys
    {
        public string ReloadConfig { get; set; } = SButton.F5.ToString();
        public string ToggleMod { get; set; } = SButton.F6.ToString();
        public string StopAutoRecast { get; set; } = SButton.None.ToString();

        private static SButton ParseButton(string button, SButton defaultButton)
        {
            return (from SButton value in Enum.GetValues(typeof(SButton))
                    let name = Enum.GetName(typeof(SButton), value)
                    where name is not null && name.Equals(button, StringComparison.OrdinalIgnoreCase)
                    select value)
                .DefaultIfEmpty(defaultButton)
                .FirstOrDefault();
        }

        public ModConfigKeys ParseControls()
        {
            return new ModConfigKeys(
                reloadConfig: ParseButton(ReloadConfig, SButton.F5),
                toggleMod: ParseButton(ToggleMod, SButton.F6),
                stopAutoRecast: ParseButton(StopAutoRecast, SButton.None));
        }

        public SButton ParseStopAutoRecast() => ParseButton(StopAutoRecast, SButton.None);
    }
}
