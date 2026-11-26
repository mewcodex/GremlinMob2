using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobArms() : DescribedMobCard(
    0, CardType.Skill, CardRarity.Common, TargetType.Self, effect: 1, effectUpgrade: 1)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Crew is not { } crew) return;
        if (!Body.HasPower<MobRampageForm>())
            for (int i = 0; i < EffectAmount; i++)
                await crew.TriggerBonus(ctx, crew.Front, play);
        await ChooseFighter(ctx);
    }
}
