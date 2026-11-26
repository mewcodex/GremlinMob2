using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace GremlinMob2;

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Relics.TouchOfOrobas), "SetupForPlayer")]
internal static class PreserveRosterUpgrade
{
    private static bool Prefix(Player player, ref bool __result)
    {
        if (MobRosterRelic.For(player) == null) return true;
        __result = false;
        return false;
    }
}
