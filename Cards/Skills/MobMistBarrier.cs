using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobMistBarrier() : DescribedMobCard(
    1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, effect: 2, costUpgrade: -1)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Apply<MobMirrorBlockPower>(ctx, EffectAmount);
}
