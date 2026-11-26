using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobSpareVolley() : DescribedMobCard(
    2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, damage: 4, damageUpgrade: 2)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Attack(ctx, play, DamageAmount);
        await Shivs(Math.Min(CardsPlayed, 10 - Owner.PlayerCombatState!.Hand.Cards.Count), IsUpgraded);
    }
}
