using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobPreparedWard() : DescribedMobCard(
    0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, block: 5, blockUpgrade: 2, effect: 2)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Block(play, BlockAmount);
        if (ChargeCount >= 3) await Wards(EffectAmount, IsUpgraded);
    }
}
