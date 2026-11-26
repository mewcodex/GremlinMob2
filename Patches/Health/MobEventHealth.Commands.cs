using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

internal static partial class MobEventHealth
{
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
        [typeof(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext), typeof(IEnumerable<DamageTargetData>),
         typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(MegaCrit.Sts2.Core.Entities.Cards.CardPlay),
         typeof(ModifyDamageHookType)])]
    private static class EventDamage
    {
        private static void Prefix(ref IEnumerable<DamageTargetData>? targetData, ref decimal amount, Creature? dealer, CardModel? cardSource)
        {
            if (dealer != null || cardSource != null || amount <= 0 || targetData == null) return;
            var targets = targetData as IReadOnlyList<DamageTargetData> ?? targetData.ToArray();
            targetData = targets;
            if (targets.Count != 1) return;
            var target = targets[0].target;
            if (target == SkipEventScalingFor.Value || target.IsDead || target.CombatState?.IsLiveCombat() == true
                || target.Player?.RunState.CurrentRoom is not EventRoom || MobRosterRelic.For(target.Player) is not { } crew) return;
            int individual = PerMember(amount);
            crew.DamageReserves(individual);
            amount = individual;
        }
    }

    [HarmonyPatch]
    private static class ProtectMaxHpLoss
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.LoseMaxHp));
            yield return AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.GainMaxHp));
        }

        private static void Prefix(Creature creature, out Creature? __state)
        {
            __state = SkipEventScalingFor.Value;
            if (MobRosterRelic.For(creature.Player) != null) SkipEventScalingFor.Value = creature;
        }

        private static void Finalizer(Creature? __state) => SkipEventScalingFor.Value = __state;
    }

    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Entities.RestSite.HealRestSiteOption), "ExecuteRestSiteHeal")]
    private static class ProtectMimickedRest
    {
        private static void Prefix(MegaCrit.Sts2.Core.Entities.Players.Player player, out Creature? __state)
        {
            __state = SkipEventScalingFor.Value;
            if (MobRosterRelic.For(player) != null) SkipEventScalingFor.Value = player.Creature;
        }

        private static void Finalizer(Creature? __state) => SkipEventScalingFor.Value = __state;
    }

    [HarmonyPatch(typeof(LeesWaffle), nameof(LeesWaffle.AfterObtained))]
    private static class Waffle
    {
        private static void Prefix(RelicModel __instance, out Creature? __state)
        {
            __state = SkipEventScalingFor.Value;
            if (MobRosterRelic.For(__instance.Owner) != null) SkipEventScalingFor.Value = __instance.Owner.Creature;
        }

        private static void Postfix(RelicModel __instance, ref Task __result)
        {
            if (MobRosterRelic.For(__instance.Owner) is { } crew)
                __result = HealAfterPickup(__result, crew, __instance.Owner.Creature);
        }

        private static void Finalizer(Creature? __state) => SkipEventScalingFor.Value = __state;

        private static async Task HealAfterPickup(Task original, MobRosterRelic crew, Creature body)
        {
            await original;
            crew.HealReserves(body.MaxHp);
        }
    }

    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
    private static class HealEventReserves
    {
        private static void Prefix(Creature creature, ref decimal amount)
        {
            if (creature == SkipEventScalingFor.Value || creature.CombatState?.IsLiveCombat() == true || creature.Player?.RunState.CurrentRoom is not EventRoom room || room.CanonicalEvent is AncientEventModel
                || MobRosterRelic.For(creature.Player) is not { } crew) return;
            if (room.CanonicalEvent is MegaCrit.Sts2.Core.Models.Events.RoundTeaParty or MegaCrit.Sts2.Core.Models.Events.UnrestSite)
            {
                crew.HealReserves(creature.MaxHp);
                return;
            }
            if (amount <= 0) return;
            if (room.CanonicalEvent is MegaCrit.Sts2.Core.Models.Events.SpiralingWhirlpool)
            {
                crew.HealReserves((int)amount);
                return;
            }
            int individual = PerMember(amount);
            crew.HealReserves(individual);
            amount = individual;
        }
    }
}
