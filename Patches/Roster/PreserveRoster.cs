using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

[HarmonyPatch(typeof(Player), nameof(Player.RemoveRelicInternal))]
internal static class PreserveRoster
{
    private static bool Prefix(Player __instance, RelicModel relic, bool silent) =>
        silent || __instance.Character is not GremlinMobCharacter || relic is not MobRosterRelic ||
        __instance.Relics.OfType<MobRosterRelic>().Any(other => other != relic);
}
