using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

internal static partial class MobEventHealth
{
    private sealed record TextNote(int Amount, decimal OriginalAmount, bool Maximum, bool Net = false);
    private static readonly ConditionalWeakTable<EventOption, EventModel> Events = new();
    private static readonly ConditionalWeakTable<LocString, EventModel> EventDescriptions = new();
    private static readonly ConditionalWeakTable<LocString, TextNote> Notes = new();
    private static readonly ConditionalWeakTable<LocString, TextNote> FixedRelicCosts = new();
    // Max-HP commands can invoke Heal or Damage internally. Their arguments are already per member.
    private static readonly AsyncLocal<Creature?> SkipEventScalingFor = new();
    private static readonly AsyncLocal<Creature?> ResolvedMaxHp = new();
    internal static bool HasResolvedMaxHp(Creature creature) => creature == ResolvedMaxHp.Value;

    internal static int PerMember(decimal amount) => (int)Math.Max(0, Math.Ceiling(amount / 5));
}
