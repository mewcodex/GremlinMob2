using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

public abstract class MobPower : PowerModel
{
    internal static Type[] Types { get; } = typeof(MobPower).Assembly.GetTypes()
        .Where(type => type.IsSubclassOf(typeof(MobPower)) && !type.IsAbstract).ToArray();
    internal static IEnumerable<MobPower> All => Types.Select(type => ModelDb.GetById<MobPower>(ModelDb.GetId(type)));
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

public sealed class MobFighterPassive : MobPower
{
    private Fighter Fighter => IsMutable ? MobRosterRelic.For(Owner?.Player)?.Front ?? default : default;
    public override LocString Title => new("powers", LegacyLocalization.FighterTitle(Fighter));
    public override LocString Description => new("powers", LegacyLocalization.FighterDescription(Fighter));
    public override PowerStackType StackType => PowerStackType.Single;

    internal static async Task Ensure(PlayerChoiceContext context, Creature body)
    {
        if (body.IsAlive && body.CombatState?.IsLiveCombat() == true &&
            !body.HasPower<MobRampageForm>() && !body.HasPower<MobFighterPassive>())
            await PowerCmd.Apply<MobFighterPassive>(context, body, 1, body, null);
    }
}

public sealed class MobChargePower : MobPower
{
    public override LocString Description
    {
        get
        {
            if (IsCanonical) return base.Description;
            var key = Id.Entry + (Amount >= 3 ? ".ready" : ".charging");
            var text = LocString.GetIfExists("powers", key) ?? base.Description;
            text.Add("ChargeRemaining", Math.Max(0, 3 - Amount));
            return text;
        }
    }

    internal static int Bonus(Creature creature) =>
        7 + creature.GetPowerAmount<MobEncoreDamagePower>() +
        (creature.Player?.Relics.OfType<MobSpellStaff>().Any(relic => !relic.IsMelted) == true ? 7 : 0);

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power.Owner != Owner || (power != this && power is not MobEncoreDamagePower)) return;
        if (Amount < 3)
        {
            await PowerCmd.Remove<MobBangPower>(Owner);
            return;
        }
        int bonus = Bonus(Owner);
        if (Owner.GetPower<MobBangPower>() is { } ready)
            ready.SetAmount(bonus);
        else
            await PowerCmd.Apply<MobBangPower>(context, Owner, bonus, Owner, null, silent: true);
    }

    public override Task AfterRemoved(Creature oldOwner) => PowerCmd.Remove<MobBangPower>(oldOwner);
}

public sealed class MobBangPower : MobPower;

public sealed class MobTemporaryGuard : MobPower
{

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || amount < 1) return amount;
        if (Amount > 0)
        {
            int spent = Math.Min((int)amount, Amount);
            TemporaryHpDamage.Record(this, spent);
            amount = Owner.HasPower<MobRampageForm>() ? 0 : amount - spent;
        }

        // Scatter reacts to remaining real HP loss, regardless of the powers' application order.
        return Owner.GetPower<MobEvadeAndRotate>()?.PreventHpLoss(amount) ?? amount;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? source) =>
        target == Owner && Amount == 0 ? PowerCmd.Remove(this) : Task.CompletedTask;

    public override Task AfterRemoved(Creature oldOwner) =>
        oldOwner.HasPower<MobTemporaryGuard>() ? Task.CompletedTask : PowerCmd.Remove<MobRampageForm>(oldOwner);
}

public sealed class MobRotationRhythm : MobPower
{
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature != Owner) return;
        await PlayerCmd.GainEnergy(Amount, player);
        if (MobRosterRelic.For(player) is { } crew) await crew.Rotate(context);
    }
}

public sealed class MobSwapDraw : MobPower
{
}

public sealed class MobMadStrength : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<MobDance>();
}

public sealed class MobWeakenedStrength : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<MobDance>();
    protected override bool IsPositive => false;
}

public sealed class MobShadeKnivesPower : MobPower
{
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Player.Creature != Owner || play.Card.Type != CardType.Attack || play.Card.Keywords.Contains(CardKeyword.Exhaust)) return;
        for (int i = 0; i < Amount; i++) await Shiv.CreateInHand(play.Player, Owner.CombatState!);
    }
}

public sealed class MobWardMakerPower : MobPower
{
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature != Owner) return;
        await CardPileCmd.Draw(context, Amount, player);
        var cards = Enumerable.Range(0, Amount).Select(_ => combatState.CreateCard<MobPaperWard>(player)).ToArray();
        foreach (var card in cards) CardCmd.Upgrade(card, CardPreviewStyle.None);
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Discard, player);
    }
}

public sealed class MobWizardTrainingPower : MobPower
{
    public override Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState) =>
        player.Creature == Owner ? PowerCmd.Apply<MobChargePower>(context, Owner, Amount, Owner, null) : Task.CompletedTask;
}

public sealed class MobEncoreDamagePower : MobPower
{
}

public sealed class MobWardPolishPower : MobPower
{
}

public sealed class MobMirrorBlockPower : MobPower
{
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play) =>
        play.Player.Creature == Owner && play.Card is not MobMistBarrier
            ? CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null) : Task.CompletedTask;

    public override Task AfterSideTurnEnd(PlayerChoiceContext context, MegaCrit.Sts2.Core.Combat.CombatSide side, IEnumerable<Creature> participants) =>
        participants.Contains(Owner) ? PowerCmd.Remove(this) : Task.CompletedTask;
}

public sealed class MobWhizReturnPower : MobPower
{
    private int _upgraded;
    internal void AddUpgrade() => _upgraded++;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner || Owner.CombatState is not { } combat) return;
        var cards = Enumerable.Range(0, Amount).Select(_ => combat.CreateCard<MobBurst>(player)).ToArray();
        foreach (var card in cards.Take(_upgraded)) CardCmd.Upgrade(card, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
        await PowerCmd.Remove(this);
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, player);
    }
}

public sealed class MobTemporaryThorns : TemporaryThornsPower
{
    public override AbstractModel OriginModel => ModelDb.Card<MobBruteEntrance>();
}
