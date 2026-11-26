using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

public sealed class MobPainSuppression : MobPower
{
    public override PowerType Type => PowerType.Debuff;
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? source, CardPlay? play) =>
        dealer == Owner && props.HasFlag(ValueProp.Move) ? .2m : 1m;
    public override Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants) =>
        participants.Contains(Owner) ? PowerCmd.Decrement(this) : Task.CompletedTask;
}

public sealed class MobBlockPunishment : MobPower
{
    public override PowerType Type => PowerType.Debuff;
    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? source) =>
        creature == Applier && amount > 0 && Owner.IsAlive
            ? CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, Amount, ValueProp.Unblockable | ValueProp.Unpowered, creature, null, null)
            : Task.CompletedTask;
    public override Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants) =>
        Applier != null && participants.Contains(Applier) ? PowerCmd.Remove(this) : Task.CompletedTask;
}

public sealed class MobWeakPunishment : MobPower
{
    public override PowerType Type => PowerType.Debuff;
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? card) =>
        power is WeakPower && power.Owner == Owner && amount > 0 && Owner.IsAlive
            ? CreatureCmd.Damage(context, Owner, Amount, ValueProp.Unblockable | ValueProp.Unpowered, Applier, null, null)
            : Task.CompletedTask;
}

public sealed class MobBlockAsDamage : MobPower
{
    public override Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants) =>
        participants.Contains(Owner) ? PowerCmd.Remove(this) : Task.CompletedTask;
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock), [typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardPlay), typeof(bool)])]
internal static class RedirectGainedBlock
{
    private static bool Prefix(Creature creature, decimal amount, ValueProp props, CardPlay? cardPlay, ref Task<decimal> __result)
    {
        if (creature.GetPower<MobBlockAsDamage>() == null || creature.CombatState == null) return true;
        __result = Redirect(creature, amount, props, cardPlay);
        return false;
    }

    private static async Task<decimal> Redirect(Creature creature, decimal amount, ValueProp props, CardPlay? play)
    {
        if (CombatManager.Instance.IsOverOrEnding || creature.IsDead) return 0;
        var combat = creature.CombatState!;
        await Hook.BeforeBlockGained(combat, creature, amount, props, play?.Card);
        decimal block = Math.Max(0, Hook.ModifyBlock(combat, creature, amount, props, play?.Card, play, out var modifiers));
        await Hook.AfterModifyingBlockAmount(combat, block, play?.Card, play, modifiers);
        if (block > 0)
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), combat.HittableEnemies, block, ValueProp.Unpowered, creature);
        return 0;
    }
}

public sealed class MobHecklingCover : MobPower
{
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? card) =>
        applier == Owner && power.Owner.Side != Owner.Side && power.GetTypeForAmount(amount) == PowerType.Debuff
            && amount != 0 && power is not MobTemporaryGuard
            ? PowerCmd.Apply<MobTemporaryGuard>(context, Owner, Amount, Owner, null)
            : Task.CompletedTask;
}

public sealed class MobAttackCounterArmor : MobPower
{
    private int _attacks;
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Player.Creature != Owner || play.Card.Type != CardType.Attack || ++_attacks < 7) return;
        _attacks = 0;
        await PowerCmd.Apply<ArtifactPower>(context, Owner, Amount, Owner, null);
    }
}

public sealed class MobWeakTargetBonus : MobPower
{
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? source, CardPlay? play) =>
        dealer == Owner && props.HasFlag(ValueProp.Move) && target?.GetPowerAmount<WeakPower>() > 0 ? Amount : 0;
}

public sealed class MobHoldStrength : MobPower
{
    public override bool TryModifyPowerAmountReceived(PowerModel power, Creature target, decimal amount, Creature? applier, out decimal modified)
    {
        bool prevent = power is StrengthPower && target == Owner && amount < 0;
        modified = prevent ? 0 : amount;
        return prevent;
    }
}

public sealed class MobEvadeAndRotate : MobPower
{
    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? source)
    {
        if (target != Owner || amount < 1 || Amount <= 0) return amount;
        TemporaryHpDamage.Record(this, 1);
        return 0;
    }

    internal bool Pending { get; set; }
    public override async Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? source)
    {
        if (target != Owner || !Pending) return;
        Pending = false;
        if (Amount == 0) await PowerCmd.Remove(this);
        if (MobRosterRelic.For(Owner.Player) is { } crew)
            await crew.RotateRandom(context);
    }
}

public sealed class MobRampageForm : MobPower
{
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (MobRosterRelic.For(Owner.Player) is not { } crew) return Task.CompletedTask;
        MobFormation.Update(Owner, crew);
        OriginalCardArt.RefreshDance(Owner.Player!);
        return PowerCmd.Remove<MobFighterPassive>(Owner);
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        if (MobRosterRelic.For(oldOwner.Player) is not { } crew) return Task.CompletedTask;
        MobFormation.Update(oldOwner, crew);
        OriginalCardArt.RefreshDance(oldOwner.Player!);
        return MobFighterPassive.Ensure(new ThrowingPlayerChoiceContext(), oldOwner);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner || Owner.CombatState is not { } combat) return;
        await CardPileCmd.AddGeneratedCardsToCombat([
            combat.CreateCard<MobRampageRoar>(player), combat.CreateCard<MobRampageBash>(player), combat.CreateCard<MobRampageRush>(player)
        ], PileType.Hand, player);
    }

    public override Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? source) =>
        target == Owner && target.GetPowerAmount<MobTemporaryGuard>() == 0 ? PowerCmd.Remove(this) : Task.CompletedTask;
}
