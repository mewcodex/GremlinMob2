using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobSteadySpell() : DescribedMobCard(
    1, CardType.Skill, CardRarity.Common, TargetType.Self, block: 8, blockUpgrade: 2, effect: 1, effectUpgrade: 1)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Block(play, BlockAmount);
        await Charge(ctx, EffectAmount);
    }
}
