using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace GremlinMob2;

public sealed class MobAllHands() : MobAncientCard(1, CardType.Skill, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new RepeatVar(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Enum.GetValues<Fighter>().Select(fighter =>
        (IHoverTip)new HoverTip(new LocString("powers", LegacyLocalization.FighterTitle(fighter)),
            new LocString("powers", LegacyLocalization.FighterDescription(fighter))));

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (Crew is not { } crew) return;
        var living = crew.Formation().Where(member => member.Hp > 0).ToArray();
        foreach (var member in living)
            for (int repeat = 0; repeat < DynamicVars.Repeat.IntValue; repeat++)
                await crew.TriggerBonus(context, member.Fighter, play);
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1);
}
