using System;
using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Constants;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace EideeEasyFishing
{
    internal class ModEntry : Mod
    {
        private const string WildBaitQualifiedItemId = "(O)774";
        private const string SonarBobberQualifiedItemId = "(O)SonarBobber";

        private ModConfig _config;
        private ModConfigKeys _keys;

        private int _delayTick;
        private bool _syncMinigameState;
        private float _prevBobberPosition;
        private float _prevDistanceFromCatching;
        private float _prevTreasureCatchLevel;

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
                name: I18n.Config_AlwaysGoldenTreasure_Name,
                tooltip: I18n.Config_AlwaysGoldenTreasure_Description,
                getValue: () => _config.AlwaysGoldenTreasure,
                setValue: value => _config.AlwaysGoldenTreasure = value);

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

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_AlwaysSonarBobber_Name,
                tooltip: I18n.Config_AlwaysSonarBobber_Description,
                getValue: () => _config.AlwaysSonarBobber,
                setValue: value => _config.AlwaysSonarBobber = value);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: I18n.Config_FishMovementSpeedMultiplier_Name,
                tooltip: I18n.Config_FishMovementSpeedMultiplier_Description,
                getValue: () => _config.FishMovementSpeedMultiplier,
                setValue: value => _config.FishMovementSpeedMultiplier = value);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: I18n.Config_ProgressBarDecreaseMultiplier_Name,
                tooltip: I18n.Config_ProgressBarDecreaseMultiplier_Description,
                getValue: () => _config.ProgressBarDecreaseMultiplier,
                setValue: value => _config.ProgressBarDecreaseMultiplier = value);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: I18n.Config_ProgressBarIncreaseMultiplier_Name,
                tooltip: I18n.Config_ProgressBarIncreaseMultiplier_Description,
                getValue: () => _config.ProgressBarIncreaseMultiplier,
                setValue: value => _config.ProgressBarIncreaseMultiplier = value);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: I18n.Config_TreasureCatchSpeedMultiplier_Name,
                tooltip: I18n.Config_TreasureCatchSpeedMultiplier_Description,
                getValue: () => _config.TreasureCatchSpeedMultiplier,
                setValue: value => _config.TreasureCatchSpeedMultiplier = value);
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs args)
        {
            var player = Game1.player;
            if (player is not { IsLocalPlayer: true }) return;
            if (player.CurrentTool is not FishingRod rod) return;
            if (args.NewMenu is not BobberBar bar) return;

            if (_config.AlwaysSonarBobber && bar.bobbers != null &&
                !bar.bobbers.Contains(SonarBobberQualifiedItemId))
            {
                bar.bobbers.Add(SonarBobberQualifiedItemId);
            }

            ApplyTreasureState(rod, bar);

            if (!_config.SkipMinigame)
            {
                _delayTick = 8;
                _syncMinigameState = true;
                _prevBobberPosition = 0f;
                _prevDistanceFromCatching = 0f;
                _prevTreasureCatchLevel = 0f;

                // BobberBar starts at 0.3 in normal play and 0.1 for the tutorial in 1.6.15.
                if (bar.distanceFromCatching != 0.1f)
                {
                    bar.distanceFromCatching = 0.3f;
                }
            }
            else
            {
                var numCaught = GetDesiredFishCaughtCount(rod, bar, allowLuckyDoubleFish: true);

                if (Game1.isFestival())
                {
                    Game1.CurrentEvent.perfectFishing();
                }

                rod.pullFishFromWater(bar.whichFish, bar.fishSize, bar.fishQuality, (int)bar.difficulty, bar.treasure,
                    true, bar.fromFishPond, bar.setFlagOnCatch, bar.bossFish, numCaught);

                Game1.exitActiveMenu();
                Game1.setRichPresence("location", Game1.currentLocation.Name);
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
                    rod.numberOfFishCaught = GetDesiredFishCaughtCount(rod, Game1.activeClickableMenu as BobberBar,
                        allowLuckyDoubleFish: false);
                }
            }

            if (Game1.activeClickableMenu is BobberBar bar && !_config.SkipMinigame)
            {
                if (_delayTick > 0)
                {
                    _delayTick--;
                }
                else if (_syncMinigameState)
                {
                    _prevBobberPosition = bar.bobberPosition;
                    _prevDistanceFromCatching = bar.distanceFromCatching;
                    _prevTreasureCatchLevel = bar.treasureCatchLevel;
                    _syncMinigameState = false;
                }
                else
                {
                    if (_config.FishEasyCaught)
                    {
                        bar.bobberPosition = bar.bobberBarPos + (bar.bobberBarHeight / 2f) - 25;
                    }

                    if (_config.TreasureEasyCaught)
                    {
                        bar.treasurePosition = bar.bobberBarPos + (bar.bobberBarHeight / 2f) - 25;
                    }

                    if (_prevBobberPosition != 0 && bar.bobberPosition != 0 &&
                        _prevBobberPosition != bar.bobberPosition)
                    {
                        if (_prevBobberPosition > bar.bobberPosition)
                        {
                            bar.bobberPosition = _prevBobberPosition - ((_prevBobberPosition - bar.bobberPosition) *
                                                                        _config.FishMovementSpeedMultiplier);
                        }
                        else
                        {
                            bar.bobberPosition = _prevBobberPosition + ((bar.bobberPosition - _prevBobberPosition) *
                                                                        _config.FishMovementSpeedMultiplier);
                        }
                    }

                    if (_prevDistanceFromCatching != 0 && bar.distanceFromCatching != 0 &&
                        _prevDistanceFromCatching != bar.distanceFromCatching)
                    {
                        // These multipliers adjust the per-frame progress change after the game has already
                        // calculated it; they do not replace the game's internal penalty modifier.
                        if (_prevDistanceFromCatching > bar.distanceFromCatching)
                        {
                            bar.distanceFromCatching = _prevDistanceFromCatching -
                                                       ((_prevDistanceFromCatching - bar.distanceFromCatching) *
                                                        _config.ProgressBarDecreaseMultiplier);
                        }
                        else
                        {
                            bar.distanceFromCatching = _prevDistanceFromCatching +
                                                       ((bar.distanceFromCatching - _prevDistanceFromCatching) *
                                                        _config.ProgressBarIncreaseMultiplier);
                        }

                        bar.distanceFromCatching = Math.Max(0f, Math.Min(1f, bar.distanceFromCatching));
                    }

                    if (_prevTreasureCatchLevel != 0 && bar.treasureCatchLevel != 0 &&
                        _prevTreasureCatchLevel != bar.treasureCatchLevel)
                    {
                        bar.treasureCatchLevel = _prevTreasureCatchLevel +
                                                 ((bar.treasureCatchLevel - _prevTreasureCatchLevel) *
                                                  _config.TreasureCatchSpeedMultiplier);
                    }
                }

                _prevBobberPosition = bar.bobberPosition;
                _prevDistanceFromCatching = bar.distanceFromCatching;
                _prevTreasureCatchLevel = bar.treasureCatchLevel;
            }
        }

        private void ApplyTreasureState(FishingRod rod, BobberBar bar)
        {
            var hadTreasureBeforeMod = bar.treasure;
            if (_config.TreasureAlwaysBeFound)
            {
                bar.treasure = true;
            }

            if (_config.AlwaysGoldenTreasure && bar.treasure)
            {
                bar.goldenTreasure = true;
            }
            else if (!hadTreasureBeforeMod && bar.treasure)
            {
                // This mirrors FishingRod.startMinigameEndFunction in 1.6.15 for post-added treasure only.
                bar.goldenTreasure = ShouldForceGoldenTreasure();
            }

            rod.goldenTreasure = bar.goldenTreasure;
        }

        private int GetDesiredFishCaughtCount(FishingRod rod, BobberBar bar, bool allowLuckyDoubleFish)
        {
            if (rod.bossFish || (bar != null && bar.bossFish))
            {
                return 1;
            }

            // In 1.6, Challenge Bait can override the usual double-fish result with its remaining fish count.
            if (bar != null && bar.challengeBaitFishes > 0)
            {
                return bar.challengeBaitFishes;
            }

            if (_config.CaughtDoubleFishOnAnyBait || rod.GetBait()?.QualifiedItemId == WildBaitQualifiedItemId)
            {
                if (_config.AlwaysCaughtDoubleFish ||
                    allowLuckyDoubleFish && Game1.random.NextDouble() < (0.25 + (Game1.player.DailyLuck / 2.0)))
                {
                    return 2;
                }
            }

            return 1;
        }

        private bool ShouldForceGoldenTreasure()
        {
            if (Game1.player.stats.Get(StatKeys.Mastery(1)) == 0)
            {
                return false;
            }

            return Game1.random.NextDouble() < 0.25 + Game1.player.team.AverageDailyLuck();
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
