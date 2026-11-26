using Godot;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

public sealed class MobRelics : RelicPoolModel
{
    internal static MobRelic[] Relics =>
        [ModelDb.Relic<MobRotationBell>(), ModelDb.Relic<MobReserveTalisman>(),
        ModelDb.Relic<MobReserveBomb>(), ModelDb.Relic<MobChargeMallet>(),
        ModelDb.Relic<MobSpellStaff>(), ModelDb.Relic<MobRotatingBell>(), ModelDb.Relic<MobRecruitPennant>(), ModelDb.Relic<MobSteadyMuscle>(), ModelDb.Relic<MobScattershotRelic>(),
        ModelDb.Relic<MobSpareGrave>(), ModelDb.Relic<MobSpellHat>(), ModelDb.Relic<MobWoundsRelic>(), ModelDb.Relic<MobPledgedMember>(), ModelDb.Relic<MobPilferedGoods>()];

    public override string EnergyColorName => ModelDb.CardPool<MobCards>().EnergyColorName;
    public override Color LabOutlineColor => new("b9d686");
    protected override IEnumerable<RelicModel> GenerateAllRelics() => Relics;
}
