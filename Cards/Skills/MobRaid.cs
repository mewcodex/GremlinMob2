using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobRaid() : DescribedMobCard(
    1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, effect: 2, effectUpgrade: 1)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var cards = Owner.PlayerCombatState!.DrawPile.Cards.Take(Crew?.Living ?? 1).ToArray();
        if (cards.Length > 0)
        {
            var selection = await CardSelectCmd.FromSimpleGrid(ctx, cards, Owner,
                new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, cards.Length));
            await CardPileCmd.Add(selection, PileType.Discard);
        }
        await Draw(ctx, EffectAmount);
    }
}
