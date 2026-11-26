using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobHandoffTempo() : DescribedMobCard(
    2, CardType.Power, CardRarity.Uncommon, TargetType.Self, costUpgrade: -1)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Apply<MobSwapDraw>(ctx, 1);
}
