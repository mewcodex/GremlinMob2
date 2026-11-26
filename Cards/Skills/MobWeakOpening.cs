using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public sealed class MobWeakOpening() : DescribedMobCard(
    1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, effect: 1, effectUpgrade: 1, block: 9, blockUpgrade: 3)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target is not { } target) return;
        await Apply<WeakPower>(ctx, EffectAmount, target);
        if (target.GetPowerAmount<WeakPower>() >= 3) await Block(play, BlockAmount);
    }
}
