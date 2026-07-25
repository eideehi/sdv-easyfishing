using System;
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Constants;
using StardewValley.Enchantments;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace EideeEasyFishing
{
    internal class ModEntry : Mod
    {
        private const string WildBaitQualifiedItemId = "(O)774";
        private const string SonarBobberQualifiedItemId = "(O)SonarBobber";
        private const string MagicBaitQualifiedItemId = "(O)908";
        private const string CuriosityLureQualifiedItemId = "(O)856";
        private const string ChallengeBaitQualifiedItemId = "(O)ChallengeBait";
        private const string DeluxeBaitQualifiedItemId = "(O)DeluxeBait";
        private const int AutoAdvanceCatchRetryTicks = 30;
        private const int AutoAdvanceCatchMaxAttempts = 5;

        private ModConfig _config;
        private ModConfigKeys _keys;
        private string _stopAutoRecastRaw;
        private SButton _stopAutoRecastButton = SButton.None;

        private int _delayTick;
        private bool _syncMinigameState;
        private float _prevBobberPosition;
        private float _prevDistanceFromCatching;
        private float _prevTreasureCatchLevel;

        private FishingRod _swappedRod;
        private bool _baitSwapped;
        private StardewValley.Object _originalBait;
        private int _swappedTackleSlot = -1;
        private StardewValley.Object _originalTackle;

        private ItemGrabMenu _autoCollectDeferredMenu;

        private FishingRod _autoRecastRod;
        private bool _autoRecastStopPending;
        private bool _autoRecastDispatched;
        private bool _autoRecastForcePower;
        private bool _prevRodInUse;
        private int _autoAdvanceCatchAttempts;
        private int _autoAdvanceCatchCooldownTicks;

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);

            _config = Helper.ReadConfig<ModConfig>();
            _keys = _config.Controls.ParseControls();

            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.RenderedHud += OnRenderedHud;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Player.Warped += OnWarped;
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
                name: I18n.Config_Enabled_Name,
                tooltip: I18n.Config_Enabled_Description,
                getValue: () => _config.Enabled,
                setValue: value => _config.Enabled = value);

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
                name: I18n.Config_AlwaysMagicBait_Name,
                tooltip: I18n.Config_AlwaysMagicBait_Description,
                getValue: () => _config.AlwaysMagicBait,
                setValue: value => _config.AlwaysMagicBait = value);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_AlwaysCuriosityLure_Name,
                tooltip: I18n.Config_AlwaysCuriosityLure_Description,
                getValue: () => _config.AlwaysCuriosityLure,
                setValue: value => _config.AlwaysCuriosityLure = value);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_AlwaysMaxCastPower_Name,
                tooltip: I18n.Config_AlwaysMaxCastPower_Description,
                getValue: () => _config.AlwaysMaxCastPower,
                setValue: value => _config.AlwaysMaxCastPower = value);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_AutoRecast_Name,
                tooltip: I18n.Config_AutoRecast_Description,
                getValue: () => _config.AutoRecast,
                setValue: value => _config.AutoRecast = value);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: I18n.Config_StopAutoRecastAtTime_Name,
                tooltip: I18n.Config_StopAutoRecastAtTime_Description,
                getValue: () => StopTimeToStep(_config.StopAutoRecastAtTime),
                setValue: value => _config.StopAutoRecastAtTime = StopTimeStepToTime((int)value),
                min: 0,
                max: 121,
                interval: 1,
                formatValue: value => value <= 0
                    ? I18n.Config_StopAutoRecastAtTime_Disabled()
                    : Game1.getTimeOfDayString(StopTimeStepToTime((int)value)));

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_AutoCollectFishingTreasure_Name,
                tooltip: I18n.Config_AutoCollectFishingTreasure_Description,
                getValue: () => _config.AutoCollectFishingTreasure,
                setValue: value => _config.AutoCollectFishingTreasure = value);

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

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_AlwaysMaxFishQuality_Name,
                tooltip: I18n.Config_AlwaysMaxFishQuality_Description,
                getValue: () => _config.AlwaysMaxFishQuality,
                setValue: value => _config.AlwaysMaxFishQuality = value);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_AlwaysMaxFishSize_Name,
                tooltip: I18n.Config_AlwaysMaxFishSize_Description,
                getValue: () => _config.AlwaysMaxFishSize,
                setValue: value => _config.AlwaysMaxFishSize = value);

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
            if (!_config.Enabled) return;

            // Capture original bait id before RestoreSwap clears it, so we can replay the
            // BobberBar-constructor effects the synthetic Magic Bait swap suppressed.
            var swappedAwayBaitId = (_baitSwapped && _swappedRod == rod) ? _originalBait?.QualifiedItemId : null;

            // BobberBar opening means DoFunction's getFish has already run and the fish is locked.
            // Restore now so doneFishing's consumption path sees the player's original bait/tackle.
            RestoreSwap();

            // Replay constructor-time bait effects that the swap masked. Wild Bait reads from
            // rod.GetBait() at fade-out (post-restore) so it doesn't need replay; Specific Bait
            // targeting is a documented accepted trade-off when AlwaysMagicBait is enabled.
            if (swappedAwayBaitId == ChallengeBaitQualifiedItemId)
            {
                bar.challengeBaitFishes = 3;
            }
            else if (swappedAwayBaitId == DeluxeBaitQualifiedItemId)
            {
                bar.bobberBarHeight += 12;
                // BobberBar constructor sets bobberBarPos = 568 - bobberBarHeight; keep them coupled.
                bar.bobberBarPos = 568 - bar.bobberBarHeight;
            }

            if (_config.AlwaysSonarBobber && bar.bobbers != null &&
                !bar.bobbers.Contains(SonarBobberQualifiedItemId))
            {
                bar.bobbers.Add(SonarBobberQualifiedItemId);
            }

            if (_config.AlwaysMaxFishQuality)
            {
                bar.fishQuality = 4;
            }

            if (_config.AlwaysMaxFishSize)
            {
                // BobberBar constructor adds +1 to the rolled size, so a perfect maxFishSize roll yields maxFishSize+1.
                bar.fishSize = bar.maxFishSize + 1;
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

        private void OnRenderedHud(object sender, RenderedHudEventArgs args)
        {
            if (_autoRecastRod == null || _autoRecastStopPending ||
                Game1.activeClickableMenu != null || Game1.eventUp)
            {
                return;
            }

            var stopButton = GetStopAutoRecastButton();
            var hasStopKey = stopButton != SButton.None;
            var hasStopTime = IsValidStopTime(_config.StopAutoRecastAtTime);
            string text;
            if (hasStopKey && hasStopTime)
            {
                text = I18n.Message_AutoRecast_Hud_KeyTime(
                    stopButton.ToString(),
                    Game1.getTimeOfDayString(_config.StopAutoRecastAtTime));
            }
            else if (hasStopKey)
            {
                text = I18n.Message_AutoRecast_Hud_Key(stopButton.ToString());
            }
            else if (hasStopTime)
            {
                text = I18n.Message_AutoRecast_Hud_Time(
                    Game1.getTimeOfDayString(_config.StopAutoRecastAtTime));
            }
            else
            {
                text = I18n.Message_AutoRecast_Hud_None();
            }

            var textSize = Game1.smallFont.MeasureString(text);
            var position = new Vector2((Game1.uiViewport.Width - textSize.X) / 2f, 16f);

            Utility.drawTextWithShadow(args.SpriteBatch, text, Game1.smallFont, position, Game1.textColor);
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs args)
        {
            if (!_config.Enabled)
            {
                // Defensive: undo any in-flight swap if the mod was disabled outside the hotkey path.
                RestoreSwap();
                ClearAutoRecast();
                return;
            }

            UpdateSwapState();
            UpdateAutoRecast();
            UpdateFishingTreasureAutoCollect();

            var player = Game1.player;
            if (player is not { IsLocalPlayer: true }) return;

            if (player.CurrentTool is FishingRod rod)
            {
                if ((_config.AlwaysMaxCastPower || _autoRecastForcePower) && rod.isTimingCast)
                {
                    rod.castingTimerSpeed = 0;
                    rod.castingPower = 1;
                }
                else if (rod.castingTimerSpeed == 0f)
                {
                    // Restore the vanilla charge speed we zero out above. castingTimerSpeed is
                    // [XmlIgnore] (default 0.001f) and never reset per-cast, so leaving it at 0
                    // would pin the cast bar at 0 once the option is off, until the rod is rebuilt.
                    rod.castingTimerSpeed = 0.001f;
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
                if (_config.AlwaysMaxFishQuality && bar.fishQuality != 4)
                {
                    bar.fishQuality = 4;
                }

                if (_config.AlwaysMaxFishSize)
                {
                    var targetSize = bar.maxFishSize + 1;
                    if (bar.fishSize != targetSize)
                    {
                        bar.fishSize = targetSize;
                    }
                }

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

        private void UpdateAutoRecast()
        {
            if (!Context.IsWorldReady)
            {
                ClearAutoRecast();
                UpdateRodUseState(null, out _);
                return;
            }

            var player = Game1.player;
            var currentRod = player is { IsLocalPlayer: true }
                ? player.CurrentTool as FishingRod
                : null;
            var wasRodInUse = UpdateRodUseState(currentRod, out var rodInUse);

            if (_autoRecastRod == null)
            {
                if (currentRod != null && !wasRodInUse && rodInUse && IsAutoRecastEligible())
                {
                    _autoRecastRod = currentRod;
                }
                return;
            }

            var autoRecastRod = _autoRecastRod;
            if (player is not { IsLocalPlayer: true } ||
                currentRod != autoRecastRod || !IsAutoRecastEligible())
            {
                ClearAutoRecast();
                return;
            }

            if (_autoRecastDispatched && rodInUse)
            {
                _autoRecastDispatched = false;
            }

            if (_autoRecastForcePower && !_autoRecastDispatched && !autoRecastRod.isTimingCast)
            {
                _autoRecastForcePower = false;
            }

            if (!autoRecastRod.fishCaught)
            {
                _autoAdvanceCatchAttempts = 0;
                _autoAdvanceCatchCooldownTicks = 0;
            }

            var readyToRecast = !_autoRecastDispatched && !rodInUse &&
                                !autoRecastRod.pullingOutOfWater && !autoRecastRod.showingTreasure &&
                                !autoRecastRod.castedButBobberStillInAir && !autoRecastRod.hit &&
                                Context.IsPlayerFree && !player.UsingTool && player.freezePause <= 0 &&
                                Game1.activeClickableMenu == null;

            if (readyToRecast && WouldNextCastExhaust(player, autoRecastRod))
            {
                ShowAutoRecastNotice(I18n.Message_AutoRecast_Stopped_Stamina());
                Game1.activeClickableMenu = new GameMenu();
                ClearAutoRecast();
                return;
            }

            if (IsValidStopTime(_config.StopAutoRecastAtTime) &&
                Game1.timeOfDay >= _config.StopAutoRecastAtTime &&
                !_autoRecastStopPending)
            {
                _autoRecastStopPending = true;
                ShowAutoRecastNotice(I18n.Message_AutoRecast_Stopped_Time());
            }

            // Advance the catch even while a stop is pending: the held-up fish keeps the rod busy, so
            // without this a stop that lands mid-catch would wait for a click that an unattended
            // player never makes, and the game menu that protects them from the 2:00 AM pass-out
            // would never open. Advancing cannot start a cast, so a pending stop still wins.
            if (autoRecastRod.fishCaught && Game1.activeClickableMenu == null &&
                !Game1.eventUp && player.freezePause <= 0)
            {
                if (_autoAdvanceCatchCooldownTicks > 0)
                {
                    _autoAdvanceCatchCooldownTicks--;
                    return;
                }

                autoRecastRod.doneHoldingFish(player);
                _autoAdvanceCatchAttempts++;

                if (autoRecastRod.fishCaught && _autoAdvanceCatchAttempts >= AutoAdvanceCatchMaxAttempts)
                {
                    ShowAutoRecastNotice(I18n.Message_AutoRecast_Stopped_Manual());
                    ClearAutoRecast();
                    return;
                }

                // Secret-note catches can return without changing fishCaught, so retry slowly and give up.
                _autoAdvanceCatchCooldownTicks = AutoAdvanceCatchRetryTicks;
                return;
            }

            if (_autoRecastStopPending)
            {
                if (readyToRecast && Game1.activeClickableMenu == null)
                {
                    Game1.activeClickableMenu = new GameMenu();
                    ClearAutoRecast();
                }
                return;
            }

            if (!readyToRecast) return;

            if (!IsFullPowerCastFishable(player))
            {
                // A full-power recast from here would land off fishable water. Stop rather than wait:
                // repositioning needs a movement input, and movement is itself a cancel input, so an
                // armed loop that waits here can never recover and the overlay would lie indefinitely.
                ShowAutoRecastNotice(I18n.Message_AutoRecast_Stopped_NoWater());
                ClearAutoRecast();
                return;
            }

            player.BeginUsingTool();
            _autoRecastDispatched = true;
            _autoRecastForcePower = true;
        }

        private bool UpdateRodUseState(FishingRod rod, out bool rodInUse)
        {
            var wasRodInUse = _prevRodInUse;
            rodInUse = rod?.inUse() ?? false;
            _prevRodInUse = rodInUse;
            return wasRodInUse;
        }

        private bool IsAutoRecastEligible()
        {
            return _config.Enabled && _config.AutoRecast;
        }

        private SButton GetStopAutoRecastButton()
        {
            var raw = _config.Controls.StopAutoRecast;
            if (!string.Equals(raw, _stopAutoRecastRaw, StringComparison.Ordinal))
            {
                _stopAutoRecastRaw = raw;
                _stopAutoRecastButton = _config.Controls.ParseStopAutoRecast();
            }
            return _stopAutoRecastButton;
        }

        private static bool IsValidStopTime(int time)
        {
            return time >= 600 && time <= 2600 && time % 10 == 0 && time % 100 <= 50;
        }

        private static int StopTimeStepToTime(int step)
        {
            if (step <= 0) return 0;

            var minutes = (step - 1) * 10;
            var time = 600 + (minutes / 60) * 100 + (minutes % 60);
            return Math.Min(time, 2600);
        }

        private static int StopTimeToStep(int time)
        {
            if (!IsValidStopTime(time)) return 0;

            return ((time / 100 - 6) * 60 + time % 100) / 10 + 1;
        }

        private static bool WouldNextCastExhaust(Farmer player, FishingRod rod)
        {
            // Only stop when the next cast would really cost stamina. FishingRod.DoFunction skips the
            // deduction entirely for an Efficient rod, and Farmer.Stamina ignores every decrease while
            // Statue of Blessings (Blessing of Waters) is active, so a live reading alone is not enough:
            // in both cases stamina stays put and a cost-based stop would strand the loop forever.
            if (rod.hasEnchantmentOfType<EfficientToolEnchantment>()) return false;
            if (player.hasBuff("statue_of_blessings_2")) return false;

            return player.Stamina - (8f - player.FishingLevel * 0.1f) <= 0f;
        }

        private static bool IsFullPowerCastFishable(Farmer player)
        {
            var addedDistance = player.FishingLevel >= 15 ? 4 :
                player.FishingLevel >= 8 ? 3 :
                player.FishingLevel >= 4 ? 2 :
                player.FishingLevel >= 1 ? 1 : 0;
            var standingPixel = player.StandingPixel;
            float bobberX;
            float bobberY;

            if (player.FacingDirection == 1 || player.FacingDirection == 3)
            {
                var distance = Math.Max(128f, 1f * (addedDistance + 4) * 64f);
                distance -= 8f;
                bobberX = standingPixel.X + (player.FacingDirection != 3 ? 1 : -1) * distance;
                bobberY = standingPixel.Y;
            }
            else
            {
                var distance = 0f - Math.Max(128f, 1f * (addedDistance + 3) * 64f);
                if (player.FacingDirection == 0) distance = 0f - distance;
                bobberX = standingPixel.X;
                bobberY = standingPixel.Y - distance;
            }

            var tileX = (int)(bobberX / 64f);
            var tileY = (int)(bobberY / 64f);
            return player.currentLocation.canFishHere() &&
                   player.currentLocation.isTileFishable(tileX, tileY);
        }

        private static void ShowAutoRecastNotice(string text)
        {
            Game1.addHUDMessage(new HUDMessage(text, HUDMessage.error_type)
            {
                noIcon = true,
                timeLeft = HUDMessage.defaultTime
            });
        }

        private void UpdateFishingTreasureAutoCollect()
        {
            if (!Context.IsWorldReady || Game1.player is not { IsLocalPlayer: true } ||
                Game1.activeClickableMenu is not ItemGrabMenu grab ||
                grab.context is not FishingRod)
            {
                // The fishing menu is gone, so a menu handed back earlier can be forgotten.
                _autoCollectDeferredMenu = null;
                return;
            }

            if (!_config.AutoCollectFishingTreasure) return;

            // Once a menu is handed back to the player it stays theirs until they close it. Game1's
            // exitActiveMenu just nulls activeClickableMenu without running cleanupBeforeExit or the
            // held-item exit behavior, so closing it under a picked-up item would destroy that item,
            // and it would also let an active auto-recast loop start its next cast too early.
            if (ReferenceEquals(_autoCollectDeferredMenu, grab) || grab.heldItem != null)
            {
                _autoCollectDeferredMenu = grab;
                return;
            }

            var inventory = grab.ItemsToGrabMenu.actualInventory;
            var hasRemainingItems = false;
            for (var index = 0; index < inventory.Count; index++)
            {
                var item = inventory[index];
                if (item == null) continue;

                if (Game1.player.addItemToInventory(item) == null)
                {
                    inventory[index] = null;
                }
                else
                {
                    hasRemainingItems = true;
                }
            }

            if (hasRemainingItems)
            {
                _autoCollectDeferredMenu = grab;
                return;
            }

            Game1.exitActiveMenu();
            _autoCollectDeferredMenu = null;
        }

        private void ClearAutoRecast()
        {
            _autoRecastRod = null;
            _autoRecastStopPending = false;
            _autoRecastDispatched = false;
            _autoRecastForcePower = false;
            _autoAdvanceCatchAttempts = 0;
            _autoAdvanceCatchCooldownTicks = 0;
            // Preserve the edge tracker so cleanup during a cast can't re-arm without a new manual use.
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

        private void UpdateSwapState()
        {
            if (_swappedRod != null)
            {
                var localPlayer = Game1.player;
                var stillCurrent = localPlayer != null && localPlayer.IsLocalPlayer &&
                                   localPlayer.CurrentTool == _swappedRod;
                // Do NOT restore on isNibbling: in 1.6.15 FishingRod.DoFunction calls getFish only
                // after the player hits, which happens several ticks after isNibbling becomes true.
                // Restoring here would remove the synthetic before getFish reads it.
                if (!stillCurrent || !_swappedRod.isFishing)
                {
                    RestoreSwap();
                }
            }

            if (Game1.player is not { IsLocalPlayer: true } player) return;
            if (player.CurrentTool is not FishingRod rod) return;
            if (!rod.isFishing || rod.isNibbling || rod.isReeling || rod.pullingOutOfWater || rod.hit) return;

            // Don't touch a different rod while we still hold a swap on the previous one.
            if (_swappedRod != null && _swappedRod != rod) return;

            TrySwapBait(rod);
            TrySwapTackle(rod);
        }

        private void TrySwapBait(FishingRod rod)
        {
            if (!_config.AlwaysMagicBait) return;
            // FishingRod.attachments is a NetObjectArray; mutating it in multiplayer can propagate
            // the synthetic to the host and persist on disconnect/remote save. Disable the swap
            // outside single-player until a host-coordinated restore protocol is in place.
            if (Context.IsMultiplayer) return;
            if (!rod.CanUseBait()) return;
            if (rod.attachments.Count <= 0) return;

            var current = rod.attachments[0];
            // Mirror the tackle swap: insert a temporary Magic Bait even when no bait is equipped.
            // The original (null included) is restored before any consumption, so nothing is gifted.
            if (current?.QualifiedItemId == MagicBaitQualifiedItemId) return;

            var substitute = ItemRegistry.Create(MagicBaitQualifiedItemId) as StardewValley.Object;
            if (substitute == null) return;

            _originalBait = current;
            rod.attachments[0] = substitute;
            _baitSwapped = true;
            _swappedRod = rod;
        }

        private void TrySwapTackle(FishingRod rod)
        {
            if (!_config.AlwaysCuriosityLure) return;
            // See TrySwapBait: disable the tackle swap in multiplayer for the same net-field reason.
            if (Context.IsMultiplayer) return;
            if (!rod.CanUseTackle()) return;
            if (_swappedTackleSlot >= 0) return;

            var attachments = rod.attachments;
            var slotCount = attachments.Count;
            if (slotCount < 2) return;

            // If any tackle slot already holds a Curiosity Lure, leave it alone.
            for (var i = 1; i < slotCount; i++)
            {
                if (attachments[i]?.QualifiedItemId == CuriosityLureQualifiedItemId) return;
            }

            // Prefer an empty tackle slot so the player's existing tackle effect is preserved.
            var slot = -1;
            for (var i = 1; i < slotCount; i++)
            {
                if (attachments[i] == null)
                {
                    slot = i;
                    break;
                }
            }
            if (slot < 0) slot = 1;

            var substitute = ItemRegistry.Create(CuriosityLureQualifiedItemId) as StardewValley.Object;
            if (substitute == null) return;

            _originalTackle = attachments[slot];
            attachments[slot] = substitute;
            _swappedTackleSlot = slot;
            _swappedRod = rod;
        }

        private void RestoreSwap()
        {
            var rod = _swappedRod;
            if (rod == null)
            {
                ClearSwapState();
                return;
            }

            if (_baitSwapped && rod.attachments.Count > 0)
            {
                rod.attachments[0] = _originalBait;
            }

            if (_swappedTackleSlot >= 0 && rod.attachments.Count > _swappedTackleSlot)
            {
                rod.attachments[_swappedTackleSlot] = _originalTackle;
            }

            ClearSwapState();
        }

        private void ClearSwapState()
        {
            _swappedRod = null;
            _baitSwapped = false;
            _originalBait = null;
            _swappedTackleSlot = -1;
            _originalTackle = null;
        }

        private void OnSaving(object sender, SavingEventArgs e) => RestoreSwap();
        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            RestoreSwap();
            ClearAutoRecast();
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer)
            {
                RestoreSwap();
                ClearAutoRecast();
            }
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            // The rod object is no longer attached to a live save; drop references without writing.
            ClearSwapState();
            ClearAutoRecast();
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
            else if (args.Button == _keys.ToggleMod)
            {
                _config.Enabled = !_config.Enabled;
                Helper.WriteConfig(_config);

                // Disabling mid-cast must not leave a synthetic bait/tackle in the net-synced rod.
                if (!_config.Enabled)
                {
                    RestoreSwap();
                    ClearAutoRecast();
                }

                var text = _config.Enabled ? I18n.Message_Mod_Enabled() : I18n.Message_Mod_Disabled();
                Game1.addHUDMessage(new HUDMessage(text, HUDMessage.error_type)
                {
                    noIcon = true,
                    timeLeft = HUDMessage.defaultTime
                });
            }
            else if (args.Button != SButton.None && args.Button == GetStopAutoRecastButton())
            {
                if (_autoRecastRod == null) return;

                ClearAutoRecast();
                ShowAutoRecastNotice(I18n.Message_AutoRecast_Stopped_Manual());
            }

            if (_autoRecastRod != null && IsAutoRecastCancelInput(args.Button))
            {
                ClearAutoRecast();
                ShowAutoRecastNotice(I18n.Message_AutoRecast_Stopped_Manual());
            }
        }

        private static bool IsAutoRecastCancelInput(SButton button)
        {
            if (button is SButton.ControllerStart or SButton.ControllerB or
                SButton.DPadUp or SButton.DPadDown or
                SButton.DPadLeft or SButton.DPadRight)
            {
                return true;
            }

            if (!button.TryGetKeyboard(out var key)) return false;

            return Game1.options.doesInputListContain(Game1.options.menuButton, key) ||
                   Game1.options.doesInputListContain(Game1.options.moveUpButton, key) ||
                   Game1.options.doesInputListContain(Game1.options.moveDownButton, key) ||
                   Game1.options.doesInputListContain(Game1.options.moveLeftButton, key) ||
                   Game1.options.doesInputListContain(Game1.options.moveRightButton, key);
        }
    }
}
