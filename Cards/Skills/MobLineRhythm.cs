using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobLineRhythm() : DescribedMobCard(
    1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, costUpgrade: -1)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Rotate(ctx);
        var cards = await CardSelectCmd.FromCombatPile(ctx, Owner.PlayerCombatState!.DrawPile, Owner, new CardSelectorPrefs(new MegaCrit.Sts2.Core.Localization.LocString("card_selection", "GREMLIN_MOB_FETCH"), 1), card => card.Rarity == CardRarity.Basic);
        if (cards.FirstOrDefault() is not { } chosen) return;
        chosen.EnergyCost.SetThisTurn(0);
        await CardPileCmd.Add(chosen, PileType.Hand);
    }
}
