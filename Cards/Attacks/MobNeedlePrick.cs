using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

public sealed class MobNeedlePrick() : DescribedMobCard(
    0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, damage: 3, damageUpgrade: 0, keyword: CardKeyword.Exhaust)
{
    public override async Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw)
    {
        if (card != this || !IsUpgraded || CloneOf != null) return;
        var copy = CreateClone();
        copy.AddKeyword(CardKeyword.Ethereal);
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Attack(ctx, play, DamageAmount);
        await Draw(ctx, 1);
    }
}
