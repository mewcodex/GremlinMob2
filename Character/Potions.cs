using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

public sealed class MobPotions : PotionPoolModel
{
    public override string EnergyColorName => ModelDb.CardPool<MobCards>().EnergyColorName;
    internal static MobPotion[] Potions => [ModelDb.Potion<MobRandomHandoffPotion>(), ModelDb.Potion<MobCrewTonic>(),
        ModelDb.Potion<MobReturningSpirit>()];
    protected override IEnumerable<PotionModel> GenerateAllPotions() => Potions;
}

public abstract class MobPotion : PotionModel
{
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
}

public sealed class MobRandomHandoffPotion : MobPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    protected override Task OnUse(PlayerChoiceContext context, Creature? target)
    {
        return MobRosterRelic.For(Owner)?.RotateRandom(context) ?? Task.CompletedTask;
    }
}

public sealed class MobCrewTonic : MobPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    protected override async Task OnUse(PlayerChoiceContext context, Creature? target)
    {
        var body = Owner.Creature;
        if (body.CombatState is not { } combat) return;
        await PowerCmd.Apply<WeakPower>(context, combat.HittableEnemies, 2, body, null);
        await PowerCmd.Apply<MobMadStrength>(context, body, 4, body, null);
        for (int i = 0; i < 2; i++)
            await CreatureCmd.GainBlock(body, 2, ValueProp.Unpowered, null);
        for (int i = 0; i < 2; i++)
            if (Owner.RunState.Rng.CombatTargets.NextItem(combat.HittableEnemies) is { } enemy)
                await CreatureCmd.Damage(context, enemy, 2, ValueProp.Unpowered, body, null, null);
        await PowerCmd.Apply<MobChargePower>(context, body, 2, body, null);
    }
}

public sealed class MobReturningSpirit : MobPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override bool CanBeGeneratedInCombat => false;
    public override bool PassesCustomUsabilityCheck => MobRosterRelic.For(Owner)?.HasFallen == true;
    protected override Task OnUse(PlayerChoiceContext context, Creature? target)
    {
        MobRosterRelic.For(Owner)?.ReviveRandom(Owner.Creature.MaxHp / 2);
        return Task.CompletedTask;
    }
}
