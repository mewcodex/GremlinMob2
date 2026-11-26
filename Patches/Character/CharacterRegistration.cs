using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
internal static class CharacterRegistration
{
    private static void Postfix(ref IEnumerable<CharacterModel> __result)
    {
        if (ModelDb.GetByIdOrNull<CharacterModel>(ModelDb.GetId<GremlinMobCharacter>()) is { } character &&
            !__result.Any(model => model.Id == character.Id))
            __result = __result.Append(character);
    }
}
