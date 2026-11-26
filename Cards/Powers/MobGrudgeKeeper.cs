using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobGrudgeKeeper() : DescribedMobCard(
    3, CardType.Power, CardRarity.Rare, TargetType.Self, costUpgrade: -1)
{
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Apply<MobHoldStrength>(context, 1);
}
