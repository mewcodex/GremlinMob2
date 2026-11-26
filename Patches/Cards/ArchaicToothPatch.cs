using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace GremlinMob2;

[HarmonyPatch(typeof(ArchaicTooth), "TranscendenceUpgrades", MethodType.Getter)]
internal static class ArchaicToothPatch
{
    private static void Postfix(Dictionary<ModelId, CardModel> __result)
    {
        __result[ModelDb.Card<MobDance>().Id] = ModelDb.Card<MobRoundDance>();
    }
}
