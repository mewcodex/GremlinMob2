using Godot;
using HarmonyLib;
using GremlinMob2;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

[ModInitializer(nameof(Initialize))]
public static class Probe
{
    private static bool _started;

    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    public static void Initialize()
    {
        if (!OS.GetUserDataDir().Contains("GremlinMob2Smoke-114", StringComparison.Ordinal))
            throw new InvalidOperationException("Native probe must run in an isolated profile.");
        var harmony = new Harmony("gremlin-mob-2.probe");
        if (OS.GetCmdlineUserArgs().Contains("gremlin-check-sync-assets"))
        {
            MegaCrit.Sts2.Core.Assets.PreloadManager.Enabled = false;
            // Debug room jumps can evict materials that are still attached to live card nodes.
            harmony.Patch(AccessTools.Method(typeof(MegaCrit.Sts2.Core.Assets.AssetCache), "DisposeAtEndOfFrame"),
                prefix: new HarmonyMethod(typeof(Probe), nameof(KeepProbeMaterials)));
        }
        harmony.Patch(
            AccessTools.Method(typeof(NMainMenu), nameof(NMainMenu._Ready)),
            postfix: new HarmonyMethod(typeof(Probe), nameof(Launch)));
    }

    private static bool KeepProbeMaterials(Resource? asset) => asset is not Material;

    public static void Launch(NMainMenu __instance)
    {
        if (_started) return;
        _started = true;
        _ = Run(__instance);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Console.WriteLine("GREMLIN_PASS " + message);
    }

    private static async Task WaitFor(SceneTree tree, Func<bool> condition)
    {
        for (int i = 0; i < 300 && !condition(); i++)
            await tree.ToSignal(tree.CreateTimer(.05), SceneTreeTimer.SignalName.Timeout);
        if (!condition()) throw new TimeoutException("Timed out waiting for a game action.");
    }

    private static async Task Run(NMainMenu mainMenu)
    {
        var tree = mainMenu.GetTree();
        bool skinTest = OS.GetCmdlineUserArgs().Contains("gremlin-check-skin");
        string rigPrefix = skinTest ? "FeminizationSpine_" : "OriginalSpine_";
        try
        {
            await tree.ToSignal(tree.CreateTimer(.6), SceneTreeTimer.SignalName.Timeout);
            if (OS.GetCmdlineUserArgs().Contains("gremlin-check-sync-assets"))
                MegaCrit.Sts2.Core.Assets.PreloadManager.Enabled = true;
            if (OS.GetCmdlineUserArgs().Contains("gremlin-export-card-art"))
            {
                await ExportSkinCardArt(tree);
                tree.Quit();
                return;
            }
            var character = ModelDb.Character<GremlinMobCharacter>();
            Check(ModelDb.AllCharacters.Count(value => value == character) == 1, "one registered character");
            Check(character.CardPool.AllCards.Count(card => card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare) == 72, "72 draftable cards registered in native reward pool");
            Check(character.AssetPathsCharacterSelect.All(path => ResourceLoader.Exists(path)), "character selection resources load");
            Check(ResourceLoader.Load<ShaderMaterial>(character.CharacterSelectTransitionPath)?.GetShaderParameter("transitionTex").As<Texture2D>() != null &&
                ResourceLoader.Load<PackedScene>(character.TrailPath) != null, "Downfall transition and card trail load with their dependencies");
            Check(character.AssetPaths.All(path => ResourceLoader.Exists(path)) &&
                character.ArmPointingTexture != null && character.ArmRockTexture != null &&
                character.ArmPaperTexture != null && character.ArmScissorsTexture != null,
                "native character asset paths and multiplayer gesture textures load");
            Check(character.PotionPool.AllPotions.Count() == 3 && character.RelicPool.AllRelics.Count() == 14, "three potions and fourteen source-exclusive relics registered");
            Check(!ModelDb.RelicPool<MegaCrit.Sts2.Core.Models.RelicPools.SharedRelicPool>().AllRelics.Any(relic => relic is MobRelic),
                "no Gremlin relic added to the shared pool");
            Check(character.RelicPool.AllRelics.All(relic => ResourceLoader.Exists(relic.PackedIconPath)) &&
                character.PotionPool.AllPotions.All(potion => ResourceLoader.Exists(potion.ImagePath)), "all relic and potion icons resolve");
            CheckEffectIcons(character);
            CheckRelicWidgets(mainMenu);
            using var parsed = System.Text.Json.JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://gremlin_mob_2/original/text.json"));
            string originalLanguage = LocManager.Instance.Language;
            var keywordText = AccessTools.MethodDelegate<Func<CardKeyword, string>>(
                AccessTools.Method("MegaCrit.Sts2.Core.Entities.Cards.CardKeywordExtensions:GetCardText"));
            foreach (string language in new[] { "eng", "zhs", "zht" })
            {
                LocManager.Instance.SetLanguage(language);
                bool chinese = language is "zhs" or "zht";
                string Visible(string text) => chinese ? text.Replace(" ", "") : text;
                var expected = parsed.RootElement.GetProperty(language);
                var legacyCharacter = expected.GetProperty("characters").GetProperty("Gremlin");
                Check(character.Title.GetFormattedText() == Visible(legacyCharacter.GetProperty("NAMES")[0].GetString()!) &&
                    new LocString("characters", character.CharacterSelectDesc).GetFormattedText() ==
                        Visible(legacyCharacter.GetProperty("TEXT")[0].GetString()!.Replace(" NL ", "\n")),
                    "original character name and select description " + language);
                foreach (var canonical in ModelDb.AllCards.OfType<MobCard>().Where(card => card.Rarity != CardRarity.Ancient))
                {
                    var source = expected.GetProperty("cards").GetProperty(canonical.GetType().Name);
                    var card = canonical.ToMutable();
                    if (card.TitleLocString.GetFormattedText() != Visible(source.GetProperty("NAME").GetString()!))
                        throw new InvalidOperationException("Card title differs from source: " + card.Id);
                    var baseDescription = card.GetDescriptionForPile(PileType.None);
                    foreach (var keyword in card.Keywords)
                        if (baseDescription.Split('\n').Count(line => line == keywordText(keyword)) != 1)
                            throw new InvalidOperationException($"Missing native keyword for {card.Id}: {keyword}, {baseDescription}");
                    if (baseDescription.Contains("!D!") || baseDescription.Contains("!M!") || baseDescription.Contains("{IfUpgraded:"))
                        throw new InvalidOperationException("Underlying format visibly leaked: " + card.Id);
                    if (!ResourceLoader.Exists(card.PortraitPath)) throw new InvalidOperationException("Missing portrait: " + card.Id.Entry);
                    if (card.Portrait.GetImage().DetectAlpha() != Image.AlphaMode.None)
                        throw new InvalidOperationException("Card portrait is still transparent: " + card.Id.Entry);
                    if (!card.IsUpgradable) continue;
                    CardCmd.Upgrade(card, CardPreviewStyle.None);
                    var upgradedDescription = card.GetDescriptionForPile(PileType.None);
                    foreach (var keyword in card.Keywords)
                        if (upgradedDescription.Split('\n').Count(line => line == keywordText(keyword)) != 1)
                            throw new InvalidOperationException($"Missing upgraded native keyword for {card.Id}: {keyword}, {upgradedDescription}");
                }
                var originalStrike = expected.GetProperty("cards").GetProperty(nameof(MobStrike)).GetProperty("DESCRIPTION").GetString()!;
                var strikeModel = ModelDb.Card<MobStrike>().ToMutable();
                string actualStrike = System.Text.RegularExpressions.Regex.Replace(strikeModel.GetDescriptionForPile(PileType.None), @"\[[^\]]+\]", "");
                string expectedStrike = Visible(originalStrike.Replace("!D!", "6").Trim());
                Check(actualStrike == expectedStrike, $"Strike description keeps original wording {language} (actual '{actualStrike}', expected '{expectedStrike}')");
                Check(true, "all 86 archived card names and descriptions render " + language);
                if (language == "zhs")
                {
                    string PlainCardText(CardModel card) => System.Text.RegularExpressions.Regex.Replace(
                        card.GetDescriptionForPile(PileType.None), @"\[[^\]]+\]", "");
                    var allHands = ModelDb.Card<MobAllHands>().ToMutable();
                    var roundDance = ModelDb.Card<MobRoundDance>().ToMutable();
                    for (int upgrade = 0; upgrade <= 1; upgrade++)
                    {
                        Check(PlainCardText(allHands) == $"依次触发每只存活地精的额外效果各{2 + upgrade}次。\n消耗。" &&
                            PlainCardText(roundDance) == $"造成{9 + 3 * upgrade}点伤害。\n所有地精造成额外效果。",
                            $"Ancient cards display the requested Simplified Chinese text at upgrade level {upgrade}");
                        if (upgrade == 1) break;
                        CardCmd.Upgrade(allHands, CardPreviewStyle.None);
                        CardCmd.Upgrade(roundDance, CardPreviewStyle.None);
                    }
                }
                string ward = ModelDb.Card<MobPaperWard>().GetDescriptionForPile(PileType.None);
                string wardKeyword = keywordText(CardKeyword.Exhaust);
                string block = language switch { "zhs" => "格挡", "zht" => "格擋", _ => "Block" };
                string swap = language switch { "zhs" => "切换", "zht" => "切換", _ => "Swap" };
                Check(ward.EndsWith(wardKeyword) && ward[..^wardKeyword.Length].Contains("[gold]" + block + "[/gold]") &&
                    !ward[..^wardKeyword.Length].Contains(wardKeyword.Split("[/gold]")[0]),
                    "Ward displays the native Exhaust line once and highlights Block " + language);
                Check(ModelDb.Card<MobPickFront>().GetDescriptionForPile(PileType.None).Contains("[gold]" + swap + "[/gold]") &&
                    ModelDb.Card<MobSpellDazzle>().GetDescriptionForPile(PileType.None).Contains("[gold]3[/gold]"),
                    "Swap and fixed charge threshold are highlighted " + language);
                string temporaryStrength = language switch { "zhs" => "临时力量", "zht" => "臨時力量", _ => "temporary Strength" };
                Check(ModelDb.Card<MobShortFuse>().GetDescriptionForPile(PileType.None).Contains("[gold]" + temporaryStrength + "[/gold]"),
                    "temporary Strength is highlighted without a first-game namespace " + language);
                string revel = ModelDb.Card<MobRevel>().GetDescriptionForPile(PileType.None);
                Check(revel.Contains("[img]res://images/packed/sprite_fonts/gremlin_mob_2_energy_icon.png[/img]") &&
                    (!chinese || !revel.Contains(' ')), "inline energy markup survives localized spacing " + language);
                foreach (var (card, prefix) in new (MobCard Card, string Prefix)[]
                {
                    (ModelDb.Card<MobPickFront>(), "gremlin_"), (ModelDb.Card<MobChargeJab>(), "gremlin_"),
                    (ModelDb.Card<MobEchoAttack>(), chinese ? "gremlin_2" : "expansioncontent_echo"), (ModelDb.Card<MobHideNeedles>(), "bronze_insert"),
                    (ModelDb.Card<MobJuggernaut>(), "expansioncontent_temp_hp")
                })
                    Check(card.HoverTips.OfType<HoverTip>().Any(tip => tip.Id.Contains("GREMLIN_MOB_KEYWORD_" + prefix) &&
                        !string.IsNullOrWhiteSpace(tip.Description)), "archived keyword tooltip " + language + " " + card.Id.Entry);
                var label = new LocString("card_selection", "GREMLIN_MOB_CHOOSE_FRONT");
                var table = LocManager.Instance.GetTable(label.LocTable);
                string rawLabel = label.GetRawText();
                const string synthetic = "示 例 {Text} NL [hint=two words] 提 示 [/hint][img width=24]res://a b.png[/img]";
                const string rendered = "示 例 技能\n[hint=two words] 提 示 [/hint][img width=24]res://a b.png[/img]";
                const string compact = "示例技能\n[hint=two words]提示[/hint][img width=24]res://a b.png[/img]";
                var converter = AccessTools.Method(typeof(MobCard).Assembly.GetType("GremlinMob2.LegacyLocalization"), "Syntax");
                var converted = (string)converter.Invoke(null, [synthetic])!;
                try
                {
                    table.MergeWith(new Dictionary<string, string> { [label.LocEntryKey] = converted });
                    label.Add("Text", "技能");
                    Check(converted.Contains("{Text}") && label.GetFormattedText() == (chinese ? compact : rendered),
                        "Gremlin spacing conversion preserves variables, newlines and tag data " + language);
                }
                finally { table.MergeWith(new Dictionary<string, string> { [label.LocEntryKey] = rawLabel }); }
            }
            LocManager.Instance.SetLanguage(originalLanguage);
            var picker = mainMenu.SubmenuStack.GetSubmenuType<NCharacterSelectScreen>();
            picker.InitializeSingleplayer();
            mainMenu.SubmenuStack.Push(picker);
            var button = Descendants(picker).OfType<NCharacterSelectButton>().First(node => node.Character == character);
            var shownPortrait = AccessTools.Field(typeof(NCharacterSelectButton), "_icon").GetValue(button) as TextureRect;
            button.GrabFocus();
            picker.SelectCharacter(button);
            NModalContainer.Instance?.Clear();
            if (skinTest)
            {
                var skin = Descendants(picker).OfType<HBoxContainer>().Single(node => node.Name == "GremlinSkinPicker");
                var choice = skin.GetNode<Label>("SkinChoice");
                var background = picker.GetNode("AnimatedBg").GetChildren().OfType<TextureRect>().Single();
                var originalArt = background.Texture;
                var right = skin.GetNode<TextureButton>("NextSkin");
                var left = skin.GetNode<TextureButton>("PreviousSkin");
                const string originalLabel = "原版";
                const string alternateLabel = "娘化皮肤 by anaertailin";
                if (choice.Text == alternateLabel) right.EmitSignal(BaseButton.SignalName.Pressed);
                Check(choice.Text == originalLabel, "original appearance uses the requested label");
                await tree.ToSignal(tree.CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
                NModalContainer.Instance?.Clear();
                var position = right.GetGlobalRect().GetCenter();
                foreach (bool pressed in new[] { true, false })
                    tree.Root.PushInput(new InputEventMouseButton
                    {
                        ButtonIndex = MouseButton.Left, Position = position, GlobalPosition = position, Pressed = pressed
                    }, true);
                await WaitFor(tree, () => choice.Text == alternateLabel);
                Check(background.Texture == originalArt && background.SelfModulate == Colors.White,
                    "source arrow selects Feminization with the existing character select artwork");
                left.EmitSignal(BaseButton.SignalName.Pressed);
                Check(choice.Text == originalLabel, "source arrow switches back to original");
                right.EmitSignal(BaseButton.SignalName.Pressed);
                picker.SelectCharacter(button);
                NModalContainer.Instance?.Clear();
                Check(picker.GetNode<Label>("GremlinSkinPicker/SkinChoice").Text == alternateLabel,
                    "reopening the character selection retains the selected appearance");
            }
            await tree.ToSignal(tree.CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
            tree.Root.GetTexture().GetImage().SavePng(Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "mob-select.png"));
            Check(!button.IsLocked, "native Gremlin button is unlocked");
            Check(character.CharacterSelectIcon.GetSize() == new Vector2(132, 195), "model loads cropped portrait");
            Check(shownPortrait?.Texture?.ResourcePath == character.CharacterSelectIcon.ResourcePath,
                "native character selection mounted cropped portrait");
            Check(Descendants(picker).OfType<TextureRect>().Any(rect => rect.Texture?.GetSize() == new Vector2(1920, 1200)
                && rect.Name.ToString().Contains(character.Id.Entry)), "native character selection uses original background");
            var icon = character.Icon;
            Check(icon is TextureRect { Texture: { } head } && head.GetImage().GetSize() == new Vector2I(88, 88) &&
                character.IconTexture.GetSize() == new Vector2(88, 88) && character.MapMarker.GetSize() == new Vector2(49, 64) &&
                character.ArmPointingTexture?.GetSize() == new Vector2(422, 1200), "HUD, map and multiplayer hands match the Downfall character assets");
            icon.Free();
            mainMenu.SubmenuStack.Pop();
            await CheckCardPresentation(tree, mainMenu, skinTest);
            var library = mainMenu.SubmenuStack.GetSubmenuType<NCardLibrary>();
            mainMenu.SubmenuStack.Push(library);
            var poolFilter = Descendants(library).OfType<NCardPoolFilter>().Single(node => node.Name == "GremlinMobPool");
            poolFilter.IsSelected = true;
            poolFilter.EmitSignal(NCardPoolFilter.SignalName.Toggled, poolFilter);
            var libraryGrid = Descendants(library).OfType<NCardLibraryGrid>().Single();
            var libraryCards = character.CardPool.AllCards.Where(card => card.ShouldShowInCardLibrary && card.Rarity != CardRarity.Ancient).ToHashSet();
            await WaitFor(tree, () => libraryCards.SetEquals(libraryGrid.VisibleCards));
            Check(poolFilter.Visible && libraryCards.Count > 0, $"native Compendium lists all {libraryCards.Count} library-visible squad cards under its own filter");
            var defendArt = ModelDb.Card<MobDefend>();
            var ancientArt = ModelDb.Card<MobAllHands>();
            var inspect = NGame.Instance!.GetInspectCardScreen();
            try
            {
                inspect.Open([defendArt, ancientArt], 0, viewAllUpgraded: true);
                var cardView = Descendants(inspect).OfType<NCard>().Single();
                var portrait = cardView.GetNode<TextureRect>("%Portrait");
                Check(defendArt.Portrait.GetWidth() < portrait.Texture.GetWidth() &&
                    portrait.Texture.ResourcePath.EndsWith("/cards/defend_p.res"),
                    "native inspect screen shows archived large Defend art while normal cards use the small portrait");
                AccessTools.Method(typeof(NInspectCardScreen), "OnRightButtonReleased").Invoke(inspect, []);
                Check(cardView.GetNode<TextureRect>("%AncientPortrait").Texture.ResourcePath == ancientArt.PortraitPath,
                    "native inspect screen falls back to the usual Ancient portrait when large art is missing");
            }
            finally { inspect.Close(); }
            var miscFilter = library.GetNode<NCardPoolFilter>("%MiscPool");
            miscFilter.IsSelected = true;
            miscFilter.EmitSignal(NCardPoolFilter.SignalName.Toggled, miscFilter);
            await WaitFor(tree, () => libraryGrid.VisibleCards.OfType<MobCard>().Count() == 10);
            Check(libraryGrid.VisibleCards.OfType<MobCard>().All(card => card.Pool.IsColorless && card.Rarity == CardRarity.Token &&
                !card.CanBeGeneratedInCombat && !card.CanBeGeneratedByModifiers), "native Compendium moves all ten generated Gremlin cards to colorless tokens");
            var ancientFilter = library.GetNode<NCardPoolFilter>("%AncientsPool");
            ancientFilter.IsSelected = true;
            ancientFilter.EmitSignal(NCardPoolFilter.SignalName.Toggled, ancientFilter);
            await WaitFor(tree, () => libraryGrid.VisibleCards.OfType<MobAncientCard>().Count() == 2);
            mainMenu.SubmenuStack.Pop();
            var nativeCost = ModelDb.Card<MobStrike>().EnergyIcon;
            var nativeInline = ResourceLoader.Load<Texture2D>("res://images/packed/sprite_fonts/gremlin_mob_2_energy_icon.png");
            Check(nativeCost.GetWidth() == 74 && nativeInline.GetWidth() == 24 && nativeInline.GetImage().GetUsedRect().Size.X >= 21, "cost and inline icons load their separate cropped source textures");
            Check(ModelDb.Card<MobRevel>().GetDescriptionForPile(PileType.None).Contains("gremlin_mob_2_energy_icon.png"),
                "original inline energy token uses the original Gremlin icon");
            var player = Player.CreateForNewRun(character, UnlockState.all, 1);
            MobRosterRelic bell = player.Relics.OfType<MobRotationBell>().Single();
            Check(player.Creature.MaxHp == 16 && player.Deck.Cards.Count == 10 && player.Gold == 99 && bell.Pool == character.RelicPool,
                "archived 10-card opening deck, 99 gold, 16 HP per member, and starter relic");
            var acts = ActModel.GetRandomList(new Rng(39, "gremlin-mob-smoke"), UnlockState.all, false).ToArray();
            await NGame.Instance!.StartNewSingleplayerRun(player, true, acts, [], "MOBSMOKE", GameMode.Standard, false);
            NModalContainer.Instance?.Clear();
            SaveManager.Instance.MarkFtueAsComplete("combat_rules_ftue");
            var tooth = (ArchaicTooth)ModelDb.Relic<ArchaicTooth>().ToMutable();
            CardCmd.Upgrade(player.Deck.Cards.OfType<MobDance>().Single(), CardPreviewStyle.None);
            Check(tooth.SetupForPlayer(player) && tooth.StarterCard?.Id == ModelDb.GetId<MobDance>() &&
                tooth.AncientCard?.Id == ModelDb.GetId<MobRoundDance>(), "Archaic Tooth offers Gremlin Round Dance from Gremlin Dance");
            await RelicCmd.Obtain(tooth, player);
            var upgradedDance = player.Deck.Cards.OfType<MobRoundDance>().Single();
            Check(upgradedDance.IsUpgraded && upgradedDance.DynamicVars.Damage.BaseValue == 12 &&
                !player.Deck.Cards.Any(card => card is MobDance) && player.Deck.Cards.Count(card => card is MobPickFront) == 1 &&
                player.Deck.Cards.Count == 10, "native Archaic Tooth replaces only Gremlin Dance and inherits its upgrade");
            Check(MegaCrit.Sts2.Core.Saves.SaveUtil.CardOrDeprecated(new ModelId("CARD", "MOB_PERFECT_HANDOFF")) is MobRoundDance,
                "older serialized Perfect Handoff cards resolve to Gremlin Round Dance");
            var tome = (DustyTome)ModelDb.Relic<DustyTome>().ToMutable();
            tome.SetupForPlayer(player);
            Check(tome.AncientCard == ModelDb.GetId<MobAllHands>(), "Dusty Tome picks All Hands and excludes the starter transformation");
            string beforeCombat = bell.GremlinMobLine;
            await RunManager.Instance.EnterRoomDebug(RoomType.Monster, model: ModelDb.Encounter<AxebotsNormal>().ToMutable(), showTransition: false);
            await WaitFor(tree, () => player.PlayerCombatState?.Phase == PlayerTurnPhase.Play);
            var actor = NCombatRoom.Instance!.GetCreatureNode(player.Creature)!;
            Check(actor.GetChildren().OfType<Node2D>().Count(node => node.Visible && node.Name.ToString().StartsWith("MobFighter_")) == 5,
                "five battlefield figures");
            var rigs = Descendants(actor).OfType<Node2D>().Where(node => node.Name.ToString().StartsWith(rigPrefix)).ToArray();
            Check(rigs.Length == 5, "five original Spine sprites mounted");
            await WaitFor(tree, () => rigs.All(node => new MegaSprite(node).TryGetAnimationState()?.GetCurrentAnimationDuration() > 0));
            Check(true, "five converted rigs initialized using native Spine");
            foreach (var rig in rigs) CheckRigScale(rig);
            Check(player.PlayerCombatState!.Energy == 4 && player.PlayerCombatState.Hand.Cards.Count == 6 && beforeCombat != bell.GremlinMobLine,
                "starter relic gains 1 Energy, draws 1 extra card, and rotates");

            var body = player.Creature;
            var combat = body.CombatState!;
            var enemy = combat.HittableEnemies.First();
            var context = new ThrowingPlayerChoiceContext();
            var indicator = body.Powers.OfType<MobFighterPassive>().Single();
            string firstPassive = indicator.Title.GetFormattedText();
            Check(firstPassive.Length > 0 && indicator.Description.GetFormattedText().Length > 0 &&
                indicator.Icon.GetImage().GetData().SequenceEqual(ResourceLoader.Load<Texture2D>(
                    "res://gremlin_mob_2/original/powers/gremlinPower.res").GetImage().GetData()),
                "front fighter passive uses archived icon and localization");
            float normalCameraScale = actor.GetGlobalTransformWithCanvas().X.Length();
            var hud = Descendants(NCombatRoom.Instance!).OfType<TextureRect>()
                .Where(node => node.Name.ToString().StartsWith("GremlinOrbLayer")).ToArray();
            Check(hud.Length == 6 && hud.Any(node => node.Texture.ResourcePath.EndsWith("layer1.res")),
                "combat energy counter uses six original Gremlin orb layers");
            Check(hud.All(layer => layer.Size.IsEqualApprox(new Vector2(128, 128))) &&
                hud.Single(layer => layer.Name == "GremlinOrbLayer6").Texture.GetImage().GetUsedRect().Size == new Vector2I(117, 117),
                "combat orb matches Downfall's 128 unit layout with the original 117 pixel visible rim");
            int energy = player.PlayerCombatState!.Energy;
            await PlayerCmd.SetEnergy(0, player);
            Check(hud.Any(node => node.Texture.ResourcePath.EndsWith("layer1d.res")), "zero energy uses the original dark textures");
            await PlayerCmd.SetEnergy(energy, player);
            var reward = new CardReward(new CardCreationOptions([character.CardPool], CardCreationSource.Encounter, CardRarityOddsType.RegularEncounter), 3, player);
            reward.Populate();
            Check(reward.Cards.Count() == 3 && reward.Cards.All(card => card.Pool == character.CardPool), "native encounter reward offers three squad cards");
            var strike = combat.CreateCard<MobStrike>(player);
            await CardPileCmd.Add(strike, PileType.Hand);
            int hp = enemy.CurrentHp;
            Check(strike.TryManualPlay(enemy), "strike queued through the hand");
            await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == strike));
            Check(enemy.CurrentHp < hp, "strike dealt native attack damage");

            var rotate = combat.CreateCard<MobCoverShift>(player);
            await CardPileCmd.Add(rotate, PileType.Hand);
            body.SetCurrentHpInternal(12);
            body.GainBlockInternal(3);
            string originalFront = bell.GremlinMobLine.Split(',')[0];
            Check(rotate.TryManualPlay(null), "rotation card queued through the hand");
            await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == rotate));
            Check(body.CurrentHp == 16 && body.Block >= 7 && bell.GremlinMobLine.Split(',')[^1] == originalFront,
                "resolved rotation kept Block and preserved outgoing fighter's 12 HP");
            Check(indicator.Title.GetFormattedText() != firstPassive, "passive indicator follows rotation without stacking");
            await tree.ToSignal(tree.CreateTimer(1.4), SceneTreeTimer.SignalName.Timeout);
            CheckFormation(actor, bell);
            var nativeHp = actor.GetNode<NCreatureStateDisplay>("%HealthBar").GetNode<NHealthBar>("%HealthBar").GetNode<Label>("%HpLabel");
            var reserveHp = Descendants(actor).OfType<Label>().Where(label => label.Name == "ReserveHp" && label.IsVisibleInTree()).ToArray();
            Check(reserveHp.Length == 4 && nativeHp.GetThemeConstant("outline_size") > 0 && reserveHp.All(label =>
                    label.GetThemeConstant("outline_size") == nativeHp.GetThemeConstant("outline_size") &&
                    label.GetThemeColor("font_outline_color") == Colors.Black),
                "all four reserve HP numbers have a black outline at native thickness");
            Check(Descendants(actor).OfType<Label>().Count(label => label.Name == "ReserveHp" && label.IsVisibleInTree()) == 4
                && Descendants(actor).OfType<Label>().Any(label => label.Name == "ReserveHp" && label.IsVisibleInTree() && label.Text == "12"),
                "legacy standby HP numbers display four reserves and outgoing member's 12 HP");
            CheckReserveHover(actor);
            tree.Root.GetTexture().GetImage().SavePng(Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "mob-formation.png"));

            var handoff = combat.CreateCard<MobPickFront>(player);
            await CardPileCmd.Add(handoff, PileType.Hand);
            Check(handoff.TryManualPlay(null), "targeted handoff queued through the hand");
            await WaitFor(tree, () => NOverlayStack.Instance?.Peek() is NSimpleCardSelectScreen);
            var screen = (NSimpleCardSelectScreen)NOverlayStack.Instance!.Peek()!;
            var cards = Descendants(screen).OfType<MegaCrit.Sts2.Core.Nodes.Cards.NCard>()
                .Select(node => node.Model).OfType<CardModel>().Where(card => card.Id.Entry.EndsWith("_OPTION")).Distinct().ToArray();
            Check(cards.Length == 4, "native handoff overlay displays all four available reserve cards");
            await tree.ToSignal(tree.CreateTimer(.45), SceneTreeTimer.SignalName.Timeout);
            tree.Root.GetTexture().GetImage().SavePng(Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "mob-handoff.png"));
            var clicked = ModelDb.GetById<CardModel>(cards.First(card => card.Id.Entry == "MOB_" + new[] { "BRUTE", "BRUISER", "GUARD", "SCOUT", "MAGE" }[int.Parse(originalFront.Split(':')[0])] + "_OPTION").Id);
            AccessTools.Method(typeof(NSimpleCardSelectScreen), "OnCardClicked").Invoke(screen, [cards.Single(card => card.Id == clicked.Id)]);
            await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == handoff));
            Check(body.CurrentHp == 12 && bell.GremlinMobLine.Split(',')[0] == originalFront,
                "choosing a reserve in the native overlay switched to that fighter and restored their individual HP");
            body.SetCurrentHpInternal(16);

            enemy.SetMaxHpInternal(600);
            enemy.SetCurrentHpInternal(600);
            var chargeAttack = combat.CreateCard<MobChargeJab>(player);
            var doubleBlast = combat.CreateCard<MobDoubleBlast>(player);
            await PlayerCmd.GainEnergy(8, player);
            await CardPileCmd.Add(chargeAttack, PileType.Hand);
            await CardPileCmd.Add(doubleBlast, PileType.Hand);
            await PowerCmd.Apply<MobChargePower>(context, body, 3, body, null);
            Check(body.GetPower<MobBangPower>() is { Amount: 7 } bang &&
                bang.BigIcon.GetImage().GetData().SequenceEqual(ResourceLoader.Load<Texture2D>(
                    "res://gremlin_mob_2/original/powers/bang.res").GetImage().GetData()),
                "ready charge shows the original Bang icon and +7");
            await tree.ToSignal(tree.CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
            tree.Root.GetTexture().GetImage().SavePng(Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "mob-effects.png"));
            hp = enemy.CurrentHp;
            Check(doubleBlast.TryManualPlay(null), "charge-exempt attack queued");
            await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == doubleBlast));
            Check(body.GetPowerAmount<MobChargePower>() == 3 && hp - enemy.CurrentHp >= 22,
                "Kablamo receives the charge bonus on both hits and preserves all charge");
            hp = enemy.CurrentHp;
            Check(chargeAttack.TryManualPlay(enemy), "charge-consuming attack queued");
            await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == chargeAttack));
            Check(body.GetPowerAmount<MobChargePower>() == 1 && !body.HasPower<MobBangPower>() && hp - enemy.CurrentHp >= 16,
                "charged Attack gains damage, spends only original charge, and banks its newly gained charge");
            await PowerCmd.Remove<MobChargePower>(body);
            await TestMemberPassives(tree, player, bell, enemy);
            string frontBeforePotion = bell.GremlinMobLine.Split(',')[0];
            await UsePotion<MobRandomHandoffPotion>(player, context);
            Check(bell.GremlinMobLine.Split(',')[0] != frontBeforePotion, "swap potion promotes a different living Gremlin");
            int blockBeforePotion = body.Block;
            await UsePotion<MobCrewTonic>(player, context);
            Check(body.GetPowerAmount<MobChargePower>() == 2 && body.Block == blockBeforePotion + 4,
                "Gremlin potion applies the original double charge and two Block triggers");
            await PowerCmd.Remove<MobChargePower>(body);
            body.SetCurrentHpInternal(body.MaxHp);

            await PowerCmd.Apply<MobTemporaryGuard>(context, body, 4, body, null);
            await CreatureCmd.Damage(context, body, 3, ValueProp.Unblockable | ValueProp.Unpowered, enemy);
            Check(body.CurrentHp == 16 && body.GetPowerAmount<MobTemporaryGuard>() == 1, "partial temporary HP absorption");
            await CreatureCmd.Damage(context, body, 4, ValueProp.Unblockable | ValueProp.Unpowered, enemy);
            Check(body.CurrentHp == 13 && body.GetPowerAmount<MobTemporaryGuard>() == 0, "temporary HP depletion");
            await CreatureCmd.Damage(context, body, 50, ValueProp.Unblockable | ValueProp.Unpowered, enemy);
            Check(body.IsAlive && bell.GremlinMobLine.EndsWith(":0") && actor.GetChildren().OfType<Node2D>()
                .Count(node => node.Visible && node.Name.ToString().StartsWith("MobFighter_")) == 4,
                "lethal damage removes one fighter and promotes a living reserve");
            var revival = combat.CreateCard<MobRevivalRite>(player);
            await CardPileCmd.Add(revival, PileType.Hand);
            await PowerCmd.Apply<MobChargePower>(context, body, 3, body, null);
            Check(revival.TryManualPlay(null), "revival queued through the hand");
            await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == revival));
            Check(bell.GremlinMobLine.EndsWith(":10"), "revival restores the only fallen fighter at 10 HP");
            await CreatureCmd.Kill(body);
            await UsePotion<MobReturningSpirit>(player, context);
            Check(bell.GremlinMobLine.EndsWith(":8"), "Necromancy potion revives at half maximum HP");
            var scatter = combat.CreateCard<MobScatterStep>(player);
            await CardPileCmd.Add(scatter, PileType.Hand);
            Check(scatter.TryManualPlay(null), "scatter queued through the hand");
            await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == scatter));
            string active = bell.GremlinMobLine.Split(',')[0];
            await CreatureCmd.Damage(context, body, 50, ValueProp.Unblockable | ValueProp.Unpowered, enemy);
            Check(!bell.GremlinMobLine.StartsWith(active) && bell.GremlinMobLine.EndsWith(active) &&
                body.GetPowerAmount<MobEvadeAndRotate>() == 0, "scatter prevents a lethal hit and randomly rotates once");

            var rampage = combat.CreateCard<MobJuggernaut>(player);
            await PlayerCmd.GainEnergy(4, player);
            await CardPileCmd.Add(rampage, PileType.Hand);
            Check(rampage.TryManualPlay(null), "juggernaut queued through the hand");
            await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == rampage));
            Check(body.HasPower<MobRampageForm>() && body.GetPowerAmount<MobTemporaryGuard>() == 20 && !body.HasPower<MobFighterPassive>(),
                "juggernaut entered with 20 temporary HP and replaces the passive indicator");
            await WaitFor(tree, () => Descendants(actor).OfType<Node2D>().Any(node => node.Name == rigPrefix + "nob" &&
                node.Visible && new MegaSprite(node).IsAnimationStateReady()));
            Check(true, "original Juggernaut Spine initialized");
            CheckRigScale(Descendants(actor).OfType<Node2D>().Single(node => node.Name == rigPrefix + "nob"));
            hp = body.CurrentHp;
            await CreatureCmd.Damage(context, body, 50, ValueProp.Unblockable | ValueProp.Unpowered, enemy);
            Check(body.CurrentHp == hp && !body.HasPower<MobRampageForm>() && body.HasPower<MobFighterPassive>(),
                "juggernaut cancels overflow and restores the passive on leaving form");

            await CreatureCmd.GainMaxHp(body, 7);
            Check(body.MaxHp == 18 && bell.GremlinMobLine.Split(',').All(entry => int.Parse(entry.Split(':')[1]) <= 18),
                "native maximum HP gains are divided across five members");
            await CreatureCmd.Kill(body);
            Check(body.IsAlive && bell.GremlinMobLine.EndsWith(":0"), "direct kill also promotes a reserve immediately");
            await HealRestSiteOption.ExecuteRestSiteHeal(player, isMimicked: true);
            Check(bell.GremlinMobLine.EndsWith(":9"), "native Rest revives one member with half of their maximum HP");

            string result = bell.GremlinMobLine;
            var serialized = player.ToSerializable();
            serialized.Relics.Add(new MegaCrit.Sts2.Core.Saves.Runs.SerializableRelic { Id = new ModelId("RELIC", "MOB_PRICKLY_GUARD") });
            var restored = Player.FromSerializable(serialized);
            Check(restored.Relics.OfType<MobRotationBell>().Single().GremlinMobLine == result, "native player serialization preserves order and individual health");
            Check(restored.Relics.Count == player.Relics.Count, "older serialized inventory loads without retired shared relic");
            var legacyUpgrade = player.ToSerializable();
            legacyUpgrade.Relics.Add(new MegaCrit.Sts2.Core.Saves.Runs.SerializableRelic
            {
                Id = ModelDb.GetId<MobRotatingBell>(),
                Props = new MegaCrit.Sts2.Core.Saves.Runs.SavedProperties
                {
                    bools = [new(nameof(RelicModel.IsWax), true)]
                }
            });
            var migrated = Player.FromSerializable(legacyUpgrade);
            Check(migrated.Relics.OfType<MobRosterRelic>().Single() is MobRotatingBell legacyCrown &&
                legacyCrown.GremlinMobLine == result && legacyCrown.IsWax,
                "legacy double-relic save migrates the exact lineup and retains the crown's own flags");
            await RunManager.Instance.EnterRoomDebug(RoomType.Event, model: ModelDb.Event<TrashHeap>(), showTransition: false);
            var currentRoom = (EventRoom)player.RunState.CurrentRoom!;
            var rip = currentRoom.LocalMutableEvent.CurrentOptions.Single(option => option.TextKey.EndsWith(".DIVE_IN"));
            var ids = bell.GremlinMobLine.Split(',').Select(pair => pair.Split(':')).ToDictionary(p => p[0], p => int.Parse(p[1]));
            Check(rip.Description.GetFormattedText().Contains("2"), "event option shows the per-member cost");
            await rip.Chosen();
            var after = bell.GremlinMobLine.Split(',').Select(pair => pair.Split(':')).ToDictionary(p => p[0], p => int.Parse(p[1]));
            Check(ids.All(pair => after[pair.Key] == Math.Max(0, pair.Value - 2)), "Trash Heap damage applies to each living fighter");
            await RunManager.Instance.EnterRoomDebug(RoomType.Event, model: ModelDb.Event<UnrestSite>(), showTransition: false);
            currentRoom = (EventRoom)player.RunState.CurrentRoom!;
            int beforeMax = body.MaxHp;
            var kill = currentRoom.LocalMutableEvent.CurrentOptions.Single(option => option.TextKey.EndsWith(".KILL"));
            await kill.Chosen();
            Check(body.MaxHp == beforeMax - 2, "Unrest Site loses two shared Max HP from the original eight");

            await RunManager.Instance.EnterRoomDebug(RoomType.Event, model: ModelDb.Event<UnrestSite>(), showTransition: false);
            currentRoom = (EventRoom)player.RunState.CurrentRoom!;
            await currentRoom.LocalMutableEvent.CurrentOptions.Single(option => option.TextKey.EndsWith(".REST")).Chosen();
            Check(bell.GremlinMobLine.Split(',').All(pair => pair.EndsWith(":" + body.MaxHp)),
                "full-heal event restores all living reserves");
            await CheckEventHealthPreviews(player, bell);
            await TestKaiserCrab(tree, player, bell, normalCameraScale);
            await RelicCmd.Obtain(tome, player);
            Check(player.Deck.Cards.OfType<MobAllHands>().Single().IsUpgraded, "native Dusty Tome adds upgraded All Hands to the deck");

            await RunManager.Instance.EnterRoomDebug(RoomType.Event, model: ModelDb.Event<Orobas>(), showTransition: false);
            currentRoom = (EventRoom)player.RunState.CurrentRoom!;
            var boon = currentRoom.LocalMutableEvent.CurrentOptions.Single(option =>
                option.Relic is MobRotatingBell or MobPledgedMember or MobReserveTalisman);
            var frontReserves = bell.GremlinMobLine.Split(',').Skip(1).Where(pair => int.Parse(pair.Split(':')[1]) > 0).ToArray();
            string beforeBoon = bell.GremlinMobLine;
            await boon.Chosen();
            Check(player.Relics.Any(relic => relic.Id == boon.Relic!.Id), "native Ancient event grants one of the legacy boss relics");
            if (boon.Relic is MobRotatingBell)
            {
                bell = player.Relics.OfType<MobRosterRelic>().Single();
                Check(bell.GremlinMobLine == beforeBoon, "native crown option replaced the starter and retained the active roster");
            }
            if (!player.Relics.OfType<MobPledgedMember>().Any())
                await RelicCmd.Obtain<MobPledgedMember>(player);
            Check(bell.GremlinMobLine.Split(',').Contains(frontReserves[0].Split(':')[0] + ":-1"),
                "Leader Voucher takes the first living reserve as in the archive");
            if (bell is MobRotationBell)
            {
                string line = bell.GremlinMobLine;
                bell = await RelicCmd.Obtain<MobRotatingBell>(player);
                Check(bell.GremlinMobLine == line && player.Relics.OfType<MobRosterRelic>().Single() == bell,
                    "obtaining the crown replaces the starter and preserves the active roster");
            }
            var upgradedReload = Player.FromSerializable(player.ToSerializable());
            Check(upgradedReload.Relics.OfType<MobRotatingBell>().Single().GremlinMobLine == bell.GremlinMobLine,
                "upgraded relic saves and reloads its own roster");

            await RunManager.Instance.EnterRoomDebug(RoomType.RestSite, showTransition: false);
            var rest = NRestSiteRoom.Instance!;
            Check(Descendants(rest).OfType<Node2D>().Any(node => node.Name == "GremlinMobOriginalRoomVisuals"),
                "native rest site uses original Gremlin sprites");
            await RunManager.Instance.EnterRoomDebug(RoomType.Shop, showTransition: false);
            Check(Descendants(NMerchantRoom.Instance!).OfType<Node2D>().Any(node => node.Name == "GremlinMobOriginalRoomVisuals"),
                "native merchant uses original Gremlin sprites");

            if (OS.GetCmdlineUserArgs().Contains("gremlin-check-mixed"))
                await TestMeltedRoster(tree, player, bell);
            else
                await TestRunEnding(tree, player);
            Console.WriteLine("GREMLIN_SMOKE_OK");
            tree.Quit();
        }
        catch (Exception error)
        {
            Console.WriteLine("GREMLIN_SMOKE_FAIL " + error);
            tree.Quit(1);
        }
    }

    private static void CheckEffectIcons(GremlinMobCharacter character)
    {
        var powers = typeof(MobPower).Assembly.GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(PowerModel)))
            .Select(type => ModelDb.GetById<PowerModel>(ModelDb.GetId(type))).ToArray();
        foreach (var power in powers)
        {
            if (power.ResolvedBigIconPath.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
                !HasPixels(power.Icon) || !HasPixels(power.BigIcon))
                throw new InvalidOperationException("Missing power art for " + power.Id);
        }
        foreach (var relic in character.RelicPool.AllRelics)
        {
            var icon = relic.Icon;
            var outline = relic.IconOutline;
            var large = relic.BigIcon;
            Check(HasPixels(icon) && HasPixels(outline) && HasPixels(large) &&
                icon.GetSize() == new Vector2(85, 85) && outline.GetSize() == icon.GetSize() &&
                large.GetSize() == new Vector2(256, 256) &&
                icon.ResourcePath.EndsWith("_small.res") && outline.ResourcePath.EndsWith("_outline.res") &&
                large.ResourcePath.EndsWith("_large.res"), relic.Id.Entry + " resolves distinct 85×85 icon and outline, and 256×256 detail art");
            var pixels = icon.GetImage().GetUsedRect().Size;
            Check(Math.Max(pixels.X, pixels.Y) is >= 52 and <= 84, relic.Id.Entry + " fills its small icon at " + pixels);
        }
        foreach (var potion in character.PotionPool.AllPotions)
            if (!ResourceLoader.Exists(potion.LargeImagePath) || !HasPixels(potion.Image) ||
                !HasPixels(potion.LargeImage) || !HasPixels(potion.Outline))
                throw new InvalidOperationException("Missing potion art for " + potion.Id);
        Check(true, $"{powers.Length} powers, 14 relics and 3 potions have native icons and non-placeholder large art");
    }

    private static void CheckRelicWidgets(NMainMenu mainMenu)
    {
        var relic = ModelDb.Relic<MobRotationBell>();
        var small = NRelic.CreateNonNull(relic, NRelic.IconSize.Small);
        var large = NRelic.CreateNonNull(relic, NRelic.IconSize.Large);
        mainMenu.AddChild(small);
        mainMenu.AddChild(large);
        try
        {
            Check(small.GetNode<TextureRect>("%Icon").Texture == relic.Icon &&
                small.GetNode<TextureRect>("%Outline").Texture == relic.IconOutline &&
                large.GetNode<TextureRect>("%Icon").Texture == relic.BigIcon &&
                !large.GetNode<TextureRect>("%Outline").Visible, "native small and large relic widgets load their respective art");
        }
        finally
        {
            small.QueueFree();
            large.QueueFree();
        }
    }

    private static async Task CheckCardPresentation(SceneTree tree, NMainMenu mainMenu, bool alternate)
    {
        var model = ModelDb.Card<MobDelayedSpark>().ToMutable();
        CardCmd.Upgrade(model, CardPreviewStyle.None);
        Check(model.HoverTips.OfType<CardHoverTip>().Single().Card is MobBurst { IsUpgraded: true },
            "Whiz+ previews the original upgraded Bang");
        Check(ModelDb.Card<MobPocketArmory>().HoverTips.OfType<CardHoverTip>().Single().Card is MobPaperWard { IsUpgraded: true },
            "Infinite Blocks previews upgraded Ward even before it is upgraded");
        Check(ModelDb.Card<MobHoneTools>().HoverTips.OfType<CardHoverTip>().Count() == 2,
            "Polish previews Shiv and Ward together");
        var owner = new Control { Position = new Vector2(580, 350), Size = new Vector2(220, 280) };
        mainMenu.AddChild(owner);
        try
        {
            bool ArtMatches(NCard view, string sticker)
            {
                string prefix = alternate ? "/feminization/card_art/" : "/original/card_art/";
                return Descendants(view).OfType<TextureRect>().Any(node => node.Name == "GremlinCardArt" &&
                    node.IsVisibleInTree() && node.Texture.ResourcePath.EndsWith(prefix + sticker + ".res"));
            }

            foreach (var (standard, sticker) in new (CardModel, string)[]
            {
                (ModelDb.Card<MobRakingClaws>(), "attack_thief"),
                (ModelDb.Card<MobSteadySpell>(), "skill_wiz"),
                (ModelDb.Card<MobWizardSchool>(), "power_wiz"),
                (ModelDb.Card<MobJuggernaut>(), "power_nob")
            })
            {
                var tip = NHoverTipSet.CreateAndShow(owner, [HoverTipFactory.FromCard(standard)], HoverTipAlignment.Right)!;
                var card = Descendants(tip).OfType<NCard>().Single();
                Check(ArtMatches(card, sticker), standard.Id.Entry + " uses the selected skin's static card sticker");
                NHoverTipSet.Remove(owner);
            }

            var tips = NHoverTipSet.CreateAndShow(owner, ModelDb.Card<MobDance>().HoverTips, HoverTipAlignment.Right)!;
            var preview = Descendants(tips).OfType<NCard>().Single();
            var firstModel = preview.Model;
            Check(ArtMatches(preview, "attack_thief") && Descendants(preview).OfType<Godot.Timer>().Single().WaitTime == .9,
                "Dance uses the selected skin and a 0.9-second rotation");
            string firstText = firstModel!.GetDescriptionForPile(PileType.Deck);
            await WaitFor(tree, () => preview.Model != firstModel);
            string nextText = preview.Model!.GetDescriptionForPile(PileType.Deck);
            Check(ArtMatches(preview, "attack_fat") && firstText != nextText,
                $"Dance preview rotates text and skin art together ({firstText} / {nextText})");
            await tree.ToSignal(tree.CreateTimer(.22), SceneTreeTimer.SignalName.Timeout);
            tree.Root.GetTexture().GetImage().SavePng(Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "mob-card-preview.png"));
        }
        finally
        {
            NHoverTipSet.Remove(owner);
            owner.QueueFree();
        }
    }

    private static bool HasPixels(Texture2D? texture) =>
        texture?.GetImage() is { } image && image.GetUsedRect().Size.X > 1 && !texture.ResourcePath.Contains("missing", StringComparison.OrdinalIgnoreCase);

    private static async Task ExportSkinCardArt(SceneTree tree)
    {
        var create = AccessTools.Method(typeof(MobCard).Assembly.GetType("GremlinMob2.OriginalAssets"), "Spine");
        (string Rig, string Sticker)[] fighters =
            [("angry", "thief"), ("fat", "fat"), ("shield", "tsundere"), ("sneak", "sneaky"), ("wizard", "wiz"), ("nob", "nob")];
        string output = Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "skin-card-art");
        Directory.CreateDirectory(output);
        foreach (var (rig, sticker) in fighters)
        {
            var viewport = new SubViewport
            {
                Size = new Vector2I(768, 768), TransparentBg = true, Disable3D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            tree.Root.AddChild(viewport);
            var sprite = (Node2D)create.Invoke(null, [rig, true])!;
            viewport.AddChild(sprite);
            sprite.Position = new Vector2(384, 620);
            await WaitFor(tree, () => new MegaSprite(sprite).IsAnimationStateReady());
            for (int frame = 0; frame < 3; frame++)
                await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var image = viewport.GetTexture().GetImage();
            Rect2I bounds = image.GetUsedRect();
            Check(bounds.Size.X > 1 && bounds.Size.Y > 1 && bounds.Position.X > 0 && bounds.Position.Y > 0 &&
                bounds.End.X < image.GetWidth() && bounds.End.Y < image.GetHeight() &&
                image.GetRegion(bounds).SavePng(Path.Combine(output, sticker + ".png")) == Error.Ok,
                $"exported uncropped skin sticker {sticker} at {bounds}");
            viewport.QueueFree();
        }
    }

    private static void CheckRigScale(Node2D rig)
    {
        string name = rig.Name.ToString();
        string fighter = name[(name.IndexOf('_') + 1)..];
        string variants = name.StartsWith("FeminizationSpine_") ? "feminization/" : "";
        using var json = System.Text.Json.JsonDocument.Parse(Godot.FileAccess.GetFileAsString(
            "res://gremlin_mob_2/original/" + variants + "char/" + fighter + "/skeleton.v3.json"));
        using var skeleton = new MegaSprite(rig).GetSkeleton()!;
        int samples = 0;
        foreach (var bone in json.RootElement.GetProperty("bones").EnumerateArray())
        {
            if (!bone.TryGetProperty("length", out var length) || length.GetSingle() <= 1) continue;
            using var native = skeleton.FindBone(bone.GetProperty("name").GetString()!)!;
            using var data = native.GetData();
            float actual = data.BoundObject.Call("get_length").AsSingle();
            if (Math.Abs(length.GetSingle() - actual) > .03)
                throw new InvalidOperationException($"Scaled bone in {fighter}: expected {length}, actual {actual}");
            samples++;
        }
        Check(rig.Scale.IsEqualApprox(Vector2.One) && rig.GetParent<Node2D>().Scale.IsEqualApprox(Vector2.One) &&
            rig.GetParent().GetParent<Node2D>().Scale.IsEqualApprox(Vector2.One) && samples > 0,
            fighter + " uses the original unscaled bone lengths");
    }

    private static async Task TestMeltedRoster(SceneTree tree, Player player, MobRosterRelic roster)
    {
        roster.IsWax = true;
        await RelicCmd.Melt(roster);
        await RunManager.Instance.EnterRoomDebug(RoomType.Monster, model: ModelDb.Encounter<AxebotsNormal>().ToMutable(), showTransition: false);
        await WaitFor(tree, () => player.PlayerCombatState?.Phase == PlayerTurnPhase.Play);
        var combat = player.Creature.CombatState!;
        Check(roster.IsMelted && player.RunState.IterateHookListeners(combat).Count(model => model == roster) == 1 &&
            player.Creature.HasPower<MobFighterPassive>(), "melted crown still supplies exactly one roster hook in native combat");
        string line = roster.GremlinMobLine;
        await ((MobRotatingBell)roster).AfterShuffle(new ThrowingPlayerChoiceContext(), player);
        Check(line == roster.GremlinMobLine, "melted crown's shuffle reward remains disabled");
    }

    private static void CheckFormation(NCreature actor, MobRosterRelic bell)
    {
        string[] names = ["Brute", "Bruiser", "Guard", "Scout", "Mage"];
        var living = bell.GremlinMobLine.Split(',').Select(pair => pair.Split(':'))
            .Where(pair => int.Parse(pair[1]) > 0).ToArray();
        for (int i = 0; i < living.Length; i++)
        {
            var figure = actor.GetNode<Node2D>("MobFighter_" + names[int.Parse(living[i][0])]);
            var expected = actor.Visuals.Position + new Vector2(-90 * i, i == 0 ? 0 : -30);
            if (figure.Position.DistanceTo(expected) > 1) throw new InvalidOperationException($"Slot {i} position {figure.Position}, expected {expected}");
        }
        Check(true, "front uses native actor origin and occupied reserves keep original 90-unit slot spacing");
    }

    private static async Task CheckEventHealthPreviews(Player player, MobRosterRelic bell)
    {
        var body = player.Creature;
        int maxHp = body.MaxHp;
        string roster = bell.GremlinMobLine;

        async Task<EventModel> Enter<T>() where T : EventModel
        {
            await RunManager.Instance.EnterRoomDebug(RoomType.Event, model: ModelDb.Event<T>(), showTransition: false);
            return ((EventRoom)player.RunState.CurrentRoom!).LocalMutableEvent;
        }

        string Preview(EventModel model, string suffix)
        {
            var option = model.CurrentOptions.Single(option => option.TextKey.EndsWith(suffix));
            model.DynamicVars.AddTo(option.Description);
            return option.Description.GetFormattedText();
        }

        var beacon = await Enter<DrowningBeacon>();
        Check(Preview(beacon, ".CLIMB").Contains("[red]3[/red]"), "Drowning Beacon previews the actual three-point maximum HP cost");
        var baths = await Enter<AbyssalBaths>();
        string immerse = Preview(baths, ".IMMERSE");
        Check(immerse.Contains("[green]1[/green]") && immerse.Contains("[red]1[/red]"),
            "Abyssal Baths shows both scaled amounts independently");
        var seed = await Enter<SapphireSeed>();
        Check(Preview(seed, ".EAT").Contains("[green]2[/green]"), "Sapphire Seed previews two HP of healing per living member");

        var waffle = await RelicCmd.Obtain<LeesWaffle>(player);
        Check(waffle.DynamicDescription.GetFormattedText().Contains("[blue]2[/blue]"), "Waffle description shows two shared maximum HP");
        body.SetMaxHpInternal(maxHp);
        body.SetCurrentHpInternal(2);
        await waffle.AfterObtained();
        Check(body.MaxHp == maxHp + 2 && body.CurrentHp == body.MaxHp &&
            bell.GremlinMobLine.Split(',').All(member => member.EndsWith(":" + body.MaxHp)),
            "Waffle heals the leader and all living reserves even in an ordinary event room");

        body.SetMaxHpInternal(maxHp);
        body.SetCurrentHpInternal(maxHp);
        var conveyor = await Enter<EndlessConveyor>();
        var roll = AccessTools.Method(typeof(EndlessConveyor), "RollDish");
        var current = AccessTools.Field(typeof(EndlessConveyor), "_lastDishId");
        bool RollsClamRoll()
        {
            for (int i = 0; i < 40; i++)
            {
                roll.Invoke(conveyor, []);
                if (current.GetValue(conveyor) is "CLAM_ROLL") return true;
            }
            return false;
        }
        bell.GremlinMobLine = roster;
        Check(!RollsClamRoll(), "Endless Conveyor excludes healing if all living members are full");
        var members = roster.Split(',');
        members[^1] = members[^1].Split(':')[0] + ":1";
        bell.GremlinMobLine = string.Join(',', members);
        Check(RollsClamRoll() && body.CurrentHp == maxHp, "Endless Conveyor can offer healing when only a reserve is injured");
        bell.GremlinMobLine = roster;
    }

    private static async Task TestKaiserCrab(SceneTree tree, Player player, MobRosterRelic bell, float normalCameraScale)
    {
        await RunManager.Instance.EnterRoomDebug(RoomType.Boss, model: ModelDb.Encounter<KaiserCrabBoss>().ToMutable(), showTransition: false);
        await WaitFor(tree, () => player.PlayerCombatState?.Phase == PlayerTurnPhase.Play);
        NModalContainer.Instance?.Clear();
        var room = NCombatRoom.Instance!;
        var actor = room.GetCreatureNode(player.Creature)!;
        var origin = actor.Visuals.GetGlobalTransformWithCanvas().Origin;
        await tree.ToSignal(tree.CreateTimer(1), SceneTreeTimer.SignalName.Timeout);
        Check(Math.Abs(actor.Position.X) < 1 && player.Creature.CombatState!.HittableEnemies.Count() == 2 &&
            actor.GetChildren().OfType<Node2D>().Count(node => node.Visible && node.Name.ToString().StartsWith("MobFighter_")) == 5,
            "Kaiser Crab keeps the fully centered native lead, four reserves and both claws");
        Check(Math.Abs(actor.GetGlobalTransformWithCanvas().X.Length() / normalCameraScale - .75f) < .02f,
            "Kaiser Crab's 75% camera scale applies uniformly to the original rigs");
        CheckFormation(actor, bell);
        var rotate = player.Creature.CombatState!.CreateCard<MobCoverShift>(player);
        string old = bell.GremlinMobLine;
        await CardPileCmd.Add(rotate, PileType.Hand);
        Check(rotate.TryManualPlay(null), "Kaiser Crab rotation queued through the hand");
        await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == rotate));
        await tree.ToSignal(tree.CreateTimer(1), SceneTreeTimer.SignalName.Timeout);
        Check(bell.GremlinMobLine != old && actor.Visuals.GetGlobalTransformWithCanvas().Origin.DistanceTo(origin) < 1,
            "Kaiser Crab rotation changed the leader without moving the native front anchor");
        CheckFormation(actor, bell);
        CheckReserveHover(actor);
        tree.Root.GetTexture().GetImage().SavePng(Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "mob-kaiser-crab.png"));

        var body = player.Creature;
        var combat = body.CombatState!;
        var enemies = combat.HittableEnemies.ToArray();
        string savedLine = bell.GremlinMobLine;
        bell.GremlinMobLine = "0:16,1:16,2:16,3:16,4:16";
        foreach (bool upgraded in new[] { false, true })
        {
            var allHands = combat.CreateCard<MobAllHands>(player);
            if (upgraded) CardCmd.Upgrade(allHands, CardPreviewStyle.None);
            int repeats = upgraded ? 3 : 2;
            await PlayerCmd.GainEnergy(1, player);
            await CardPileCmd.Add(allHands, PileType.Hand);
            int startingEnergy = player.PlayerCombatState!.Energy;
            int startingBlock = body.Block;
            int startingCharge = body.GetPowerAmount<MobChargePower>();
            int startingHistory = CombatManager.Instance.History.Entries.Count();
            Check(allHands.EnergyCost.GetAmountToSpend() == 1 && allHands.TryManualPlay(null), $"All Hands x{repeats} queues at one energy");
            await WaitFor(tree, () => allHands.Pile?.Type == PileType.Exhaust);
            var order = CombatManager.Instance.History.Entries.Skip(startingHistory).OfType<PowerReceivedEntry>()
                .Where(entry => entry.Power is MobMadStrength or MobChargePower).Select(entry => entry.Power.GetType());
            Check(player.PlayerCombatState.Energy == startingEnergy - 1 && body.Block == startingBlock + 2 * repeats &&
                body.GetPowerAmount<MobChargePower>() == startingCharge + repeats &&
                order.SequenceEqual(Enumerable.Repeat(typeof(MobMadStrength), repeats).Concat(Enumerable.Repeat(typeof(MobChargePower), repeats))),
                $"All Hands resolves each Gremlin's {repeats} triggers together in formation order and exhausts");
        }

        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Remove<MobChargePower>(body);
        await PowerCmd.Remove<MobMadStrength>(body);
        await PowerCmd.Remove<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>(body);
        bell.GremlinMobLine = "2:16,0:16,1:16,3:16,4:16";
        foreach (var claw in enemies)
        {
            claw.SetMaxHpInternal(200);
            claw.SetCurrentHpInternal(200);
        }
        var dance = body.CombatState.CreateCard<MobRoundDance>(player);
        await CardPileCmd.Add(dance, PileType.Hand);
        int block = body.Block;
        int energy = player.PlayerCombatState!.Energy;
        int drawCount = CombatManager.Instance.History.Entries.OfType<CardDrawnEntry>().Count();
        Check(dance.Type == CardType.Attack && dance.DynamicVars.Damage.BaseValue == 9 &&
            dance.TargetType == TargetType.AllEnemies && dance.TryManualPlay(null), "Gremlin Round Dance targets both Kaiser Crab claws at base damage 9");
        await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == dance));
        int drawn = CombatManager.Instance.History.Entries.OfType<CardDrawnEntry>().Count() - drawCount;
        Check(player.PlayerCombatState.Energy == energy && body.Block - block == 6 && body.GetPowerAmount<MobChargePower>() == 2 &&
            enemies.All(claw => claw.CurrentHp == 191 && claw.GetPowerAmount<MobWeakenedStrength>() != 0) &&
            drawn == 2, $"Round Dance spends zero energy and resolves all five original Dance effects " +
            $"(energy cost {energy - player.PlayerCombatState.Energy}, Block +{body.Block - block}, charge {body.GetPowerAmount<MobChargePower>()}, " +
            $"draw {drawn}, claw HP {string.Join(',', enemies.Select(claw => claw.CurrentHp))}, " +
            $"temporary Strength loss {string.Join(',', enemies.Select(claw => claw.GetPowerAmount<MobWeakenedStrength>()))})");

        bell.GremlinMobLine = "2:16,0:0,1:0,3:0,4:0";
        await PowerCmd.Apply<MobRampageForm>(context, body, 1, body, null);
        var nobleDance = body.CombatState.CreateCard<MobRoundDance>(player);
        CardCmd.Upgrade(nobleDance, CardPreviewStyle.None);
        await CardPileCmd.Add(nobleDance, PileType.Hand);
        block = body.Block;
        int nobStrength = body.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>();
        int hp = enemies[0].CurrentHp;
        int otherHp = enemies[1].CurrentHp;
        Check(nobleDance.TargetType == TargetType.AnyEnemy && nobleDance.TryManualPlay(enemies[0]),
            "upgraded Gremlin Round Dance selects one target when the Brute is fallen");
        await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == nobleDance));
        Check(body.Block == block && body.GetPowerAmount<MobChargePower>() == 2 && enemies[0].CurrentHp == hp - 12 &&
            enemies[1].CurrentHp == otherHp && body.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>() == nobStrength + 2,
            "Round Dance skips fallen gremlins and uses Nob instead of the transformed Shield Gremlin");
        bell.GremlinMobLine = savedLine;
        await PowerCmd.Remove<MobRampageForm>(body);
    }

    private static void CheckReserveHover(NCreature actor)
    {
        var hovering = (Dictionary<Control, NHoverTipSet>)AccessTools.Field(typeof(NHoverTipSet), "_activeHoverTips").GetValue(null)!;
        var reserves = Descendants(actor).OfType<Control>().Where(node => node.Name == "ReserveHover" && node.IsVisibleInTree()).ToArray();
        Check(reserves.Length == 4, "only the four reserves have active hover hitboxes");
        foreach (var reserve in reserves)
        {
            var hit = reserve.GetGlobalRect();
            var figure = reserve.GetParent<Node2D>();
            reserve.EmitSignal(Control.SignalName.MouseEntered);
            var tip = hovering[reserve].GetNode<Control>("textHoverTipContainer").GetGlobalRect();
            Check(Math.Abs(hit.GetCenter().X - figure.GlobalPosition.X) < 1 && hit.Position.Y < figure.GlobalPosition.Y &&
                tip.End.Y <= hit.Position.Y && tip.GetCenter().X < hit.GetCenter().X + 180,
                "reserve tooltip is above the associated model in " + figure.Name);
            reserve.EmitSignal(Control.SignalName.MouseExited);
            Check(!hovering.ContainsKey(reserve), "reserve tooltip clears on exit");
        }
    }

    private static async Task UsePotion<T>(Player player, PlayerChoiceContext context) where T : PotionModel
    {
        var result = await PotionCmd.TryToProcure<T>(player);
        if (!result.success) throw new InvalidOperationException("Could not procure " + typeof(T).Name);
        await result.potion.OnUseWrapper(context, player.Creature);
    }

    private static async Task TestMemberPassives(SceneTree tree, Player player, MobRosterRelic bell, MegaCrit.Sts2.Core.Entities.Creatures.Creature enemy)
    {
        var body = player.Creature;
        var combat = body.CombatState!;
        var context = new ThrowingPlayerChoiceContext();
        string saved = bell.GremlinMobLine;
        string[] members = saved.Split(',');
        void SetFront(int fighter) => bell.GremlinMobLine = string.Join(',', members.OrderByDescending(member => member.StartsWith(fighter + ":")));

        async Task Play<T>(MegaCrit.Sts2.Core.Entities.Creatures.Creature? target = null, bool free = false) where T : CardModel
        {
            await PlayerCmd.GainEnergy(1, player);
            var card = combat.CreateCard<T>(player);
            if (free) card.EnergyCost.SetThisTurnOrUntilPlayed(0);
            await CardPileCmd.Add(card, PileType.Hand);
            if (!card.TryManualPlay(target)) throw new InvalidOperationException("Passive test card did not play: " + card.Id);
            await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == card));
        }

        SetFront(0);
        body.GainBlockInternal(3);
        await CreatureCmd.Damage(context, body, 1, ValueProp.Move, enemy);
        Check(body.GetPowerAmount<MobMadStrength>() >= 2, "Mad Gremlin gains temporary Strength even on a blocked attack");
        await PowerCmd.Remove<MobMadStrength>(body);
        SetFront(1);
        int weak = enemy.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.WeakPower>();
        await Play<MobStrike>(enemy);
        Check(enemy.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.WeakPower>() == weak + 1, "Fat Gremlin applies Weak on an Attack");
        SetFront(2);
        int block = body.Block;
        await Play<MobDefend>();
        Check(body.Block - block == 7, "Shield Gremlin adds 2 Block on a Skill");
        await PowerCmd.Apply<MobChargePower>(context, body, 3, body, null);
        block = body.Block;
        int beforeDance = enemy.CurrentHp;
        await Play<MobDance>(enemy);
        await WaitFor(tree, () => !body.HasPower<MobChargePower>() && !body.HasPower<MobBangPower>());
        Check(body.Block - block == 6 && beforeDance - enemy.CurrentHp >= 13,
            $"Shield Gremlin's Dance grants Block, attacks, and spends the ready charge (Block +{body.Block - block}, damage {beforeDance - enemy.CurrentHp})");
        SetFront(3);
        int hp = enemy.CurrentHp;
        await Play<MobStrike>(enemy, free: true);
        Check(hp - enemy.CurrentHp >= 8, "Sneaky Gremlin adds damage to a 0-cost Attack and pings a random enemy");
        SetFront(4);
        await Play<MobDefend>();
        Check(body.GetPowerAmount<MobChargePower>() == 1, "Gremlin Wizard gains 1 charge on a Skill");
        await PowerCmd.Remove<MobChargePower>(body);
        bell.GremlinMobLine = saved;
    }

    private static async Task TestRunEnding(SceneTree tree, Player player)
    {
        var manager = RunManager.Instance;
        var run = (RunState)player.RunState;
        int previousWins = SaveManager.Instance.Progress.GetStatsForCharacter(player.Character.Id)?.TotalWins ?? 0;
        for (int act = 0; act < run.Acts.Count; act++)
        {
            await manager.EnterMapCoord(run.Map.BossMapPoint.coord);
            await WaitFor(tree, () => player.PlayerCombatState?.Phase == PlayerTurnPhase.Play);
            var body = player.Creature;
            var combat = body.CombatState!;
            for (int hit = 0; hit < 30 && CombatManager.Instance.IsInProgress; hit++)
            {
                await WaitFor(tree, () => !CombatManager.Instance.IsInProgress || combat.HittableEnemies.Any());
                if (!CombatManager.Instance.IsInProgress) break;
                var target = combat.HittableEnemies.First();
                target.SetCurrentHpInternal(1);
                target.LoseBlockInternal(target.Block);
                await PlayerCmd.GainEnergy(1, player);
                var strike = combat.CreateCard<MobStrike>(player);
                await CardPileCmd.Add(strike, PileType.Hand);
                Check(strike.TryManualPlay(target), "real boss fight accepted Gremlin Strike in act " + (act + 1));
                await WaitFor(tree, () => CombatManager.Instance.History.CardPlaysFinished.Any(entry => entry.CardPlay.Card == strike)
                    || !CombatManager.Instance.IsInProgress);
                await tree.ToSignal(tree.CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
            }
            await WaitFor(tree, () => NOverlayStack.Instance?.Peek() is NRewardsScreen);
            Check(run.CurrentRoom is CombatRoom { RoomType: RoomType.Boss } && !CombatManager.Instance.IsInProgress,
                "act " + (act + 1) + " boss finished and native rewards opened");
            if (act < run.Acts.Count - 1)
            {
                await manager.EnterNextAct();
                Check(run.CurrentActIndex == act + 1 && player.Creature.IsAlive, "native transition preserved living crew");
            }
        }
        await manager.EnterNextAct();
        Check(run.CurrentRoom is EventRoom { LocalMutableEvent: TheArchitect }, "native final boss reached the Architect");
        var finalEvent = ((EventRoom)run.CurrentRoom!).LocalMutableEvent;
        for (int lines = 0; lines < 30 && finalEvent.CurrentOptions.Count > 0; lines++)
            await finalEvent.CurrentOptions.Single().Chosen();
        await WaitFor(tree, () => Descendants(tree.Root).OfType<NGameOverScreen>().Any());
        Check(run.CurrentRoom!.IsVictoryRoom && manager.HasRunEnded, "Architect completed the run and opened native victory results");
        Check(SaveManager.Instance.Progress.GetStatsForCharacter(player.Character.Id)?.TotalWins == previousWins + 1,
            "native progression recorded the Gremlin Mob victory in the isolated save");
        await tree.ToSignal(tree.CreateTimer(.9), SceneTreeTimer.SignalName.Timeout);
        tree.Root.GetTexture().GetImage().SavePng(Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "mob-victory.png"));
    }
}
