using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobDelayedSpark() : DescribedMobCard(
    0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, effect: 1, effectUpgrade: 1, keyword: CardKeyword.Exhaust)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Charge(ctx, EffectAmount);
        await Apply<MobWhizReturnPower>(ctx, 1);
        if (IsUpgraded) Body.GetPower<MobWhizReturnPower>()?.AddUpgrade();
    }
}
