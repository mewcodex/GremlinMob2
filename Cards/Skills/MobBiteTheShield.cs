using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobBiteTheShield() : DescribedMobCard(
    1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, costUpgrade: -1)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int guard = Body.Block;
        if (guard == 0) return;
        Body.LoseBlockInternal(guard);
        await Apply<MobTemporaryGuard>(ctx, guard);
    }
}
