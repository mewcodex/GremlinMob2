using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobWeakHunter() : DescribedMobCard(
    1, CardType.Power, CardRarity.Rare, TargetType.Self, effect: 3, effectUpgrade: 2)
{
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Apply<MobWeakTargetBonus>(context, EffectAmount);
}
