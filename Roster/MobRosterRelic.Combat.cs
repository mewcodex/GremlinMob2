using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

public abstract partial class MobRosterRelic
{
    private sealed record PlayInfo(Fighter? Fighter, HashSet<Creature> Targets, int SpentCharge);

    private ConditionalWeakTable<CardPlay, PlayInfo> _plays = new();

    private Stack<CardPlay> _activePlays = new();

    public override Task BeforeCombatStart()
    {
        _plays = new();
        _activePlays = new();
        _firstDeathUsed = false;
        _fallenInCombat = 0;
        MobFormation.Update(Body, this);
        return Task.CompletedTask;
    }

    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player == Owner && Owner.PlayerCombatState?.TurnNumber == 1 && this is MobRotationBell && !IsMelted
            ? count + 1 : count;

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext context, Player player)
    {
        if (player != Owner || player.Character is not GremlinMobCharacter) return;
        await MobFighterPassive.Ensure(context, Body);
        if (player.PlayerCombatState?.TurnNumber != 1 || this is not MobRotationBell || IsMelted) return;
        Flash();
        await PlayerCmd.GainEnergy(1, Owner);
        await Rotate(context, fromRelic: true);
    }

    internal async Task SpendCharge(PlayerChoiceContext context, int amount)
    {
        if (Body.GetPowerAmount<MobChargePower>() <= amount)
        {
            await ClearCharge();
            return;
        }
        await Charge(context, -amount);
        await MobSpellHat.RemoveDebuff(Owner);
    }

    internal async Task ClearCharge()
    {
        await PowerCmd.Remove<MobChargePower>(Body);
        await MobSpellHat.RemoveDebuff(Owner);
    }

    internal Task Charge(PlayerChoiceContext context, int amount, CardModel? source = null) =>
        PowerCmd.Apply<MobChargePower>(context, Body, amount, Body, source);

    internal async Task TriggerBonus(PlayerChoiceContext context, Fighter fighter, CardPlay play)
    {
        switch (fighter)
        {
            case Fighter.Brute:
                await PowerCmd.Apply<MobMadStrength>(context, Body, 2, Body, play.Card);
                break;
            case Fighter.Bruiser:
                await PowerCmd.Apply<WeakPower>(context, Body.CombatState?.HittableEnemies, 1, Body, play.Card);
                break;
            case Fighter.Guard:
                await CreatureCmd.GainBlock(Body, 2, ValueProp.Unpowered, null);
                break;
            case Fighter.Scout:
                if (Owner.RunState.Rng.CombatTargets.NextItem(Body.CombatState?.HittableEnemies ?? []) is { } enemy)
                    await CreatureCmd.Damage(context, enemy, 2 + MobScattershotRelic.Bonus(Owner), ValueProp.Unpowered, Body, null, null);
                break;
            case Fighter.Mage:
                await Charge(context, 1, play.Card);
                break;
        }
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker == Body) MobFormation.React(Body, attack: true);
        return Task.CompletedTask;
    }

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? source, CardPlay? play) =>
        target == Body && source is MobPaperWard ? Body.GetPowerAmount<MobWardPolishPower>() : 0;

    public override Task BeforeCardPlayed(CardPlay play)
    {
        if (play.Player != Owner) return Task.CompletedTask;

        // A play may rotate the leader or gain charge. Its attack and after-play hooks use the opening state.
        int charge = Body.GetPowerAmount<MobChargePower>();
        bool charged = play.Card.Type == CardType.Attack && play.Card is not MobDoubleBlast && charge >= 3;
        _plays.Add(play, new(Body.HasPower<MobRampageForm>() ? null : Front, [], charged ? charge : 0));
        _activePlays.Push(play);
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? source, CardPlay? play)
    {
        if (dealer != Body || !props.HasFlag(ValueProp.Move) || source?.Type != CardType.Attack) return 0;
        bool chargeReady = source is not MobDoubleBlast && play != null && _plays.TryGetValue(play, out var info)
            ? info.SpentCharge > 0
            : Body.GetPowerAmount<MobChargePower>() >= 3;
        decimal bonus = chargeReady ? MobChargePower.Bonus(Body) : 0;
        if (source is MegaCrit.Sts2.Core.Models.Cards.Shiv)
            bonus += Body.GetPowerAmount<MobWardPolishPower>();
        if (!Body.HasPower<MobRampageForm>() && Front == Fighter.Scout && (play?.Resources.EnergyValue ?? source.EnergyCost.GetAmountToSpend()) == 0) bonus += 2;
        return bonus;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? source)
    {
        _activePlays.TryPeek(out CardPlay? play);
        if (dealer != Body || play == null || source != play.Card || source.Type != CardType.Attack
            || !props.HasFlag(ValueProp.Move) || !target.IsAlive || !_plays.TryGetValue(play, out var info)
            || info.Fighter != Fighter.Bruiser || !info.Targets.Add(target)) return;
        await PowerCmd.Apply<WeakPower>(context, target, 1, Body, source);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? source)
    {
        if (target != Body || !Body.IsAlive) return;
        if (_fallenInCombat > 0)
            await AfterFallen(context);
        else
        {
            if (result.UnblockedDamage > 0) MobFormation.React(Body, attack: false);
            if (!Body.HasPower<MobRampageForm>() && Front == Fighter.Brute && dealer != null && dealer.Side != Body.Side && props.HasFlag(ValueProp.Move))
                await PowerCmd.Apply<MobMadStrength>(context, Body, 2, Body, null);
        }
    }

    internal async Task AfterFallen(PlayerChoiceContext context)
    {
        if (_fallenInCombat <= 0) return;
        _fallenInCombat--;
        if (Owner.Relics.OfType<MobReserveBomb>().Any() && Body.CombatState is { } combat)
            await CreatureCmd.Damage(context, combat.HittableEnemies, 30, ValueProp.Unpowered, Body);
        await OnRotation(context);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (!_plays.TryGetValue(play, out var info)) return;
        if (_activePlays.TryPeek(out var active) && active == play) _activePlays.Pop();
        if (!Body.IsAlive) return;
        if (info.SpentCharge > 0) await SpendCharge(context, info.SpentCharge);
        if ((play.Card.Type == CardType.Skill && info.Fighter is Fighter.Guard or Fighter.Mage)
            || (play.Card.Type == CardType.Attack && info.Fighter == Fighter.Scout))
            await TriggerBonus(context, info.Fighter!.Value, play);
    }
}
