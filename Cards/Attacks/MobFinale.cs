using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobFinale() : DescribedMobCard(
    0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies, damage: 3, effect: 5, effectUpgrade: 1)
{
    protected override bool IsPlayable => ChargeCount == 7;
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Attack(ctx, play, DamageAmount, EffectAmount, all: true);
}
