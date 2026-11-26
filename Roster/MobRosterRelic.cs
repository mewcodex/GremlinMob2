using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace GremlinMob2;

// These numeric values are written into GremlinMobLine and index the archived art and localization.
internal enum Fighter { Brute, Bruiser, Guard, Scout, Mage }

public abstract partial class MobRosterRelic : MobRelic
{
    private sealed record Member(Fighter Fighter, int Hp);

    private List<Member>? _line;
    private bool _firstDeathUsed;
    private int _fallenInCombat;

    private Creature Body => Owner.Creature;
    private List<Member> Line => _line ??= Enum.GetValues<Fighter>()
        .Select(fighter => new Member(fighter, Body.MaxHp)).ToList();

    internal Fighter Front => Line[0].Fighter;
    internal Fighter? VoucherMember => Line.FirstOrDefault(member => member.Hp < 0)?.Fighter ??
        Line.Skip(1).FirstOrDefault(member => member.Hp > 0)?.Fighter;
    internal int Living => 1 + Reserves.Length;
    internal Fighter[] Reserves => Line.Skip(1).Where(x => x.Hp > 0).Select(x => x.Fighter).ToArray();
    internal bool HasFallen => Line.Skip(1).Any(x => x.Hp == 0);

    [SavedProperty]
    public bool OverpoweredMode { get; set; }

    // Slot zero uses the native creature's current HP. A reserve at 0 is fallen and -1 is pledged.
    [SavedProperty]
    public string GremlinMobLine
    {
        get => string.Join(',', Line.Select((member, index) => $"{(int)member.Fighter}:{(index == 0 ? Body.CurrentHp : member.Hp)}"));
        set
        {
            AssertMutable();
            if (string.IsNullOrEmpty(value))
            {
                _line = null;
                return;
            }
            _line = value.Split(',').Select(entry =>
            {
                string[] values = entry.Split(':');
                return new Member((Fighter)int.Parse(values[0]), int.Parse(values[1]));
            }).ToList();
        }
    }

    internal static MobRosterRelic? For(Player? player) => player?.Character is GremlinMobCharacter
        ? player.Relics.OfType<MobRosterRelic>().FirstOrDefault() : null;

    internal (Fighter Fighter, int Hp)[] Formation() =>
        HealthByMember()
            .Where((member, index) => index == 0 || member.Item2 > 0).ToArray();

    internal IEnumerable<(Fighter Fighter, int Hp)> HealthByMember() =>
        Line.Select((member, index) => (member.Fighter, index == 0 ? Body.CurrentHp : Math.Min(member.Hp, Body.MaxHp)));

    internal Task RotateRandom(PlayerChoiceContext context) => Reserves is { Length: > 0 } reserves
        ? Rotate(context, Owner.RunState.Rng.Niche.NextItem(reserves)) : Task.CompletedTask;

    internal async Task Rotate(PlayerChoiceContext context, Fighter? next = null, bool fromRelic = false)
    {
        if (Body.HasPower<MobRampageForm>())
        {
            if (!fromRelic && LegacyLocalization.Ui("Swap") is { Length: > 0 } lines)
                TalkCmd.Play(new LocString("powers", "GREMLIN_MOB_NOB_SWAP_" + Godot.GD.RandRange(0, Math.Min(2, lines.Length - 1))),
                    Body, VfxColor.Cyan, VfxDuration.Standard);
            return;
        }
        int index = Line.FindIndex(1, member => member.Hp > 0 && (next == null || member.Fighter == next));
        if (index < 0) return;
        Move(index, false);
        await OnRotation(context);
    }

    private async Task OnRotation(PlayerChoiceContext context)
    {
        int cards = Body.GetPowerAmount<MobSwapDraw>();
        if (cards > 0) await CardPileCmd.Draw(context, cards, Owner);
        int sticks = Owner.PlayerCombatState?.Hand.Cards.Count(card => card is MobSwapStick) ?? 0;
        if (sticks > 0) await PlayerCmd.GainEnergy(sticks, Owner);
        await MobPilferedGoods.AddCard(Owner);
    }

    private void Move(int index, bool fallen)
    {
        Member next = Line[index];
        Member previous = Line[0] with { Hp = fallen ? 0 : Body.CurrentHp };
        Line.RemoveAt(index);
        Line.RemoveAt(0);
        Line.Insert(0, next);
        Line.Add(previous);
        Body.SetCurrentHpInternal(Math.Min(next.Hp, Body.MaxHp));
        MobFormation.Update(Body, this);
        OriginalCardArt.RefreshDance(Owner);
    }

}
