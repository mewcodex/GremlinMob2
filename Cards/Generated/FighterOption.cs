using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

public abstract class FighterOption : MobCard
{
    private protected FighterOption(Fighter fighter) : base(0, CardType.Skill, CardRarity.Token, TargetType.None) => Fighter = fighter;
    internal Fighter Fighter { get; }
    private (int Current, int Max)? _restSiteHealth;
    public override string Title => _restSiteHealth is { } hp ? $"{base.Title} ({hp.Current}/{hp.Max})" : base.Title;
    public override string PortraitPath => OriginalAssets.Icon(this) ?? base.PortraitPath;

    internal static FighterOption ForRestSite(Player owner, Fighter fighter, int hp)
    {
        var option = (FighterOption)For(fighter).ToMutable();
        option.Owner = owner;
        option._restSiteHealth = (hp, owner.Creature.MaxHp);
        return option;
    }
    internal static CardModel For(Fighter fighter) => fighter switch
    {
        Fighter.Brute => ModelDb.Card<MobBruteOption>(), Fighter.Bruiser => ModelDb.Card<MobBruiserOption>(),
        Fighter.Guard => ModelDb.Card<MobGuardOption>(), Fighter.Scout => ModelDb.Card<MobScoutOption>(),
        _ => ModelDb.Card<MobMageOption>()
    };
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
}
