using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public sealed class MobPressTheWeak() : DescribedMobCard(
    2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, damage: 4, damageUpgrade: 2)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Attack(ctx, play, DamageAmount, 1 + (play.Target?.GetPowerAmount<WeakPower>() ?? 0));
}
