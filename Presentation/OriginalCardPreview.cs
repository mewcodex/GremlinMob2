using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace GremlinMob2;

internal static class OriginalCardPreview
{
    private sealed class RotatingPreview(CardModel[] cards) : CardHoverTip(cards[0])
    {
        internal CardModel[] Cards { get; } = cards;
    }

    internal static IEnumerable<IHoverTip> GetTips(MobCard card) => card switch
    {
        MobDelayedSpark => [HoverTipFactory.FromCard<MobBurst>(card.IsUpgraded)],
        MobBurst => [HoverTipFactory.FromCard<MobDelayedSpark>(card.IsUpgraded)],
        MobHideNeedles => [HoverTipFactory.FromCard<MobNeedlePrick>(card.IsUpgraded)],
        MobPocketArmory => [HoverTipFactory.FromCard<MobPaperWard>(upgrade: true)],
        MobPocketPowder => [HoverTipFactory.FromCard<MobPaperWard>()],
        MobWardedBlade or MobBrushGuard or MobPreparedWard or MobMageEntrance =>
            [HoverTipFactory.FromCard<MobPaperWard>(card.IsUpgraded)],
        MobBarrage => [HoverTipFactory.FromCard<MobBarrage>(card.IsUpgraded)],
        MobHookedBlade or MobSpareVolley or MobPocketAces => [HoverTipFactory.FromCard<Shiv>(card.IsUpgraded)],
        MobPinchOfSalt or MobShadowWorkshop or MobKnifeStone => [HoverTipFactory.FromCard<Shiv>()],
        MobHoneTools or MobReadyTools => [HoverTipFactory.FromCard<Shiv>(), HoverTipFactory.FromCard<MobPaperWard>()],
        MobDance { PreviewFighter: 0 } => [new RotatingPreview(Enumerable.Range(1, 5).Select(Dance).Cast<CardModel>().ToArray())],
        MobRoundDance => [new RotatingPreview(Enumerable.Range(1, MobDance.NobPreview)
            .Select(fighter => Dance(fighter, card.IsUpgraded)).Cast<CardModel>().ToArray())],
        MobJuggernaut => [new RotatingPreview([ModelDb.Card<MobRampageRoar>(), ModelDb.Card<MobRampageBash>(), ModelDb.Card<MobRampageRush>(), Dance(MobDance.NobPreview)])],
        MobArms => Arms(card),
        _ => []
    };

    private static MobDance Dance(int fighter) => Dance(fighter, upgraded: false);

    private static MobDance Dance(int fighter, bool upgraded)
    {
        var card = (MobDance)ModelDb.Card<MobDance>().ToMutable();
        card.PreviewFighter = fighter;
        if (upgraded) card.UpgradeInternal(silent: true);
        return card;
    }

    private static IEnumerable<IHoverTip> Arms(MobCard card)
    {
        var roster = card.IsMutable ? MobRosterRelic.For(card.Owner) : null;
        var fighters = roster?.Reserves ?? Enum.GetValues<Fighter>();
        if (fighters.Length > 0)
            yield return new RotatingPreview(fighters.Select(FighterOption.For).ToArray());
    }

    [HarmonyPatch(typeof(NHoverTipCardContainer), nameof(NHoverTipCardContainer.Add))]
    private static class Cycle
    {
        private static void Postfix(NHoverTipCardContainer __instance, CardHoverTip cardTip)
        {
            if (cardTip is not RotatingPreview { Cards.Length: > 1 } preview ||
                __instance.GetChildren().OfType<Control>().LastOrDefault()?.GetNodeOrNull<NCard>("%Card") is not { } view) return;
            var timer = new Godot.Timer { Name = "GremlinPreviewTimer", WaitTime = .9 };
            view.AddChild(timer);
            int index = 0;
            timer.Timeout += () =>
            {
                index = (index + 1) % preview.Cards.Length;
                var next = preview.Cards[index];
                view.Model = next.IsMutable ? next : next.ToMutable();
                view.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
            };
            timer.Start();
        }
    }
}
