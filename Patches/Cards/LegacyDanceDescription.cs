using MegaCrit.Sts2.Core.Localization;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class LegacyDanceDescription
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is MobDance { DisplayedFighter: > 0 } dance)
            __result = LocString.GetIfExists("cards", dance.Id.Entry + ".front" + dance.DisplayedFighter) ?? __result;
    }
}
