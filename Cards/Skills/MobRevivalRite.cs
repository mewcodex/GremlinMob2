using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobRevivalRite() : DescribedMobCard(
    1, CardType.Skill, CardRarity.Rare, TargetType.Self, effect: 10, effectUpgrade: 3, keyword: CardKeyword.Exhaust)
{
    protected override bool IsPlayable => ChargeCount >= 3 && Crew?.HasFallen == true;
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Crew is { } crew && crew.ReviveRandom(EffectAmount)) await crew.SpendCharge(ctx, ChargeCount);
    }
}
