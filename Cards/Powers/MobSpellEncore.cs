using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobSpellEncore() : DescribedMobCard(
    1, CardType.Power, CardRarity.Rare, TargetType.Self, effect: 4, effectUpgrade: 2)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Apply<MobEncoreDamagePower>(ctx, EffectAmount);
        await Charge(ctx, 3);
    }
}
