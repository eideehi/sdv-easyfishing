using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Tools;

namespace EideeEasyFishing
{
    // Fish ponds and unknown location overrides may mutate persistent state. Island overrides
    // are handled separately, without awarding walnuts or triggering multiplayer events.
    internal sealed class FishChancePanel
    {
        private const int SamplesPerTick = 50;
        private const int MaxRows = 6;
        private const int MaxVisibleCatchRows = 8;
        private const int MaxPanelWidth = 320;
        private const string Junk = "<junk>";
        private readonly ITranslationHelper _translations;
        private readonly IMonitor _monitor;
        private readonly Func<ModConfig> _config;
        private readonly Dictionary<string, int> _counts = new();
        private readonly Dictionary<string, int> _displayCounts = new();
        private readonly HashSet<string> _legendaryIds = new();
        private readonly HashSet<string> _displayLegendaryIds = new();
        private readonly Random _sampleRandom = new();
        private FishingRod _rod;
        private GameLocation _location;
        private Vector2 _landingPosition;
        private string _signature;
        private int _samples;
        private int _eligibleSamples;
        private int _displaySamples;
        private int _target;
        private int _panelWidth;
        private int _scrollOffset;
        private int _maxScroll;
        private Rectangle _panelBounds;
        private bool _unavailable;
        private bool _noOrdinaryCatch;
        private bool _hostWalnutDrop;
        private bool _pondPreview;
        private bool _pondEmpty;
        private int _pondFishCount;

        public FishChancePanel(ITranslationHelper translations, IMonitor monitor, Func<ModConfig> config)
        {
            _translations = translations;
            _monitor = monitor;
            _config = config;
        }

        public void Reset()
        {
            _rod = null;
            _location = null;
            _signature = null;
            _counts.Clear();
            _displayCounts.Clear();
            _legendaryIds.Clear();
            _displayLegendaryIds.Clear();
            _samples = 0;
            _eligibleSamples = 0;
            _displaySamples = 0;
            _panelWidth = 0;
            _scrollOffset = 0;
            _maxScroll = 0;
            _panelBounds = Rectangle.Empty;
            _unavailable = false;
            _noOrdinaryCatch = false;
            _hostWalnutDrop = false;
            _pondPreview = false;
            _pondEmpty = false;
            _pondFishCount = 0;
        }

        public void Update()
        {
            var config = _config();
            if (!config.Enabled || !config.ShowFishChances || !Context.IsWorldReady || Game1.eventUp ||
                Game1.player?.CurrentTool is not FishingRod rod || !rod.isFishing || rod.isReeling ||
                rod.pullingOutOfWater || Game1.activeClickableMenu != null || Game1.player.currentLocation == null)
            {
                Reset();
                return;
            }

            var player = Game1.player;
            var location = player.currentLocation;
            if (_rod != rod || _location != location)
            {
                Reset();
                _rod = rod;
                _location = location;
                // While waiting, vanilla animates bobber.Y. Lock the observed landing position
                // for this cast so bobbing across a tile edge cannot repeatedly restart sampling.
                _landingPosition = rod.bobber.Value;
            }
            var tile = _landingPosition / 64f;
            // Read the pond's net fields only; FishPond.CatchFish removes a fish and must never
            // be called by a preview. Keep the same fractional tile check as GameLocation.getFish.
            var pond = location.buildings.OfType<FishPond>().FirstOrDefault(p => p.isTileFishable(tile));
            int target = Math.Clamp(config.FishChanceSamples, 100, 2000);
            var bait = rod.GetBait();
            var splash = location.fishSplashPoint.Value;
            var bubble = new Rectangle(splash.X * 64, splash.Y * 64, 64, 64);
            var bobber = new Rectangle((int)_landingPosition.X - 80, (int)_landingPosition.Y - 80, 64, 64);
            bool inBubbles = splash != Point.Zero && bubble.Intersects(bobber);
            bool inFrenzy = !string.IsNullOrEmpty(location.fishFrenzyFish.Value) &&
                            Vector2.Distance(tile, Utility.PointToVector2(splash)) <= 2f;
            var islandOutcome = GetIslandOutcome(location, tile, config.ShowIslandWalnutChance);
            // Time, location, equipment and splash changes can all change the selection rules.
            string signature = $"{(int)tile.X}:{(int)tile.Y}:{rod.clearWaterDistance}:{Game1.timeOfDay}:" +
                $"{player.TilePoint}:{location.IsRainingHere()}:{rod.QualifiedItemId}:" +
                $"{player.FishingLevel}:{player.LuckLevel}:{player.DailyLuck}:{bait?.QualifiedItemId}:" +
                $"{bait?.preservedParentSheetIndex.Value}:{rod.HasMagicBait()}:{rod.HasCuriosityLure()}:" +
                $"{splash}:{location.fishFrenzyFish.Value}:{inBubbles}:{inFrenzy}:{islandOutcome}:" +
                $"{config.ShowIslandWalnutChance}:{target}:{pond != null}:{pond?.FishCount}:{pond?.fishType.Value}";
            if (_signature != signature)
            {
                _signature = signature;
                _counts.Clear();
                _legendaryIds.Clear();
                _samples = 0;
                _eligibleSamples = 0;
                _target = target;
                _unavailable = (pond != null ? !CanPreviewPond(location) : !CanSample(location, tile)) ||
                               islandOutcome == IslandCatchOutcome.Unavailable;
                _noOrdinaryCatch = islandOutcome == IslandCatchOutcome.NoOrdinaryCatch;
                _hostWalnutDrop = !_unavailable && islandOutcome == IslandCatchOutcome.HostWalnutDrop;
                _pondPreview = !_unavailable && pond != null && islandOutcome == IslandCatchOutcome.Normal;
                _pondFishCount = _pondPreview ? pond.FishCount : 0;
                _pondEmpty = _pondPreview && _pondFishCount <= 0;
                if (_unavailable || _noOrdinaryCatch)
                {
                    _displayCounts.Clear();
                    _displayLegendaryIds.Clear();
                    _displaySamples = 0;
                }
                else if (_hostWalnutDrop)
                {
                    // In multiplayer the game returns null (the rod catches trash) and requests
                    // a separate world drop. Preview both without firing a network event.
                    _displayCounts.Clear();
                    _displayLegendaryIds.Clear();
                    _displayCounts[Junk] = 1;
                    _displaySamples = 1;
                    _samples = _target;
                }
                else if (_pondPreview)
                {
                    _displayCounts.Clear();
                    _displayLegendaryIds.Clear();
                    _displaySamples = 0;
                    _samples = _target;
                    if (!_pondEmpty)
                    {
                        var fishId = pond.fishType.Value;
                        var data = string.IsNullOrWhiteSpace(fishId)
                            ? null : ItemRegistry.GetDataOrErrorItem(ItemRegistry.QualifyItemId(fishId));
                        if (data == null || data.IsErrorItem)
                            _unavailable = true;
                        else
                        {
                            _displayCounts[data.QualifiedItemId] = 1;
                            _displaySamples = 1;
                        }
                    }
                }
            }
            if (_unavailable || _noOrdinaryCatch || _samples >= _target) return;

            // All calls are synchronous and on the game thread. Restore the original RNG even
            // if selection fails, so this display doesn't consume the game's random sequence.
            var originalRandom = Game1.random;
            try
            {
                Game1.random = _sampleRandom;
                int depth = rod.clearWaterDistance + (inBubbles ? 1 : 0);
                double potency = (bait != null ? bait.Price / 10f : 0f) + (inBubbles ? 0.4 : 0.0);
                int limit = Math.Min(_target, _samples + SamplesPerTick);
                while (_samples < limit)
                {
                    Item item;
                    if (location is IslandLocation)
                    {
                        item = islandOutcome switch
                        {
                            IslandCatchOutcome.Walnut => ItemRegistry.Create("(O)73"),
                            IslandCatchOutcome.Trash => null,
                            _ => SampleIslandFish(location, tile, depth, player, inFrenzy)
                        };
                    }
                    else
                    {
                        item = location.getFish(0f, bait?.QualifiedItemId, depth, player, potency, tile);
                    }
                    _samples++;
                    // A custom item query may itself return a walnut. Exclude it from both the
                    // rows and their denominator when the player chose ordinary catches only.
                    if (location is IslandLocation && !config.ShowIslandWalnutChance &&
                        item?.QualifiedItemId == "(O)73")
                        continue;
                    var key = item == null || item.Category == -20 ? Junk : item.QualifiedItemId;
                    if (key != Junk && item != null &&
                        (item.HasContextTag("fish_legendary") ||
                         (item.TryGetTempData<bool>("IsBossFish", out var isBossFish) && isBossFish)))
                        _legendaryIds.Add(key);
                    _counts.TryGetValue(key, out int count);
                    _counts[key] = count + 1;
                    _eligibleSamples++;
                }
                // Partial estimates constantly reorder rows and resize the panel. Publish the
                // completed batch at once; retain the previous result while a refresh runs.
                if (_samples >= _target)
                {
                    _displayCounts.Clear();
                    _displayLegendaryIds.Clear();
                    foreach (var pair in _counts)
                        _displayCounts.Add(pair.Key, pair.Value);
                    _displayLegendaryIds.UnionWith(_legendaryIds);
                    _displaySamples = _eligibleSamples;
                    _noOrdinaryCatch = _eligibleSamples == 0;
                }
            }
            catch (Exception ex)
            {
                _unavailable = true;
                _counts.Clear();
                _monitor.Log($"Fish chance sampling failed: {ex}", LogLevel.Warn);
            }
            finally
            {
                Game1.random = originalRandom;
            }
        }

        private static IslandCatchOutcome GetIslandOutcome(GameLocation location, Vector2 tile, bool includeWalnuts)
        {
            if (location is not IslandLocation) return IslandCatchOutcome.Normal;

            bool isPool = location is IslandSouthEast && (int)tile.X >= 18 && (int)tile.X <= 20 &&
                          (int)tile.Y >= 20 && (int)tile.Y <= 22;
            if (!includeWalnuts)
                return IslandCatchPreview.GetOutcomeWithoutWalnuts(isPool);
            bool poolCollected = location is IslandSouthEast southeast && southeast.fishedWalnut.Value;
            Game1.player.team.limitedNutDrops.TryGetValue("IslandFishing", out int collected);
            double roll = Utility.CreateRandom(Game1.stats.DaysPlayed, Game1.stats.TimesFished,
                Game1.uniqueIDForThisGame).NextDouble();
            return IslandCatchPreview.GetOutcome(isPool, poolCollected, Game1.IsMultiplayer,
                Game1.IsMasterGame, collected, roll);
        }

        private static Item SampleIslandFish(GameLocation location, Vector2 tile, int depth, Farmer player,
            bool inFrenzy)
        {
            // This is the base GameLocation selection path. Calling the virtual getFish here would
            // run IslandLocation's walnut award/request or IslandSouthEast's one-time event.
            if (inFrenzy)
                return ItemRegistry.Create(location.fishFrenzyFish.Value);
            return GameLocation.GetFishFromLocationData(location.Name, tile, depth, player,
                player.fishCaught.Length == 0, isInherited: false, location) ?? ItemRegistry.Create("(O)168");
        }

        private static bool CanSample(GameLocation location, Vector2 tile)
        {
            if (location.buildings.Any(b => b is FishPond pond && pond.isTileFishable(tile)))
                return false;
            var method = location.GetType().GetMethod(nameof(GameLocation.getFish));
            var owner = method?.DeclaringType;
            if (owner != typeof(GameLocation) && owner != typeof(Farm) && owner != typeof(MineShaft) &&
                owner != typeof(IslandLocation) && owner != typeof(IslandSouthEast))
                return false;

            // Farm maps may redirect selection to another location; don't invoke an unknown
            // override indirectly. The vanilla redirects point at ordinary base locations.
            if (owner == typeof(Farm))
            {
                var mapProperty = location.GetMapPropertySplitBySpaces("FarmFishLocationOverride");
                if (mapProperty.Length > 0)
                {
                    var destination = Game1.getLocationFromName(mapProperty[0]);
                    if (destination?.GetType().GetMethod(nameof(GameLocation.getFish))?.DeclaringType != typeof(GameLocation))
                        return false;
                }
            }
            return true;
        }

        private static bool CanPreviewPond(GameLocation location)
        {
            var owner = location.GetType().GetMethod(nameof(GameLocation.getFish))?.DeclaringType;
            if (owner != typeof(GameLocation) && owner != typeof(Farm) && owner != typeof(IslandLocation) &&
                owner != typeof(IslandSouthEast))
                return false;

            // A farm map can redirect getFish to another location before it checks the pond.
            // Without simulating that override, a guaranteed pond-fish percentage is not honest.
            return owner != typeof(Farm) || location.GetMapPropertySplitBySpaces("FarmFishLocationOverride").Length == 0;
        }

        public void Scroll(int delta, Vector2 cursor)
        {
            if (_rod == null || !_config().Enabled || !_config().ShowFishChances || !Context.IsWorldReady ||
                Game1.activeClickableMenu != null || Game1.eventUp || _maxScroll <= 0 || delta == 0 ||
                !_panelBounds.Contains((int)cursor.X, (int)cursor.Y))
                return;

            int steps = Math.Max(1, Math.Abs(delta) / 120);
            _scrollOffset = Math.Clamp(_scrollOffset + (delta < 0 ? steps : -steps), 0, _maxScroll);
        }

        public void Draw(SpriteBatch batch)
        {
            if (_rod == null || !_config().Enabled || !_config().ShowFishChances ||
                Game1.activeClickableMenu != null || Game1.eventUp || !Context.IsWorldReady)
            {
                _panelBounds = Rectangle.Empty;
                return;
            }

            var font = Game1.smallFont;
            int lineHeight = Math.Max(40, (int)Math.Ceiling(font.LineSpacing * 1f));
            Func<string, float> measure = text => font.MeasureString(text).X;
            int maxWidth = Math.Max(1, Math.Min(MaxPanelWidth, Game1.uiViewport.Width - 16));
            const int textInset = 12;
            const int fishTextInset = 50;
            const int rightPadding = 12;
            float maxFishTextWidth = Math.Max(1, maxWidth - fishTextInset - rightPadding);
            var lines = new List<(string Text, string ItemId, int Inset)>();
            string title = _translations.Get(_pondPreview ? "fish-chances.pond-title" : "fish-chances.title");
            int percent = _target > 0 ? Math.Clamp(100 * _samples / _target, 0, 100) : 0;
            string sampling = _translations.Get("fish-chances.sampling", new { percent });
            string titleText = _displaySamples > 0 && _samples < _target && !_unavailable
                ? $"{title} · {percent}%" : title;
            lines.Add((PanelText.FitWithSuffix(titleText, "", maxWidth - textInset - rightPadding, measure), null, textInset));
            if (_unavailable)
                lines.Add((PanelText.FitWithSuffix(_translations.Get("fish-chances.unavailable"), "",
                    maxWidth - textInset - rightPadding, measure), null, textInset));
            else if (_noOrdinaryCatch)
                lines.Add((PanelText.FitWithSuffix(_translations.Get("fish-chances.no-ordinary-catch"), "",
                    maxWidth - textInset - rightPadding, measure), null, textInset));
            else if (_pondEmpty)
                lines.Add((PanelText.FitWithSuffix(_translations.Get("fish-chances.pond-empty"), "",
                    maxWidth - textInset - rightPadding, measure), null, textInset));
            else if (_displaySamples == 0)
                lines.Add((PanelText.FitWithSuffix(sampling, "", maxWidth - textInset - rightPadding, measure), null, textInset));
            else
            {
                // Sort legendary fish first, but keep every sampled item accessible by scrolling.
                var sorted = _displayCounts.OrderByDescending(pair => _displayLegendaryIds.Contains(pair.Key))
                    .ThenByDescending(pair => pair.Value).ThenBy(pair => pair.Key).ToList();
                int extraRows = (_hostWalnutDrop ? 1 : 0) + (_pondPreview && !_pondEmpty ? 1 : 0);
                int rowsFit = Math.Max(1, (Game1.uiViewport.Height - 32) / lineHeight);
                int visible = Math.Max(1, Math.Min(MaxVisibleCatchRows, rowsFit - 1 - extraRows));
                if (sorted.Count > visible)
                    visible = Math.Max(1, Math.Min(MaxVisibleCatchRows, rowsFit - 2 - extraRows));
                _maxScroll = Math.Max(0, sorted.Count - visible);
                _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScroll);
                foreach (var pair in sorted.Skip(_scrollOffset).Take(visible))
                {
                    string id = pair.Key == Junk ? "(O)168" : pair.Key;
                    string name = pair.Key == Junk ? _translations.Get("fish-chances.junk") : ItemRegistry.GetDataOrErrorItem(id).DisplayName;
                    string marker = _displayLegendaryIds.Contains(pair.Key)
                        ? $" {_translations.Get("fish-chances.legendary")} " : "  ";
                    string suffix = $"{marker}{100.0 * pair.Value / _displaySamples:F1}%";
                    lines.Add((PanelText.FitWithSuffix(name, suffix, maxFishTextWidth, measure), id, fishTextInset));
                }
                if (_maxScroll > 0)
                {
                    string hint = _translations.Get("fish-chances.scroll-hint");
                    string suffix = $"  {_scrollOffset + 1}-{Math.Min(_scrollOffset + visible, sorted.Count)}/{sorted.Count}";
                    lines.Add((PanelText.FitWithSuffix(hint, suffix, maxWidth - textInset - rightPadding, measure), null, textInset));
                }
            }

            if (_unavailable || _noOrdinaryCatch || _pondEmpty || _displaySamples == 0)
            {
                _maxScroll = 0;
                _scrollOffset = 0;
            }

            if (_hostWalnutDrop)
                lines.Add((PanelText.FitWithSuffix(_translations.Get("fish-chances.host-walnut-drop"), "",
                    maxWidth - textInset - rightPadding, measure), null, textInset));
            if (_pondPreview && !_pondEmpty && !_unavailable)
                lines.Add((PanelText.FitWithSuffix(_translations.Get("fish-chances.pond-count", new { count = _pondFishCount }), "",
                    maxWidth - textInset - rightPadding, measure), null, textInset));

            string fullProgress = _translations.Get("fish-chances.sampling", new { percent = 100 });
            float reservedWidth = Math.Max(measure($"{title} · 100%"), measure(fullProgress)) + textInset + rightPadding;
            int requestedWidth = (int)Math.Ceiling(Math.Max(reservedWidth,
                lines.Max(line => measure(line.Text) + line.Inset + rightPadding)));
            // The width can grow to fit the completed results, but never oscillates during a cast.
            _panelWidth = Math.Min(maxWidth, Math.Max(_panelWidth, requestedWidth));
            int width = _panelWidth;
            int height = lines.Count * lineHeight + 16;
            int preferredY = (Game1.uiViewport.Height - ((MaxRows + 1) * lineHeight + 16)) / 2;
            int y = Math.Max(8, Math.Min(preferredY, Game1.uiViewport.Height - height - 8));
            _panelBounds = new Rectangle(8, y, width, height);
            IClickableMenu.drawTextureBox(batch, 8, y, width, height,
                Color.White * Math.Clamp(_config().FishChancePanelOpacity, 0.1f, 1f));
            for (int i = 0; i < lines.Count; i++)
            {
                var (text, itemId, inset) = lines[i];
                int rowY = y + 8 + i * lineHeight;
                if (itemId != null)
                {
                    var data = ItemRegistry.GetDataOrErrorItem(itemId);
                    var source = data.GetSourceRect();
                    float scale = Math.Min(32f / source.Width, 32f / source.Height);
                    int iconWidth = (int)(source.Width * scale), iconHeight = (int)(source.Height * scale);
                    batch.Draw(data.GetTexture(), new Rectangle(18 + (32 - iconWidth) / 2,
                        rowY + (lineHeight - iconHeight) / 2, iconWidth, iconHeight), source, Color.White);
                }
                Utility.drawTextWithShadow(batch, text, font,
                    new Vector2(8 + inset, rowY + (lineHeight - font.MeasureString(text).Y) / 2), Game1.textColor);
            }
        }
    }
}
