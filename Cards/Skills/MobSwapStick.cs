using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobSwapStick() : DescribedMobCard(
    -2, CardType.Skill, CardRarity.Uncommon, TargetType.None, keyword: CardKeyword.Unplayable, secondKeyword: CardKeyword.Ethereal, removeOnUpgrade: CardKeyword.Ethereal)
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Task.CompletedTask;
}
