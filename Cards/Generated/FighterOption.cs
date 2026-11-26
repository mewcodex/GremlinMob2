using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

public abstract class FighterOption : MobCard
{
    private protected FighterOption(Fighter fighter) : base(0, CardType.Skill, CardRarity.Token, TargetType.None) => Fighter = fighter;
    internal Fighter Fighter { get; }
    public override string PortraitPath => OriginalAssets.Icon(this) ?? base.PortraitPath;
    internal static CardModel For(Fighter fighter) => fighter switch
    {
        Fighter.Brute => ModelDb.Card<MobBruteOption>(), Fighter.Bruiser => ModelDb.Card<MobBruiserOption>(),
        Fighter.Guard => ModelDb.Card<MobGuardOption>(), Fighter.Scout => ModelDb.Card<MobScoutOption>(),
        _ => ModelDb.Card<MobMageOption>()
    };
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
}
