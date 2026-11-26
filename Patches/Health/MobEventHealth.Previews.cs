using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace GremlinMob2;

internal static partial class MobEventHealth
{
    [HarmonyPatch(typeof(EventOption), nameof(EventOption.ThatWillKillPlayerIf))]
    private static class TabletPreview
    {
        private static void Postfix(EventOption __instance)
        {
            if (!__instance.TextKey.StartsWith("TABLET_OF_TRUTH.pages.", StringComparison.Ordinal)
                || !__instance.TextKey.Contains(".DECIPHER", StringComparison.Ordinal)
                || !Events.TryGetValue(__instance, out var model) || model is not TabletOfTruth
                || MobRosterRelic.For(model.Owner) == null || Notes.TryGetValue(__instance.Description, out _)) return;
            decimal original = model.DynamicVars["DecipherMaxHpLoss"].BaseValue;
            int amount = __instance.TextKey.Contains("DECIPHER_4", StringComparison.Ordinal)
                ? (int)original : PerMember(original);
            AccessTools.PropertySetter(typeof(EventOption), nameof(EventOption.WillKillPlayer))
                .Invoke(__instance, [(Func<MegaCrit.Sts2.Core.Entities.Players.Player, bool>)(player => player.Creature.MaxHp <= amount)]);
            Notes.AddOrUpdate(__instance.Description, new(amount, original, true));
        }
    }

    [HarmonyPatch(typeof(EventOption), "AddLocVars")]
    private static class ObserveOption
    {
        private static void Postfix(EventOption __instance, EventModel eventModel)
        {
            Events.AddOrUpdate(__instance, eventModel);
            if (MobRosterRelic.For(eventModel.Owner) != null)
                EventDescriptions.AddOrUpdate(__instance.Description, eventModel);
        }
    }

    [HarmonyPatch(typeof(EventOption), nameof(EventOption.ThatDoesDamage))]
    private static class DamagePreview
    {
        private static void Postfix(EventOption __instance, decimal damage)
        {
            if (!Events.TryGetValue(__instance, out var model) || MobRosterRelic.For(model.Owner) is not { } crew || damage <= 0) return;
            int amount = model is AbyssalBaths ? BathNetCost(model) : PerMember(damage);
            __instance.ThatWillKillPlayerIf(player => player.Creature.CurrentHp <= amount && !crew.HasReserveAfterDamage(amount));
            Notes.AddOrUpdate(__instance.Description, new(amount, damage, false, model is AbyssalBaths));
        }
    }

    [HarmonyPatch(typeof(EventOption), nameof(EventOption.ThatDecreasesMaxHp))]
    private static class MaxHpPreview
    {
        private static void Postfix(EventOption __instance, decimal value)
        {
            if (!Events.TryGetValue(__instance, out var model) || MobRosterRelic.For(model.Owner) is not { } crew || value <= 0) return;
            int amount = PerMember(value);
            __instance.ThatWillKillPlayerIf(player => player.Creature.MaxHp <= amount && !crew.HasReserveAfterDamage(0));
            Notes.AddOrUpdate(__instance.Description, new(amount, value, true));
        }
    }

    [HarmonyPatch(typeof(LocString), nameof(LocString.GetFormattedText), [])]
    private static class ShowNote
    {
        private static void Postfix(LocString __instance, ref string __result)
        {
            if (FixedRelicCosts.TryGetValue(__instance, out var relicCost))
            {
                __result = __result.Replace($"]{relicCost.OriginalAmount}[/", $"]{relicCost.Amount}[/");
                return;
            }
            Notes.TryGetValue(__instance, out var note);
            EventDescriptions.TryGetValue(__instance, out var model);
            if (note == null && model == null) return;
            Dictionary<string, object>? variables = null;
            foreach (var pair in __instance.Variables)
            {
                if (pair.Value is not DynamicVar value) continue;
                int? amount = PreviewAmount(model, note, pair.Key, value);
                if (amount == null || amount == value.BaseValue) continue;
                variables ??= new Dictionary<string, object>(__instance.Variables);
                variables[pair.Key] = WithAmount(value, amount.Value);
            }
            if (variables != null) __result = LocManager.Instance.SmartFormat(__instance, variables);
            if (note is { Maximum: false, Net: false })
                __result += "\n" + LegacyLocalization.EventHp(note.Amount);
        }

        private static int? PreviewAmount(EventModel? model, TextNote? note, string key, DynamicVar value)
        {
            bool maxHp = key.Contains("MaxHp", StringComparison.OrdinalIgnoreCase);
            bool hpLoss = key.Contains("HpLoss", StringComparison.OrdinalIgnoreCase);
            if (note is { Net: false } && value.BaseValue == note.OriginalAmount &&
                (note.Maximum ? maxHp || model is DrowningBeacon && hpLoss
                    : hpLoss || key.Contains("Damage", StringComparison.OrdinalIgnoreCase)))
                return note.Amount;
            if (model is AncientEventModel) return null;
            return value is MaxHpVar || key is "DrinkMaxHpGain" or "SmashHPGain"
                || value is HealVar && model is not (RoundTeaParty or UnrestSite or SpiralingWhirlpool or DenseVegetation)
                || value is DamageVar && model is AbyssalBaths
                    ? PerMember(value.BaseValue) : null;
        }
    }

    private static DynamicVar WithAmount(DynamicVar value, int amount)
    {
        var copy = value.Clone();
        copy.BaseValue = amount;
        return copy;
    }

    [HarmonyPatch]
    private static class RelicPreview
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertyGetter(typeof(RelicModel), nameof(RelicModel.DynamicDescription));
            yield return AccessTools.PropertyGetter(typeof(RelicModel), nameof(RelicModel.DynamicEventDescription));
        }

        private static void Postfix(RelicModel __instance, LocString __result)
        {
            if (!__instance.IsMutable || __instance is Wuppy || MobRosterRelic.For(__instance.Owner) == null) return;
            foreach (var key in __result.Variables.Keys.ToArray())
            {
                if (__result.Variables[key] is not DynamicVar value) continue;
                if (value is MaxHpVar ||
                    __instance is SereTalon or FragrantMushroom && value is HpLossVar ||
                    __instance is PrecariousShears && value is DamageVar)
                    __result.AddObj(key, WithAmount(value, PerMember(value.BaseValue)));
                if (__instance is SereTalon && value is HpLossVar)
                    FixedRelicCosts.AddOrUpdate(__result, new(PerMember(value.BaseValue), value.BaseValue, true));
            }
        }
    }
}
