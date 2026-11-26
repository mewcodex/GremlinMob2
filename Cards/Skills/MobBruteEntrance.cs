using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

public sealed class MobBruteEntrance() : DescribedMobCard(
    1, CardType.Skill, CardRarity.Common, TargetType.Self, block: 6, blockUpgrade: 2, effect: 3, effectUpgrade: 2)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Block(play, BlockAmount);
        await Apply<MobTemporaryThorns>(ctx, EffectAmount);
        await Rotate(ctx, Fighter.Brute);
    }
}
