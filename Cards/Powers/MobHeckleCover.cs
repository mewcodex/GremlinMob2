using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobHeckleCover() : DescribedMobCard(
    2, CardType.Power, CardRarity.Rare, TargetType.Self, effect: 2, costUpgrade: -1)
{
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Apply<MobHecklingCover>(context, EffectAmount);
}
