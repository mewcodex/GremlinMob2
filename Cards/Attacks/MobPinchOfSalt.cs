using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public sealed class MobPinchOfSalt() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, damage: 4, damageUpgrade: 0, effect: 2, effectUpgrade: 1)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Attack(ctx, play, DamageAmount);
        if (play.Target is not { IsAlive: true } target || target.GetPowerAmount<WeakPower>() <= 0) return;
        await Apply<WeakPower>(ctx, -1, target);
        await Shivs(EffectAmount);
    }
}
