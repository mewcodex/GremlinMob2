using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobFullRotation() : DescribedMobCard(
    2, CardType.Power, CardRarity.Rare, TargetType.Self, costUpgrade: -1)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Apply<MobRotationRhythm>(ctx, 1);
}
