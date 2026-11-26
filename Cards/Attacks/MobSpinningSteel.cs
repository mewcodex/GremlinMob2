using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobSpinningSteel() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies, damage: 3, effect: 2, effectUpgrade: 1)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Attack(ctx, play, DamageAmount, EffectAmount, all: true);
}
