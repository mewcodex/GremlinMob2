using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobBraceStrike() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, damage: 7, costUpgrade: -1, block: 7)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int bonus = ChargeCount >= 3 ? MobChargePower.Bonus(Body) : 0;
        await Block(play, BlockAmount + bonus);
        await Attack(ctx, play, DamageAmount);
    }
}
