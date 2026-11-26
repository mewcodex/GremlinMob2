using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

public sealed class MobRoundDance() : MobAncientCard(0, CardType.Attack, TargetType.AnyEnemy)
{
    public override TargetType TargetType => IsMutable && Pile?.IsCombatPile == true &&
        Dancers(Body.HasPower<MobRampageForm>()).Contains(Fighter.Brute) ? TargetType.AllEnemies : TargetType.AnyEnemy;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move), new BlockVar(6, ValueProp.Move)];

    private Fighter[] Dancers(bool nob) => Crew is { } crew
        ? crew.Formation().Where(member => member.Hp > 0 && (!nob || member.Fighter != crew.Front))
            .Select(member => member.Fighter).ToArray()
        : [];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool nob = Body.HasPower<MobRampageForm>();
        return PlayDance(context, play, Dancers(nob), nob);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars.Block.UpgradeValueBy(3);
    }
}
