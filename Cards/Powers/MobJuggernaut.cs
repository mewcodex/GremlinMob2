using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobJuggernaut() : DescribedMobCard(
    4, CardType.Power, CardRarity.Rare, TargetType.Self, effect: 20, upgradeKeyword: CardKeyword.Innate)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<MobTemporaryGuard>(context, EffectAmount);
        await Apply<MobRampageForm>(context, 1);
    }
}
