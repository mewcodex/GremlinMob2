using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public sealed class MobPatchworkArmor() : DescribedMobCard(
    1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (IsUpgraded) await Apply<ArtifactPower>(context, 1);
        await Apply<MobAttackCounterArmor>(context, 1);
    }
}
