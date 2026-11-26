using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public sealed class MobPoweredFury() : DescribedMobCard(
    4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, damage: 5, damageUpgrade: 2)
{
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Attack(context, play, DamageAmount, 3);
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = card == this ? Math.Max(0, originalCost - Math.Max(0, Body.GetPowerAmount<StrengthPower>()) / 2) : originalCost;
        return modifiedCost != originalCost;
    }
}
