using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobPickFront() : MobCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => ChooseFighter(context);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
