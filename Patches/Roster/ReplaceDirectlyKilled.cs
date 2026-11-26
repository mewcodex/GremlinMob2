using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace GremlinMob2;

[HarmonyPatch(typeof(CreatureCmd), "KillWithoutCheckingWinCondition")]
internal static class ReplaceDirectlyKilled
{
    private static readonly AsyncLocal<bool> ForceKill = new();
    internal static bool Forced => ForceKill.Value;

    private static bool Prefix(Creature creature, bool force, ref Task __result, out bool __state)
    {
        __state = ForceKill.Value;
        if (force)
        {
            ForceKill.Value = true;
            return true;
        }
        if (MobRosterRelic.For(creature.Player) is not { } crew || !crew.PreventDeath()) return true;
        __result = crew.AfterFallen(new ThrowingPlayerChoiceContext());
        return false;
    }

    private static void Finalizer(bool __state) => ForceKill.Value = __state;
}
