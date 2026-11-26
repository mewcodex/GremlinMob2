using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobShortFuse() : DescribedMobCard(
    0, CardType.Skill, CardRarity.Common, TargetType.Self, effect: 2, effectUpgrade: 2)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Apply<MobMadStrength>(ctx, EffectAmount);
}
