using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobDoubleBlast() : DescribedMobCard(
    2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies, damage: 4, damageUpgrade: 2)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Attack(ctx, play, DamageAmount, 2, all: true);
}
