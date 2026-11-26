using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobBrushGuard() : DescribedMobCard(
    1, CardType.Skill, CardRarity.Common, TargetType.Self, effect: 2)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Wards(EffectAmount, IsUpgraded);
        await Rotate(ctx, Fighter.Guard);
    }
}
