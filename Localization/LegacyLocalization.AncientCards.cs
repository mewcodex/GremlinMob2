using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

internal static partial class LegacyLocalization
{
    private static void AddAncientCardText(Dictionary<string, string> cards, string language)
    {
        var (dance, danceText, allHands, allHandsText) = language switch
        {
            "zhs" => ("地精轮舞", "造成{Damage:diff()}点伤害。\n所有地精造成[gold]额外效果[/gold]。", "全员上阵", "依次触发每只存活地精的[gold]额外效果[/gold]各{Repeat:diff()}次。"),
            "zht" => ("地精輪舞", "造成{Damage:diff()}點傷害。\n所有地精造成[gold]額外效果[/gold]。", "全員上陣", "依次觸發每隻存活地精的[gold]額外效果[/gold]各{Repeat:diff()}次。"),
            _ => ("Gremlin Round Dance", "Deal {Damage:diff()} damage.\nAll Gremlins apply their [gold]bonus effects[/gold].",
                "All Hands", "In order, trigger each living Gremlin's [gold]bonus effect[/gold] {Repeat:diff()} times.")
        };
        string danceId = ModelDb.GetId<MobRoundDance>().Entry;
        string rallyId = ModelDb.GetId<MobAllHands>().Entry;
        cards[danceId + ".title"] = dance;
        cards[danceId + ".description"] = danceText;
        cards[rallyId + ".title"] = allHands;
        cards[rallyId + ".description"] = allHandsText;
    }
}
