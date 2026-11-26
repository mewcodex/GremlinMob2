using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Rooms;

namespace GremlinMob2;

public abstract partial class MobRosterRelic
{
    public override Task AfterCombatVictory(CombatRoom room)
    {
        if (!OverpoweredMode || !HasFallen) return Task.CompletedTask;

        for (int i = 1; i < Line.Count; i++)
            if (Line[i].Hp == 0)
                Line[i] = Line[i] with { Hp = 1 };
        MobFormation.Update(Body, this);
        return Task.CompletedTask;
    }

    public override Task AfterActEntered()
    {
        if (_line == null)
        {
            _line = Enum.GetValues<Fighter>().OrderBy(_ => Owner.RunState.Rng.Niche.NextInt())
                .Select(fighter => new Member(fighter, Body.MaxHp)).ToList();
        }
        else if (Owner.RunState.CurrentActIndex > 0)
        {
            bool reduced = Owner.RunState.AscensionLevel >= 5;
            decimal fraction = reduced ? .75m : 1m;
            for (int i = 0; i < (reduced ? 2 : 4) && HasFallen; i++)
                ReviveRandom(Body.MaxHp / 2);
            for (int i = 1; i < Line.Count; i++)
            {
                if (Line[i].Hp <= 0) continue;
                int hp = Math.Min(Body.MaxHp, Line[i].Hp);
                int healed = (int)((Body.MaxHp - hp) * fraction + .5m);
                Line[i] = Line[i] with { Hp = hp + healed };
            }
        }
        return Task.CompletedTask;
    }

    public override Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (player != Owner) return Task.CompletedTask;
        int healing = (int)HealRestSiteOption.GetHealAmount(player);
        for (int i = 1; i < Line.Count; i++)
            if (Line[i].Hp > 0)
                Line[i] = Line[i] with { Hp = Math.Min(Body.MaxHp, Line[i].Hp + healing) };
        ReviveRandom(Body.MaxHp / 2);
        return Task.CompletedTask;
    }

    internal bool HasReserveAfterDamage(int damage) => Line.Skip(1).Any(fighter => fighter.Hp > damage);

    internal void DamageReserves(int damage)
    {
        for (int i = 1; i < Line.Count; i++)
            if (Line[i].Hp > 0) Line[i] = Line[i] with { Hp = Math.Max(0, Line[i].Hp - damage) };
    }

    internal void HealReserves(int amount)
    {
        for (int i = 1; i < Line.Count; i++)
            if (Line[i].Hp > 0) Line[i] = Line[i] with { Hp = Math.Min(Body.MaxHp, Line[i].Hp + amount) };
    }

    internal void ApplySharedMaxHpChange(int oldMaxHp)
    {
        int gain = Math.Max(0, Body.MaxHp - oldMaxHp);
        for (int i = 1; i < Line.Count; i++)
            if (Line[i].Hp > 0) Line[i] = Line[i] with { Hp = Math.Min(Body.MaxHp, Line[i].Hp + gain) };
        MobFormation.Update(Body, this);
    }

    public override IReadOnlyList<LocString> ModifyExtraRestSiteHealText(Player player, IReadOnlyList<LocString> currentExtraText) =>
        player == Owner ? [.. currentExtraText, new LocString("rest_site_ui", "GREMLIN_MOB_HEAL")] : currentExtraText;

    internal bool CanInterceptDeath => (Body.CombatState?.IsLiveCombat() == true && !_firstDeathUsed && Owner.Relics.OfType<MobReserveTalisman>().Any()) || Reserves.Length > 0;

    internal bool PreventDeath()
    {
        bool combat = Body.CombatState?.IsLiveCombat() == true;
        if (combat && !_firstDeathUsed && Owner.Relics.OfType<MobReserveTalisman>().FirstOrDefault() is { } talisman)
        {
            _firstDeathUsed = true;
            Body.SetCurrentHpInternal(Math.Max(1, Body.MaxHp * 3 / 4));
            talisman.Flash();
            MobFormation.Update(Body, this);
            return true;
        }
        int index = !combat && Owner.RunState.CurrentRoom is EventRoom
            ? Line.FindLastIndex(member => member.Hp > 0)
            : Line.FindIndex(1, member => member.Hp > 0);
        if (index < 1) return false;
        Move(index, true);
        if (combat) _fallenInCombat++;
        return true;
    }

    internal void SacrificeRear()
    {
        int index = Line.FindIndex(1, member => member.Hp > 0);
        if (index > 0)
        {
            Line[index] = Line[index] with { Hp = -1 };
            MobFormation.Update(Body, this);
        }
    }

    internal bool ReviveRandom(int hp)
    {
        int[] fallen = Enumerable.Range(1, Line.Count - 1).Where(i => Line[i].Hp == 0).ToArray();
        if (fallen.Length == 0) return false;
        int index = Owner.RunState.Rng.Niche.NextItem(fallen);
        var revived = Line[index] with { Hp = Math.Clamp(hp, 1, Body.MaxHp) };
        if (Body.CombatState?.IsLiveCombat() == true)
        {
            Line.RemoveAt(index);
            Line.Add(revived);
        }
        else
        {
            Line[index] = Line[fallen[0]];
            Line[fallen[0]] = revived;
        }
        MobFormation.Update(Body, this);
        return true;
    }

    internal async Task<bool> RestoreFromGravestone(Fighter fighter)
    {
        int index = Line.FindIndex(member => member.Fighter == fighter && member.Hp >= 0);
        if (index < 0) return false;
        if (index == 0)
        {
            int missing = Body.MaxHp - Body.CurrentHp;
            if (missing > 0) await CreatureCmd.Heal(Body, missing);
        }
        else
        {
            Line[index] = Line[index] with { Hp = Body.MaxHp };
            MobFormation.Update(Body, this);
        }
        return true;
    }
}
