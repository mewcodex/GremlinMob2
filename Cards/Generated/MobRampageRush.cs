using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobRampageRush() : DescribedMobCard(
    1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, damage: 14, damageUpgrade: 2,
    keyword: CardKeyword.Ethereal, secondKeyword: CardKeyword.Exhaust)
{
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Attack(context, play, DamageAmount);
}
