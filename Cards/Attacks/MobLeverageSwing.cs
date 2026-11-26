using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

public sealed class MobLeverageSwing() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, damage: 5, effect: 4, effectUpgrade: 2)
{
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Attack(context, play, DamageAmount);
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? source, CardPlay? play) =>
        source == this && dealer == Body && props.HasFlag(ValueProp.Move) ? (EffectAmount - 1) * Body.GetPowerAmount<StrengthPower>() : 0;
}
