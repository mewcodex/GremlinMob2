using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AttackSfx), MethodType.Getter)]
internal static class NativeAttackSound
{
    private static void Postfix(CharacterModel __instance, ref string __result)
    {
        if (__instance is GremlinMobCharacter) __result = GremlinMobCharacter.Placeholder.AttackSfx;
    }
}
