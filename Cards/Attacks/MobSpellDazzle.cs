using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public sealed class MobSpellDazzle() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, damage: 9, damageUpgrade: 4, keyword: CardKeyword.Exhaust)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool ready = ChargeCount >= 3;
        await Attack(context, play, DamageAmount);
        if (!ready || play.Target is not { IsAlive: true } target) return;
        await Apply<StrengthPower>(context, -2, target);
        await Apply<StrengthPower>(context, 2);
    }
}
