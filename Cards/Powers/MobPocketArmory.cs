using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobPocketArmory() : DescribedMobCard(
    1, CardType.Power, CardRarity.Uncommon, TargetType.Self, upgradeKeyword: CardKeyword.Innate)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Apply<MobWardMakerPower>(ctx, 1);
}
