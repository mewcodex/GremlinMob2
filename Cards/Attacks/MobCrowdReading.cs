using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobCrowdReading() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies, damage: 8, damageUpgrade: 3, effect: 1)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int count = CombatState!.HittableEnemies.Count(target => target.Monster?.IntendsToAttack == true);
        await Attack(ctx, play, DamageAmount, all: true);
        if (count > 0) await Charge(ctx, EffectAmount * count);
    }
}
