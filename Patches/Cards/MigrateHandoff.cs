using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace GremlinMob2;

[HarmonyPatch(typeof(SaveUtil), nameof(SaveUtil.CardOrDeprecated))]
internal static class MigrateHandoff
{
    private static void Prefix(ref ModelId id)
    {
        if (id == new ModelId("CARD", "MOB_PERFECT_HANDOFF")) id = ModelDb.GetId<MobRoundDance>();
    }
}
