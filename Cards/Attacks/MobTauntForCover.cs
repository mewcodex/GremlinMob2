using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobTauntForCover() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, damage: 7, damageUpgrade: 4, effect: 2, effectUpgrade: 2)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        bool debuff = play.Target?.Powers.Any(power => power.Type == PowerType.Debuff) == true;
        await Attack(ctx, play, DamageAmount);
        if (debuff) await Apply<MobTemporaryGuard>(ctx, EffectAmount);
    }
}
