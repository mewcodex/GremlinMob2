using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobStockade() : DescribedMobCard(
    1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, effect: 2)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (IsUpgraded) await Draw(ctx, 1);
        await Block(play, EffectAmount * Owner.PlayerCombatState!.Hand.Cards.Count(card => card != this));
    }
}
