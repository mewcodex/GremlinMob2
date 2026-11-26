using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

public sealed class MobGuardfire() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, damage: 11, damageUpgrade: 4)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play, DamageAmount);
        await Apply<MobBlockAsDamage>(context, 1);
    }
    public override Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw) =>
        card == this ? Draw(context, 2) : Task.CompletedTask;
}
