using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

public sealed class MobPaybackSwing() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, damage: 6, damageUpgrade: 2, effect: 3, effectUpgrade: -2)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Attack(ctx, play, DamageAmount, 2);
        if (play.Target is { IsAlive: true } target) await CreatureCmd.Damage(ctx, Body, EffectAmount, ValueProp.Move, target, null, null);
    }
}
