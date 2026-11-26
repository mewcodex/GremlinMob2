using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobBracedBruiser() : DescribedMobCard(
    1, CardType.Skill, CardRarity.Common, TargetType.Self, effect: 5, effectUpgrade: 2)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Apply<MobTemporaryGuard>(ctx, EffectAmount);
        await Rotate(ctx, Fighter.Bruiser);
    }
}
