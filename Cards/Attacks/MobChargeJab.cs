using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobChargeJab() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, damage: 9, damageUpgrade: 2, effect: 1, effectUpgrade: 1)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Attack(ctx, play, DamageAmount);
        await Charge(ctx, EffectAmount);
    }
}
