using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobBurst() : DescribedMobCard(
    0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, damage: 0, damageUpgrade: 0, effect: 3, effectUpgrade: 1, keyword: CardKeyword.Exhaust)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Attack(ctx, play, DamageAmount, EffectAmount);
        await AddCards<MobDelayedSpark>(1, IsUpgraded, PileType.Discard);
    }
}
