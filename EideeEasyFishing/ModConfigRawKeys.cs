using System;
using System.Linq;
using StardewModdingAPI;

namespace EideeEasyFishing
{
    internal class ModConfigRawKeys
    {
        public string ReloadConfig { get; set; } = SButton.F5.ToString();

        private static SButton ParseButton(string button)
        {
            return (from SButton value in Enum.GetValues(typeof(SButton))
                let name = Enum.GetName(typeof(SButton), value)
                where name is not null && name.Equals(button, StringComparison.OrdinalIgnoreCase)
                select value).FirstOrDefault();
        }

        public ModConfigKeys ParseControls()
        {
            return new ModConfigKeys(reloadConfig: ParseButton(ReloadConfig));
        }
    }
}