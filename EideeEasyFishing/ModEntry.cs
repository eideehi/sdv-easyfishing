using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace EideeEasyFishing
{
    internal class ModEntry : Mod
    {
        private ModConfig _config;
        private ModConfigKeys _keys;

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);

            _config = Helper.ReadConfig<ModConfig>();
            _keys = _config.Controls.ParseControls();

            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null) return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => _config = new ModConfig(),
                save: () => Helper.WriteConfig(_config));

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: I18n.Config_Section_General_Name);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_BiteFaster_Name,
                tooltip: I18n.Config_BiteFaster_Description,
                getValue: () => _config.BiteFaster,
                setValue: value => _config.BiteFaster = value);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_HitAutomatically_Name,
                tooltip: I18n.Config_HitAutomatically_Description,
                getValue: () => _config.HitAutomatically,
                setValue: value => _config.HitAutomatically = value);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_TreasureAlwaysBeFound_Name,
                tooltip: I18n.Config_TreasureAlwaysBeFound_Description,
                getValue: () => _config.TreasureAlwaysBeFound,
                setValue: value => _config.TreasureAlwaysBeFound = value);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_AlwaysCaughtDoubleFish_Name,
                tooltip: I18n.Config_AlwaysCaughtDoubleFish_Description,
                getValue: () => _config.AlwaysCaughtDoubleFish,
                setValue: value => _config.AlwaysCaughtDoubleFish = value);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_CaughtDoubleFishOnAnyBait_Name,
                tooltip: I18n.Config_CaughtDoubleFishOnAnyBait_Description,
                getValue: () => _config.CaughtDoubleFishOnAnyBait,
                setValue: value => _config.CaughtDoubleFishOnAnyBait = value);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_AlwaysMaxCastPower_Name,
                tooltip: I18n.Config_AlwaysMaxCastPower_Description,
                getValue: () => _config.AlwaysMaxCastPower,
                setValue: value => _config.AlwaysMaxCastPower = value);

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: I18n.Config_Section_Minigame_Name);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_SkipMinigame_Name,
                tooltip: I18n.Config_SkipMinigame_Description,
                getValue: () => _config.SkipMinigame,
                setValue: value => _config.SkipMinigame = value);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_FishEasyCaught_Name,
                tooltip: I18n.Config_FishEasyCaught_Description,
                getValue: () => _config.FishEasyCaught,
                setValue: value => _config.FishEasyCaught = value);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_TreasureEasyCaught_Name,
                tooltip: I18n.Config_TreasureEasyCaught_Description,
                getValue: () => _config.TreasureEasyCaught,
                setValue: value => _config.TreasureEasyCaught = value);
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs args)
        {
            var player = Game1.player;
            if (player is not { IsLocalPlayer: true }) return;

            if (args.NewMenu is BobberBar bar)
            {
                if (_config.TreasureAlwaysBeFound)
                {
                    Helper.Reflection.GetField<bool>(bar, "treasure").SetValue(true);
                }

                if (_config.SkipMinigame)
                {
                    if (player.CurrentTool is FishingRod rod)
                    {
                        var whichFish = Helper.Reflection.GetField<string>(bar, "whichFish").GetValue();
                        var fishSize = Helper.Reflection.GetField<int>(bar, "fishSize").GetValue();
                        var fishQuality = Helper.Reflection.GetField<int>(bar, "fishQuality").GetValue();
                        var difficulty = Helper.Reflection.GetField<float>(bar, "difficulty").GetValue();
                        var treasure = Helper.Reflection.GetField<bool>(bar, "treasure").GetValue();
                        var fromFishPond = Helper.Reflection.GetField<bool>(bar, "fromFishPond").GetValue();
                        var setFlagOnCatch = Helper.Reflection.GetField<string>(bar, "setFlagOnCatch").GetValue();
                        var bossFish = Helper.Reflection.GetField<bool>(bar, "bossFish").GetValue();
                        var numCaught = 1;

                        if (!bossFish)
                        {
                            if (_config.CaughtDoubleFishOnAnyBait || rod?.GetBait()?.QualifiedItemId == "(O)774") {
                                numCaught = (_config.AlwaysCaughtDoubleFish || Game1.random.NextDouble() < (0.25 + (Game1.player.DailyLuck / 2.0))) ? 2 : 1;
                            }
                        }

                        if (Game1.isFestival())
                        {
                            Game1.CurrentEvent.perfectFishing();
                        }

                        rod.pullFishFromWater(whichFish, fishSize, fishQuality, (int)difficulty, treasure, true, fromFishPond, setFlagOnCatch, bossFish, numCaught);

                        Game1.exitActiveMenu();
                        Game1.setRichPresence("location", (object)Game1.currentLocation.Name);
                    }
                }
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs args)
        {
            var player = Game1.player;
            if (player is not { IsLocalPlayer: true }) return;

            if (player.CurrentTool is FishingRod rod)
            {
                if (_config.AlwaysMaxCastPower && rod.isTimingCast)
                {
                    rod.castingTimerSpeed = 0;
                    rod.castingPower = 1;
                }

                if (_config.BiteFaster && !rod.isNibbling && rod.isFishing && !rod.isReeling &&
                    !rod.pullingOutOfWater && !rod.hit)
                {
                    rod.timeUntilFishingBite = 0;
                }

                if (_config.HitAutomatically && rod.isNibbling && rod.isFishing && !rod.isReeling &&
                    !rod.pullingOutOfWater && !rod.hit)
                {
                    Farmer.useTool(player);
                }

                if (!_config.SkipMinigame && _config.AlwaysCaughtDoubleFish)
                {
                    rod.numberOfFishCaught = (!rod.bossFish && (_config.CaughtDoubleFishOnAnyBait || rod?.GetBait()?.QualifiedItemId == "(O)774")) ? 2 : 1;
                }
            }

            if (Game1.activeClickableMenu is BobberBar bar)
            {
                var bobberBarPos = Helper.Reflection.GetField<float>(bar, "bobberBarPos").GetValue();
                var bobberBarHeight = Helper.Reflection.GetField<int>(bar, "bobberBarHeight").GetValue();

                if (_config.FishEasyCaught)
                {
                    Helper.Reflection.GetField<float>(bar, "bobberPosition")
                        .SetValue(bobberBarPos + (bobberBarHeight / 2) - 25);
                }

                if (_config.TreasureEasyCaught)
                {
                    Helper.Reflection.GetField<float>(bar, "treasurePosition")
                        .SetValue(bobberBarPos + (bobberBarHeight / 2) - 25);
                }
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs args)
        {
            if (!Context.IsWorldReady) return;

            if (args.Button == _keys.ReloadConfig)
            {
                _config = Helper.ReadConfig<ModConfig>();
                _keys = _config.Controls.ParseControls();
                Game1.addHUDMessage(new HUDMessage(I18n.Message_Config_Reload(), HUDMessage.error_type)
                {
                    noIcon = true,
                    timeLeft = HUDMessage.defaultTime
                });
            }
        }
    }
}