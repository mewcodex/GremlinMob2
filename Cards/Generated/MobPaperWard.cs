using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobPaperWard() : DescribedMobCard(
    0, CardType.Skill, CardRarity.Token, TargetType.Self, block: 3, blockUpgrade: 2, keyword: CardKeyword.Exhaust)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Block(play, BlockAmount);
}
