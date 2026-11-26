using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobRakingClaws() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, damage: 2, effect: 3, effectUpgrade: 1)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Attack(ctx, play, DamageAmount, EffectAmount);
}
