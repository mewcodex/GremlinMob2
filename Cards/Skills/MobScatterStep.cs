using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobScatterStep() : DescribedMobCard(
    2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, costUpgrade: -1, keyword: CardKeyword.Exhaust)
{
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Apply<MobEvadeAndRotate>(context, 1);
}
