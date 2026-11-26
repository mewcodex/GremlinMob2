using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace GremlinMob2;

[HarmonyPatch]
internal static class SharedMaxHp
{
    private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.GainMaxHp));
        yield return AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.LoseMaxHp));
    }

    private static void Prefix(Creature creature, ref decimal amount, out int __state)
    {
        __state = creature.MaxHp;
        if (amount > 0 && !MobEventHealth.HasResolvedMaxHp(creature) && MobRosterRelic.For(creature.Player) != null)
            amount = MobEventHealth.PerMember(amount);
    }

    private static void Postfix(Creature creature, int __state, ref Task __result)
    {
        if (MobRosterRelic.For(creature.Player) is { } crew) __result = Refresh(__result, crew, __state);
    }

    private static async Task Refresh(Task operation, MobRosterRelic crew, int oldMaxHp)
    {
        await operation;
        crew.ApplySharedMaxHpChange(oldMaxHp);
    }
}
