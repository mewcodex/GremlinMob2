using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobHoneTools() : DescribedMobCard(
    1, CardType.Power, CardRarity.Uncommon, TargetType.Self, effect: 2, effectUpgrade: 1)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Apply<MobWardPolishPower>(ctx, EffectAmount);
}
