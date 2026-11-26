using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobSharpenTheLot() : DescribedMobCard(
    2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (IsUpgraded) await Draw(context, 1);
        foreach (var card in Owner.PlayerCombatState!.Hand.Cards.Where(card => card.Type == CardType.Attack))
            card.EnergyCost.SetThisTurn(0, true);
    }
}
