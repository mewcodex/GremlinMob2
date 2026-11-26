using System.Text.Json;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;

namespace GremlinMob2;

internal static partial class LegacyLocalization
{
    private const string DataPath = "res://gremlin_mob_2/original/text.json";
    private const string Energy = "{energyPrefix:energyIcons(1)}";
    private static readonly Regex RenderedMarkupOrSpace = new(
        @"(\{[^{}\r\n]*\}|\[img(?:[ =][^\]\r\n]*)?\][^\[\r\n]*\[/img\]|\[/?[a-zA-Z][a-zA-Z0-9_-]*(?:[ =][^\]\r\n]*)?\])| +",
        RegexOptions.Compiled);

    private static readonly Lazy<JsonElement> Data = new(() =>
        Godot.FileAccess.FileExists(DataPath) ? JsonDocument.Parse(Godot.FileAccess.GetFileAsString(DataPath)).RootElement : default);

    private static JsonElement Read(JsonElement source, string key) =>
        source.ValueKind == JsonValueKind.Object && source.TryGetProperty(key, out var hit) ? hit : default;
    private static string Text(JsonElement data, string field) => Read(data, field).ValueKind == JsonValueKind.String
        ? Read(data, field).GetString()! : "";
    private static string[] Lines(JsonElement data, string field) =>
        Read(data, field).ValueKind == JsonValueKind.Array
            ? Read(data, field).EnumerateArray().Select(e => e.GetString() ?? "").ToArray() : [];

    private static JsonElement Entry(string group, string key) => Entry(group, key, out _);

    private static JsonElement Entry(string group, string key, out string language)
    {
        language = LocManager.Instance?.Language ?? "eng";
        var current = Read(Read(Read(Data.Value, language), group), key);
        if (current.ValueKind != JsonValueKind.Undefined) return current;
        language = "eng";
        return Read(Read(Read(Data.Value, language), group), key);
    }

    internal static string Title(AbstractModel model, string group) => DisplayText(Text(Entry(group, model.GetType().Name), "NAME"));
    internal static string[] Ui(string key) => Lines(Entry("ui", key), "TEXT");
    private static readonly string[] FighterOrbIds = ["MadGremlin", "FatGremlin", "ShieldGremlin", "SneakyGremlin", "GremlinWizard"];
    internal static string FighterTitle(Fighter fighter) => "GREMLIN_MOB_ORB_" + FighterOrbIds[(int)fighter] + ".title";
    internal static string FighterDescription(Fighter fighter) => "GREMLIN_MOB_ORB_" + FighterOrbIds[(int)fighter] + ".description";

    internal static string Syntax(string text) => DisplayText(ConvertSyntax(text));

    private static string ConvertSyntax(string text)
    {
        text = Regex.Replace(text, @"[ \t]*\bNL\b[ \t]*", "\n");
        text = Regex.Replace(text, @"(?:(?:gremlin|expansioncontent|bronze):)([\p{L}_]+)",
            match => "[gold]" + match.Groups[1].Value.Replace('_', ' ') + "[/gold]");
        text = Regex.Replace(text, @"(?<![\p{L}])\*(?=[\p{L}])", "");
        text = text.Replace("[REMOVE_SPACE]", "").Replace("[E]", Energy);
        text = Regex.Replace(text, @"#([byrg])([^\s#]+)", match =>
        {
            string color = match.Groups[1].Value switch { "r" => "red", "g" => "green", "b" => "blue", _ => "gold" };
            return $"[{color}]{match.Groups[2].Value}[/{color}]";
        });
        return Regex.Replace(text, @"#[byrg](?=\s|$)", "").Trim();
    }

    private static string DisplayText(string text) => LocManager.Instance?.Language is "zhs" or "zht" && text.Contains(' ')
        ? RenderedMarkupOrSpace.Replace(text, match => match.Groups[1].Value)
        : text;

    internal static IEnumerable<IHoverTip> KeywordTips(MobCard card)
    {
        var source = Entry("cards", card.GetType().Name, out string language);
        string field = card is MobDance { DisplayedFighter: > 0 } dance ? $"FRONT_{dance.DisplayedFighter}_KEYWORD_TIPS" :
            card.IsUpgraded && Read(source, "UPGRADE_KEYWORD_TIPS").ValueKind == JsonValueKind.Array ? "UPGRADE_KEYWORD_TIPS" : "KEYWORD_TIPS";
        foreach (string id in Lines(source, field))
        {
            var native = NativeKeywords.FirstOrDefault(keyword => keyword.Id == id).Keyword;
            if (native != CardKeyword.None && card.Keywords.Contains(native)) continue;
            string key = "GREMLIN_MOB_KEYWORD_" + id + "_" + language;
            if (LocString.GetIfExists("static_hover_tips", key + ".description") is { } description)
                yield return new HoverTip(new LocString("static_hover_tips", key + ".title"), description);
        }
    }

    private static void LoadKeywords(LocManager loc)
    {
        var strings = new Dictionary<string, string>();
        foreach (string language in new[] { "eng", loc.Language }.Distinct())
        {
            var definitions = Read(Read(Data.Value, language), "hover_keywords");
            if (definitions.ValueKind != JsonValueKind.Object) continue;
            foreach (var definition in definitions.EnumerateObject())
            {
                string key = "GREMLIN_MOB_KEYWORD_" + definition.Name + "_" + language;
                string title = Text(definition.Value, "PROPER_NAME");
                strings[key + ".title"] = DisplayText(title.Length > 0 ? title : Lines(definition.Value, "NAMES").FirstOrDefault() ?? "");
                strings[key + ".description"] = Syntax(Text(definition.Value, "DESCRIPTION"));
            }
        }
        loc.GetTable("static_hover_tips").MergeWith(strings);
    }

    private static string Join(string[] parts, int index, string arg) =>
        parts.Length > index ? string.Concat(parts.Take(index)) + arg + parts[index] : string.Concat(parts);

    private static string RelicDescription(MobRelic relic, string[] parts) => relic switch
    {
        MobRotatingBell => Join(parts, 1, Text(Entry("relics", nameof(MobRotationBell)), "NAME")),
        MobReserveBomb => Join(parts, 1, "30"),
        MobScattershotRelic => Join(parts, 1, "3"),
        MobWoundsRelic => Join(parts, 1, "4"),
        MobPilferedGoods => Join(parts, 1, MobPilferedGoods.OriginalRage.ToString()),
        _ => parts.FirstOrDefault() ?? ""
    };

    private static string PowerDescription(MobPower power, string[] parts) => power switch
    {
        MobChargePower when parts.Length > 3 => parts[0] + "{ChargeRemaining}" + parts[2],
        MobPainSuppression when parts.Length > 2 => parts[0] + "80" + parts[1] + "{Amount}" + parts[2],
        MobRotationRhythm => Join(parts, 1, "{Amount:energyIcons}"),
        MobAttackCounterArmor when parts.Length > 3 => parts[0] + "7" + parts[2] + "{Amount}" + parts[3],
        MobTemporaryGuard or MobBlockAsDamage or MobHoldStrength or MobRampageForm => parts.FirstOrDefault() ?? "",
        _ => Join(parts, 1, "{Amount}")
    };

    internal static void Load()
    {
        if (LocManager.Instance is not { } loc || Data.Value.ValueKind == JsonValueKind.Undefined) return;
        LoadKeywords(loc);
        LoadCharacter(loc);
        LoadCards(loc);
        LoadRelics(loc);
        LoadPowers(loc);
        LoadPotions(loc);
        LoadRestSite(loc);
    }

    private static void LoadCharacter(LocManager loc)
    {
        var character = Entry("characters", "Gremlin");
        string name = DisplayText(Lines(character, "NAMES").FirstOrDefault() ?? "");
        var texts = Lines(character, "TEXT");
        string characterId = ModelDb.GetId<GremlinMobCharacter>().Entry;
        var charStrings = new Dictionary<string, string>();
        foreach (string key in new[] { "title", "shortTitle", "titleObject", "cardsModifierTitle" }) charStrings[characterId + "." + key] = name;
        foreach (string key in new[] { "description", "cardsModifierDescription" }) charStrings[characterId + "." + key] = Syntax(texts.FirstOrDefault() ?? "");
        charStrings[characterId + ".eventDeathPrevention"] = "";
        charStrings[characterId + ".bestiaryQuote"] = "";
        string neutral = ModelDb.GetId<Defect>().Entry;
        foreach (string key in new[] { "pronounSubject", "pronounObject", "pronounPossessive", "possessiveAdjective" })
            charStrings[characterId + "." + key] = LocString.GetIfExists("characters", neutral + "." + key)?.GetRawText() ?? name;
        loc.GetTable("characters").MergeWith(charStrings);
    }

    private static void LoadCards(LocManager loc)
    {
        var cards = new Dictionary<string, string>();
        var cardText = new Dictionary<string, CardText>();
        foreach (var type in Bootstrap.Cards)
        {
            var original = Entry("cards", type.Name, out string language);
            if (original.ValueKind != JsonValueKind.Object) continue;
            if (!cardText.TryGetValue(language, out var converter))
                cardText[language] = converter = new CardText(language);
            var model = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
            string id = model.Id.Entry;
            cards[id + ".title"] = DisplayText(Text(original, "NAME"));
            string raw = Text(original, "DESCRIPTION");
            string upgradeRaw = Text(original, "UPGRADE_DESCRIPTION");
            string description = converter.Render(raw, model.Keywords);
            if (model.IsUpgradable)
            {
                var upgradedModel = model.ToMutable();
                upgradedModel.UpgradeInternal(silent: true);
                string upgraded = converter.Render(upgradeRaw.Length == 0 ? raw : upgradeRaw, upgradedModel.Keywords);
                if (upgraded != description) description = "{IfUpgraded:show:" + upgraded + "|" + description + "}";
            }
            cards[id + ".description"] = description;

            if (model is MobDance)
            {
                var parts = Lines(original, "EXTENDED_DESCRIPTION");
                for (int i = 1; i < parts.Length; i++)
                    cards[id + ".front" + i] = converter.Render(parts[0] + parts[i], model.Keywords);
            }
        }
        AddAncientCardText(cards, loc.Language);
        loc.GetTable("cards").MergeWith(cards);
        loc.GetTable("card_selection").MergeWith(new Dictionary<string, string>
        {
            ["GREMLIN_MOB_CHOOSE_FRONT"] = Title(ModelDb.Card<MobPickFront>(), "cards"),
            ["GREMLIN_MOB_COPY"] = Title(ModelDb.Card<MobEchoAttack>(), "cards"),
            ["GREMLIN_MOB_FETCH"] = Title(ModelDb.Card<MobLineRhythm>(), "cards")
        });
    }

    private static void LoadRelics(LocManager loc)
    {
        var relics = new Dictionary<string, string>();
        foreach (var relic in MobRelics.Relics)
        {
            string id = relic.Id.Entry;
            var original = Entry("relics", relic.GetType().Name);
            relics[id + ".title"] = DisplayText(Text(original, "NAME"));
            relics[id + ".description"] = Syntax(RelicDescription(relic, Lines(original, "DESCRIPTIONS")));
            relics[id + ".flavor"] = Syntax(Text(original, "FLAVOR"));
        }
        loc.GetTable("relics").MergeWith(relics);
    }

    private static void LoadPowers(LocManager loc)
    {
        var powers = new Dictionary<string, string>();
        foreach (var power in MobPower.All)
        {
            if (power is MobFighterPassive) continue;
            string id = power.Id.Entry;
            var original = Entry("powers", power.GetType().Name);
            var parts = Lines(original, "DESCRIPTIONS");
            powers[id + ".title"] = DisplayText(Text(original, "NAME"));
            string description = PowerDescription(power, parts);
            if (power is MobChargePower && parts.Length > 3)
            {
                powers[id + ".ready"] = Syntax(parts[3]);
                powers[id + ".charging"] = Syntax(description);
                description = parts[0];
            }
            powers[id + ".description"] = Syntax(description);
        }
        foreach (var fighter in Enum.GetValues<Fighter>())
        {
            var orb = Entry("orbs", FighterOrbIds[(int)fighter]);
            var parts = Lines(orb, "DESCRIPTION");
            string description = fighter == Fighter.Scout && parts.Length == 3
                ? parts[0] + "2" + parts[1] + "2" + parts[2]
                : Join(parts, 1, fighter is Fighter.Brute or Fighter.Guard ? "2" : "1");
            powers[FighterTitle(fighter)] = DisplayText(Text(orb, "NAME"));
            powers[FighterDescription(fighter)] = Syntax(description);
        }
        loc.GetTable("powers").MergeWith(powers);
        loc.GetTable("powers").MergeWith(Ui("Swap").Select((line, index) =>
            KeyValuePair.Create("GREMLIN_MOB_NOB_SWAP_" + index, Syntax(line))).ToDictionary());
    }

    private static void LoadPotions(LocManager loc)
    {
        var potions = new Dictionary<string, string>();
        foreach (var potion in MobPotions.Potions)
        {
            var original = Entry("potions", potion.GetType().Name);
            var parts = Lines(original, "DESCRIPTIONS");
            string id = potion.Id.Entry;
            potions[id + ".title"] = DisplayText(Text(original, "NAME"));
            potions[id + ".description"] = Syntax(potion is MobCrewTonic ? Join(parts, 1, "2") :
                potion is MobReturningSpirit ? Join(parts, 1, "50") : parts.FirstOrDefault() ?? "");
            potions[id + ".selectionScreenPrompt"] = potions[id + ".title"];
        }
        loc.GetTable("potions").MergeWith(potions);
    }

    private static void LoadRestSite(LocManager loc)
    {
        var revive = Ui("ResurrectOption");
        var rest = Ui("RestOption");
        var restStrings = new Dictionary<string, string> { ["GREMLIN_MOB_HEAL"] = Syntax(rest.ElementAtOrDefault(4) ?? "") };
        string[] keys = ["name", "description", "descriptionDisabled"];
        for (int i = 0; i < keys.Length; i++) restStrings["OPTION_MOB_REVIVE." + keys[i]] = Syntax(revive.ElementAtOrDefault(i) ?? "");
        loc.GetTable("rest_site_ui").MergeWith(restStrings);
    }

    internal static string EventHp(decimal amount)
    {
        var parts = Ui("EventReplacementStrings");
        return parts.Length > 2 ? Syntax(parts[2] + amount + parts[1]) : "";
    }
}
