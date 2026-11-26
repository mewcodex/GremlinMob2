using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public sealed class MobDisarmTheft() : DescribedMobCard(
    2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, effect: 1, effectUpgrade: 1, keyword: CardKeyword.Exhaust)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target is not { } target) return;
        await Apply<WeakPower>(ctx, EffectAmount, target);
        await StealStrength(ctx, target, EffectAmount);
    }
}
