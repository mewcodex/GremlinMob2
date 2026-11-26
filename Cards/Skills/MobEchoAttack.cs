using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobEchoAttack() : DescribedMobCard(
    2, CardType.Skill, CardRarity.Rare, TargetType.Self, effect: 2, keyword: CardKeyword.Exhaust, removeOnUpgrade: CardKeyword.Exhaust)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var selection = await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(new MegaCrit.Sts2.Core.Localization.LocString("card_selection", "GREMLIN_MOB_COPY"), 1),
            card => card.Type == CardType.Attack, this);
        if (selection.FirstOrDefault() is not { } chosen) return;
        var copies = Enumerable.Range(0, EffectAmount).Select(_ => chosen.CreateClone()).ToArray();
        foreach (var copy in copies)
        {
            copy.AddKeyword(CardKeyword.Exhaust);
            copy.AddKeyword(CardKeyword.Ethereal);
            copy.EnergyCost.AddThisCombat(-1);
        }
        await CardPileCmd.AddGeneratedCardsToCombat(copies, PileType.Hand, Owner);
    }
}
