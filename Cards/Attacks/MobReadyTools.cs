using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobReadyTools() : DescribedMobCard(
    2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, damage: 5, damageUpgrade: 3, effect: 3, effectUpgrade: 1)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        bool attacking = play.Target?.Monster?.IntendsToAttack == true;
        await Attack(ctx, play, DamageAmount);
        if (attacking) await Wards(EffectAmount); else await Shivs(EffectAmount);
    }
}
