using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace GremlinMob2;

[HarmonyPatch(typeof(Creature), nameof(Creature.LoseHpInternal))]
internal static class ReplaceFallen
{
    private static void Postfix(Creature __instance, bool dryRun, ref HpLossResult __result)
    {
        var crew = MobRosterRelic.For(__instance.Player);
        if (!__result.killed || crew == null || ReplaceDirectlyKilled.Forced || (dryRun ? !crew.CanInterceptDeath : !crew.PreventDeath())) return;
        __result.killed = false;
        __result.overkillAmount = 0;
    }
}
