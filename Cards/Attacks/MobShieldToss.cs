using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobShieldToss() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, effect: 3, costUpgrade: -1)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<MobTemporaryGuard>(context, EffectAmount);
        await Attack(context, play, Body.Block + Body.GetPowerAmount<MobTemporaryGuard>());
    }
}
