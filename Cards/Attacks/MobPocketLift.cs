using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobPocketLift() : DescribedMobCard(
    2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, damage: 10, damageUpgrade: 4, keyword: CardKeyword.Exhaust)
{
    protected override IEnumerable<MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar> CanonicalVars =>
        base.CanonicalVars.Append(new MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar("Gold", 10));
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Gold"].UpgradeValueBy(5);
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play, DamageAmount);
        await PlayerCmd.GainGold(DynamicVars["Gold"].BaseValue, Owner);
    }
}
