using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

internal static partial class MobEventHealth
{
    [HarmonyPatch(typeof(EndlessConveyor), "RollDish")]
    private static class ConveyorHealing
    {
        // Replace the HP read in the dish eligibility check and keep the original weighted roll.
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var currentHp = AccessTools.PropertyGetter(typeof(Creature), nameof(Creature.CurrentHp));
            var checkSquad = AccessTools.Method(typeof(ConveyorHealing), nameof(HealthForDishSelection));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(currentHp))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = checkSquad;
                }
                yield return instruction;
            }
        }

        private static int HealthForDishSelection(Creature creature) =>
            MobRosterRelic.For(creature.Player)?.HealthByMember().Any(member => member.Hp > 0 && member.Hp < creature.MaxHp) == true
                ? Math.Min(creature.CurrentHp, creature.MaxHp - 1)
                : creature.CurrentHp;
    }

    [HarmonyPatch(typeof(TabletOfTruth), "LoseMaxHpAndUpgrade")]
    private static class TabletCost
    {
        private static readonly System.Reflection.FieldInfo Count = AccessTools.Field(typeof(TabletOfTruth), "_decipherCount");

        private static void Prefix(TabletOfTruth __instance, ref decimal hp, out Creature? __state)
        {
            __state = ResolvedMaxHp.Value;
            if (MobRosterRelic.For(__instance.Owner) == null) return;
            ResolvedMaxHp.Value = __instance.Owner!.Creature;
            if ((int)Count.GetValue(__instance)! < 4) hp = PerMember(hp);
        }

        private static void Finalizer(Creature? __state) => ResolvedMaxHp.Value = __state;
    }

    private static int BathNetCost(EventModel model) =>
        Math.Max(0, PerMember(model.DynamicVars.Damage.BaseValue) - PerMember(model.DynamicVars.MaxHp.BaseValue));

    [HarmonyPatch(typeof(AbyssalBaths), "WillKillPlayer")]
    private static class BathDeathWarning
    {
        private static void Postfix(AbyssalBaths __instance, ref bool __result)
        {
            if (MobRosterRelic.For(__instance.Owner) is not { } crew) return;
            int amount = BathNetCost(__instance);
            __result = __instance.Owner!.Creature.CurrentHp <= amount && !crew.HasReserveAfterDamage(amount);
        }
    }

    [HarmonyPatch(typeof(DenseVegetation), nameof(DenseVegetation.IsAllowed))]
    private static class DenseVegetationEligibility
    {
        private static void Postfix(DenseVegetation __instance, MegaCrit.Sts2.Core.Runs.IRunState runState, ref bool __result)
        {
            if (__result || runState.Players.Count == 1 || !runState.Players.Any(player => player.Character is GremlinMobCharacter)) return;
            decimal cost = __instance.DynamicVars.HpLoss.BaseValue;
            int each = PerMember(cost);
            __result = runState.Players.All(player =>
                MobRosterRelic.For(player) is { } roster
                    ? player.Creature.CurrentHp > each || roster.HasReserveAfterDamage(each)
                    : player.Creature.CurrentHp > cost);
        }
    }
}
