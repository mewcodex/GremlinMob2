using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

public sealed class MobDance() : MobCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    internal const int NobPreview = 6;
    internal int PreviewFighter { get; set; }

    // The archived descriptions use slot zero for the generic text and slots one through six for specific fighters.
    internal int DisplayedFighter
    {
        get
        {
            if (PreviewFighter != 0) return PreviewFighter;
            if (!IsMutable || Owner == null || Crew is not { } crew) return 0;
            return Body.HasPower<MobRampageForm>() ? NobPreview : (int)crew.Front + 1;
        }
    }

    public override TargetType TargetType => IsMutable && Pile?.IsCombatPile == true &&
        !Body.HasPower<MobRampageForm>() && Crew?.Front == Fighter.Brute
            ? TargetType.AllEnemies : TargetType.AnyEnemy;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move), new BlockVar(6, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool nob = Body.HasPower<MobRampageForm>();
        return PlayDance(context, play, !nob && Crew?.Front is { } front ? [front] : [], nob);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars.Block.UpgradeValueBy(3);
    }
}
