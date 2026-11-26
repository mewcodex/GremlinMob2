using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobBarrage() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, damage: 7, damageUpgrade: 3, keyword: CardKeyword.Exhaust)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Attack(ctx, play, DamageAmount);
        await AddCards<MobBarrage>(1, IsUpgraded);
    }
}
