using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;

namespace GremlinMob2;

[HarmonyPatch(typeof(AncientEventModel), "GenerateInitialOptionsWrapper")]
internal static class MobAncientRelics
{
    private static readonly Func<AncientEventModel, RelicModel, string, EventOption> NativeOption =
        AccessTools.MethodDelegate<Func<AncientEventModel, RelicModel, string, EventOption>>(
            AccessTools.Method(typeof(AncientEventModel), "RelicOption", [typeof(RelicModel), typeof(string)]));

    private static void Postfix(AncientEventModel __instance, IReadOnlyList<EventOption> __result)
    {
        if (__instance is Neow || __instance.DebugOption != null || __result is not List<EventOption> options || options.Count == 0
            || __instance.Owner is not { } player || MobRosterRelic.For(player) is not { } crew) return;

        RelicModel[] choices = [ModelDb.Relic<MobRotatingBell>(), ModelDb.Relic<MobReserveTalisman>(), ModelDb.Relic<MobPledgedMember>()];
        var available = choices.Where(relic => player.Relics.All(owned => owned.Id != relic.Id)
            && (relic is not MobPledgedMember || crew.Reserves.Length > 0)
            && (relic is not MobRotatingBell || crew is MobRotationBell)).ToArray();
        var relic = player.RunState.Rng.Niche.NextItem(available);
        int index = options.FindLastIndex(option => option.Relic is not (ArchaicTooth or DustyTome));
        if (relic != null && index >= 0)
            options[index] = NativeOption(__instance, relic.ToMutable(), "GREMLIN_MOB");
    }
}
