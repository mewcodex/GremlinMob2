using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

public sealed class MobKnifeStone() : DescribedMobCard(
    -2, CardType.Skill, CardRarity.Common, TargetType.None, effect: 2, effectUpgrade: 1, keyword: CardKeyword.Unplayable)
{
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
    public override Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw) =>
        card == this ? Shivs(EffectAmount) : Task.CompletedTask;
}
