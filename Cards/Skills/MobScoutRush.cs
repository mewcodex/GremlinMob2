using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobScoutRush() : DescribedMobCard(
    1, CardType.Skill, CardRarity.Common, TargetType.Self, effect: 4, effectUpgrade: 2)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var cards = await CardPileCmd.Draw(ctx, EffectAmount, Owner);
        await CardCmd.Discard(ctx, cards.Where(card => card.Type != CardType.Attack && card.Pile?.Type == PileType.Hand));
        await Rotate(ctx, Fighter.Scout);
    }
}
