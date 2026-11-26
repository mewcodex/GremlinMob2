using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public sealed class MobBreakTheirStride() : DescribedMobCard(
    2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy, effect: 4, effectUpgrade: -1, keyword: CardKeyword.Exhaust)
{
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        play.Target is { } target && target.GetPowerAmount<WeakPower>() / EffectAmount is int turns && turns > 0
            ? Apply<MobPainSuppression>(context, turns, target) : Task.CompletedTask;
}
