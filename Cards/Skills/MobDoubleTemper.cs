using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public sealed class MobDoubleTemper() : DescribedMobCard(
    2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, blockUpgrade: 5, keyword: CardKeyword.Exhaust)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (BlockAmount > 0) await Block(play, BlockAmount);
        int strength = Body.GetPowerAmount<StrengthPower>();
        if (strength != 0) await Apply<StrengthPower>(ctx, strength);
    }
}
