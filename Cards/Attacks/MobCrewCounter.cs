using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobCrewCounter() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, damage: 8, damageUpgrade: 2, effect: 2, effectUpgrade: 1)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        bool attacking = play.Target?.Monster?.IntendsToAttack == true;
        await Attack(ctx, play, DamageAmount);
        if (!attacking || Body.HasPower<MobRampageForm>() || Crew is not { } crew) return;
        for (int i = 0; i < EffectAmount; i++) await crew.TriggerBonus(ctx, crew.Front, play);
    }
}
