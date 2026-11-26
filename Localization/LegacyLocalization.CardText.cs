using System.Text.Json;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace GremlinMob2;

internal static partial class LegacyLocalization
{
    private static readonly (CardKeyword Keyword, string Id)[] NativeKeywords =
    [
        (CardKeyword.Exhaust, "base_exhaust"), (CardKeyword.Ethereal, "base_ethereal"),
        (CardKeyword.Innate, "base_innate"), (CardKeyword.Retain, "base_retain"),
        (CardKeyword.Unplayable, "base_unplayable")
    ];

    private static readonly string[] HighlightedBaseTerms =
    [
        "artifact", "block", "dexterity", "ethereal", "exhaust", "frail", "innate", "intangible",
        "poison", "retain", "shiv", "strength", "thorns", "unplayable", "upgrade", "vulnerable", "weak"
    ];

    private static readonly Regex CardMarkup = new(
        @"(\{[^{}]+\}|\[(gold|red|green|blue|img|hint)(?:[ =][^\]]*)?\].*?\[/\2\])", RegexOptions.Compiled);

    private sealed class CardText
    {
        private readonly Dictionary<CardKeyword, Regex> _keywordSentences = new();
        private readonly Regex _highlights;

        internal CardText(string language)
        {
            var definitions = Read(Read(Data.Value, language), "hover_keywords");
            foreach (var (keyword, id) in NativeKeywords)
            {
                string[] aliases = (language, keyword) switch
                {
                    ("jpn", CardKeyword.Ethereal) => ["エセリアル"],
                    ("ptb", CardKeyword.Ethereal) => ["Eterno"],
                    ("ptb", CardKeyword.Unplayable) => ["Não pode ser jogado", "Não jogável"],
                    ("zht", CardKeyword.Unplayable) => ["不能被打出"],
                    _ => []
                };
                string pattern = Alternatives(Lines(Read(definitions, id), "NAMES").Concat(aliases).Append(id[5..]));
                _keywordSentences[keyword] = new Regex(@"(?:\A|(?<=[.!?。！？\n]))[ \t]*\*?(?:" + pattern +
                    @")[ \t]*(?:[.!?。！？]+[ \t]*(?:\n|$)?|\n|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            string[] extra = language switch
            {
                "eng" => ["bonus effects", "bonus effect", "bonus", "hand", "discard pile", "draw pile", "gold",
                    "temporary Strength", "temporary Thorns"],
                "zhs" => ["额外效果", "特性", "手牌", "抽牌堆", "弃牌堆", "消耗牌堆", "窃取", "检索", "预见", "灵魂", "金币"],
                "zht" => ["額外效果", "特性", "切換", "臨時生命", "手牌", "抽牌堆", "棄牌堆", "消耗牌堆", "竊取", "檢索", "預見", "靈魂", "金幣"],
                _ => []
            };
            var terms = definitions.ValueKind == JsonValueKind.Object
                ? definitions.EnumerateObject().Where(entry => entry.Name.StartsWith("gremlin_") ||
                    entry.Name.StartsWith("expansioncontent_") || entry.Name == "bronze_insert" ||
                    entry.Name.StartsWith("base_") && HighlightedBaseTerms.Contains(entry.Name[5..]))
                    .SelectMany(entry => Lines(entry.Value, "NAMES"))
                : [];
            string words = Alternatives(terms.Concat(extra).Where(term => !term.Contains('_')));
            if (words.Length > 0 && language is not ("zhs" or "zht" or "jpn" or "kor"))
                words = @"(?<![\p{L}\p{N}_])(?:" + words + @")(?![\p{L}\p{N}_])";
            // The native diff formatter still owns all dynamic amounts and upgrade colors.
            const string numbers = @"(?<![\d.])\d+(?:\.\d+)?%?(?![\d.]*\d)";
            _highlights = new Regex(words.Length == 0 ? numbers : words + "|" + numbers,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static string Alternatives(IEnumerable<string> terms) => string.Join('|', terms
            .Where(term => !string.IsNullOrWhiteSpace(term)).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(term => term.Length).Select(Regex.Escape));

        internal string Render(string text, IEnumerable<CardKeyword> keywords)
        {
            text = Regex.Replace(text, @"[ \t]*\bNL\b[ \t]*", "\n");
            foreach (var keyword in keywords)
                if (_keywordSentences.TryGetValue(keyword, out var pattern)) text = pattern.Replace(text, "");
            text = Regex.Replace(text, @"guardianmod:((?:临时|臨時)[ \t]+[\p{L}]+)", "[gold]$1[/gold]");
            text = Regex.Replace(text, @"(?<![\p{L}])\*([\p{L}\p{M}\p{N}_+'’-]+(?:[ \t]+\*[\p{L}\p{M}\p{N}_+'’-]+)*(?:[ \t]+\+)?)",
                match => "[gold]" + match.Groups[1].Value.Replace("*", "") + "[/gold]");
            text = ConvertSyntax(text.Replace("!D!", "{Damage:diff()}").Replace("!B!", "{Block:diff()}")
                .Replace("!M!", "{OriginalMagic:diff()}").Replace("!blamage!", "{Gold:diff()}"));
            int position = 0;
            var result = new System.Text.StringBuilder();
            foreach (Match markup in CardMarkup.Matches(text))
            {
                result.Append(_highlights.Replace(text[position..markup.Index], Gold));
                result.Append(markup.Value);
                position = markup.Index + markup.Length;
            }
            result.Append(_highlights.Replace(text[position..], Gold));
            return DisplayText(result.ToString());
        }

        private static string Gold(Match match) => "[gold]" + match.Value + "[/gold]";
    }
}
