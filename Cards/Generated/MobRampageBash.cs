using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public sealed class MobRampageBash() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, damage: 6, damageUpgrade: 2, effect: 2,
    keyword: CardKeyword.Ethereal, secondKeyword: CardKeyword.Exhaust)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play, DamageAmount);
        if (play.Target is { IsAlive: true } target) await Apply<VulnerablePower>(context, EffectAmount, target);
    }
}
