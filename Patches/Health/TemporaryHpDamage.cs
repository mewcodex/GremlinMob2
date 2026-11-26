using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace GremlinMob2;

internal static class TemporaryHpDamage
{
    private sealed record Absorption(MobPower Guard, int Amount);
    private static readonly ConditionalWeakTable<DamageResult, List<Absorption>> Absorptions = new();
    [ThreadStatic] private static List<Absorption>? _calculation;

    internal static void Record(MobPower power, int amount) => _calculation?.Add(new(power, amount));

    [HarmonyPatch(typeof(CreatureCmd), "GetDamageResults")]
    private static class Collect
    {
        private static void Prefix(out List<Absorption>? __state)
        {
            __state = _calculation;
            _calculation = [];
        }

        private static void Postfix(DamageResults __result)
        {
            if (_calculation == null) return;
            foreach (DamageResult result in __result.AllResults())
            {
                var hits = _calculation.Where(hit => hit.Guard.Owner == result.Receiver).ToList();
                if (hits.Count > 0) Absorptions.Add(result, hits);
            }
        }

        private static void Finalizer(List<Absorption>? __state) => _calculation = __state;
    }

    [HarmonyPatch(typeof(CreatureCmd), "ApplyDamageResults")]
    private static class Commit
    {
        private static void Prefix(DamageResults results)
        {
            foreach (DamageResult result in results.AllResults())
            {
                if (!Absorptions.TryGetValue(result, out var hits)) continue;
                Absorptions.Remove(result);
                foreach (var hit in hits)
                {
                    hit.Guard.SetAmount(Math.Max(0, hit.Guard.Amount - hit.Amount));
                    if (hit.Guard is MobEvadeAndRotate scatter) scatter.Pending = true;
                }
            }
        }
    }
}
