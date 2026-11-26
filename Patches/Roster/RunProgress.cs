using HarmonyLib;
using System.Reflection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace GremlinMob2;

[HarmonyPatch]
internal static class NativeCharacterEpochs
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch");
        yield return AccessTools.Method(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch");
        yield return AccessTools.Method(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch");
    }

    private static bool Prefix(Player localPlayer) => localPlayer.Character is not GremlinMobCharacter;
}

[HarmonyPatch(typeof(Player), nameof(Player.FromSerializable))]
internal static class RemoveRetiredSharedContent
{
    private static void Prefix(SerializablePlayer save)
    {
        save.Relics.RemoveAll(relic => IsRetiredRelic(relic.Id?.Entry));
        save.DiscoveredRelics.RemoveAll(relic => IsRetiredRelic(relic.Entry));
        save.Deck.RemoveAll(card => card.Id?.Entry == "MOB_SUPPLY_SCROLL_CARD");
        if (save.CharacterId != ModelDb.GetId<GremlinMobCharacter>()) return;
        var crown = save.Relics.Find(relic => relic.Id == ModelDb.GetId<MobRotatingBell>());
        var starter = save.Relics.Find(relic => relic.Id == ModelDb.GetId<MobRotationBell>());
        if (crown == null || starter == null) return;
        const string rosterKey = nameof(MobRosterRelic.GremlinMobLine);
        var roster = starter.Props?.strings?.FirstOrDefault(property => property.name == rosterKey);
        if (roster is { name: rosterKey } savedRoster &&
            crown.Props?.strings?.Any(property => property.name == rosterKey) != true)
        {
            crown.Props ??= new SavedProperties();
            (crown.Props.strings ??= []).Add(savedRoster);
        }
        save.Relics.Remove(starter);
    }

    private static bool IsRetiredRelic(string? entry) => entry is "MOB_PRICKLY_GUARD" or "MOB_THIRD_TURN_SATCHEL";
}

[HarmonyPatch(typeof(TheArchitect), "WinRun")]
internal static class ArchitectWithoutCharacterDialogue
{
    private static readonly Action<EventModel, LocString, IEnumerable<EventOption>> ClearOptions =
        AccessTools.MethodDelegate<Action<EventModel, LocString, IEnumerable<EventOption>>>(
            AccessTools.Method(typeof(EventModel), "SetEventState"));

    private static bool Prefix(TheArchitect __instance, ref Task __result)
    {
        if (__instance.Owner?.Character is not GremlinMobCharacter ||
            AccessTools.Field(typeof(TheArchitect), "_dialogue").GetValue(__instance) != null) return true;
        __result = Finish(__instance);
        return false;
    }

    private static async Task Finish(TheArchitect architect)
    {
        if (LocalContext.IsMe(architect.Owner)) await RunManager.Instance.WinRun();
        ClearOptions(architect, new LocString("ancients", "PROCEED.description"), []);
    }
}
