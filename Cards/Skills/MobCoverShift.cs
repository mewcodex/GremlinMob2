using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobCoverShift() : DescribedMobCard(
    0, CardType.Skill, CardRarity.Common, TargetType.Self, block: 4, blockUpgrade: 2)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Block(play, BlockAmount);
        await Rotate(ctx);
    }
}
