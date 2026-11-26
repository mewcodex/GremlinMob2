using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobOpenTheWound() : DescribedMobCard(
    2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, damage: 8, costUpgrade: -1, keyword: CardKeyword.Exhaust)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play, DamageAmount);
        if (play.Target is { IsAlive: true } target) await Apply<MobWeakPunishment>(context, 3, target);
    }
}
