using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobPocketPowder() : DescribedMobCard(
    0, CardType.Skill, CardRarity.Rare, TargetType.Self, effect: 2, effectUpgrade: 2, keyword: CardKeyword.Exhaust)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await AddCards<MobPaperWard>(2, pile: PileType.Discard);
        await Draw(ctx, EffectAmount);
    }
}
