using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobQuickFlurry() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, damage: 3, damageUpgrade: 0, costUpgrade: -1, keyword: CardKeyword.Exhaust)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Attack(ctx, play, DamageAmount, Math.Max(1, CardsPlayed));
}
