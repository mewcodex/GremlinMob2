using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public sealed class MobTripToe() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, damage: 5, damageUpgrade: 3, effect: 2, effectUpgrade: 1)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Attack(ctx, play, DamageAmount);
        if (play.Target is { IsAlive: true } target && target.GetPowerAmount<WeakPower>() > 0) await Apply<VulnerablePower>(ctx, EffectAmount, target);
    }
}
