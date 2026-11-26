using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public sealed class MobRampageRoar() : DescribedMobCard(
    0, CardType.Skill, CardRarity.Token, TargetType.Self, effect: 2, effectUpgrade: 1, keyword: CardKeyword.Ethereal, secondKeyword: CardKeyword.Exhaust)
{
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Apply<StrengthPower>(context,
        EffectAmount * CombatState!.HittableEnemies.Count(creature => creature.Monster?.IntendsToAttack == false));
}
