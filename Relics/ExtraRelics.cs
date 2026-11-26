using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

public sealed class MobSteadyMuscle : MobRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override string IconBaseName => "vajra";
    public override Task BeforeCombatStart() =>
        PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null);
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource) =>
        power is StrengthPower && power.Owner == Owner.Creature && amount > 0
            ? PowerCmd.Apply<MobTemporaryGuard>(context, power.Owner, amount, power.Owner, null) : Task.CompletedTask;
}

public sealed class MobScattershotRelic : MobRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override string IconBaseName => "lantern";
    internal static int Bonus(Player player) => player.Relics.OfType<MobScattershotRelic>().Any() ? 3 : 0;
}

public sealed class MobSpareGrave : MobRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override string IconBaseName => "anchor";
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner || MobRosterRelic.For(player) == null) return false;
        options.Add(new FreeRevivalOption(player));
        return true;
    }
}

internal sealed class FreeRevivalOption(Player player) : RestSiteOption(player)
{
    private string Image => OriginalAssets.Picture(IsEnabled ? "ui/campfire/resurrect" : "ui/campfire/resurrect_unusable")!;
    public override string OptionId => "MOB_REVIVE";
    public override IEnumerable<string> AssetPaths => [Image];
    public override bool IsEnabled => MobRosterRelic.For(Owner) is { } crew && (crew.OverpoweredMode || crew.HasFallen);
    public override LocString Description => MobRosterRelic.For(Owner)?.OverpoweredMode == true ? Title : base.Description;

    public override async Task<bool> OnSelect()
    {
        if (MobRosterRelic.For(Owner) is not { } crew) return false;
        if (!crew.OverpoweredMode) return crew.ReviveRandom(Owner.Creature.MaxHp);

        CardModel[] options = crew.HealthByMember().Where(member => member.Hp >= 0)
            .Select(member => FighterOption.ForRestSite(Owner, member.Fighter, member.Hp)).ToArray();
        var prefs = new CardSelectorPrefs(Title, 0, 1);
        var choices = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), options, Owner, prefs);
        return choices.FirstOrDefault() is FighterOption selected && await crew.RestoreFromGravestone(selected.Fighter);
    }

    [HarmonyPatch(typeof(RestSiteOption), "IconPath", MethodType.Getter)]
    private static class RevivalIcon
    {
        private static void Postfix(RestSiteOption __instance, ref string __result)
        {
            if (__instance is FreeRevivalOption option) __result = option.Image;
        }
    }

    [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Hooks.Hook), "ShouldDisableRemainingRestSiteOptions")]
    private static class KeepOtherActions
    {
        private static void Postfix(Player player, ref bool __result)
        {
            var choices = player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).RestSiteChoices;
            if (choices?.LastOrDefault() == "MOB_REVIVE") __result = false;
        }
    }
}

public sealed class MobSpellHat : MobRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override string IconBaseName => "bag_of_preparation";
    internal static Task RemoveDebuff(Player player)
    {
        var debuffs = player.Creature.Powers.Where(power => power.GetTypeForAmount(power.Amount) == PowerType.Debuff);
        return player.Relics.OfType<MobSpellHat>().Any() && player.RunState.Rng.CombatTargets.NextItem(debuffs) is { } debuff
            ? PowerCmd.Remove(debuff) : Task.CompletedTask;
    }
}

public sealed class MobWoundsRelic : MobRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;
    protected override string IconBaseName => "girya";
    public override Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants) =>
        participants.Contains(Owner.Creature) && Owner.Creature.CombatState is { } combat
            ? CreatureCmd.Damage(context, combat.HittableEnemies.Where(target => target.GetPowerAmount<WeakPower>() > 0),
                4, ValueProp.Unpowered, Owner.Creature) : Task.CompletedTask;
}

public sealed class MobPledgedMember : MobRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override string IconBaseName => "lantern";
    public override Task AfterObtained()
    {
        MobRosterRelic.For(Owner)?.SacrificeRear();
        return Task.CompletedTask;
    }
    public override decimal ModifyMaxEnergy(Player player, decimal amount) => player == Owner ? amount + 1 : amount;
    public override bool IsAllowedAtNeow(Player player) => false;
}

public sealed class MobPilferedGoods : MobRelic
{
    internal const int OriginalRage = 2;
    public override RelicRarity Rarity => RelicRarity.Shop;
    public override int MerchantCost => 0;
    protected override string IconBaseName => "bag_of_preparation";
    internal static async Task AddCard(Player player)
    {
        if (player.Relics.OfType<MobPilferedGoods>().FirstOrDefault() is not { } relic || player.Creature.CombatState is not { } combat ||
            player.RunState.Rng.CombatCardGeneration.NextItem(ModelDb.CardPool<ColorlessCardPool>().AllCards
                .Where(card => card.CanBeGeneratedInCombat && card.Rarity is CardRarity.Uncommon or CardRarity.Rare)) is not { } canonical) return;
        relic.Flash();
        var card = combat.CreateCard(canonical, player);
        card.AddKeyword(CardKeyword.Ethereal);
        card.AddKeyword(CardKeyword.Exhaust);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
    }
}
