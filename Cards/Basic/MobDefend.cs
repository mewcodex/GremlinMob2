using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

public sealed class MobDefend() : MobCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override IEnumerable<CardTag> Tags => [CardTag.Defend];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Block(play, DynamicVars.Block.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}
