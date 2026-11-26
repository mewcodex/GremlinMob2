using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

public abstract class MobCard(int cost, CardType type, CardRarity rarity, TargetType target, bool show = true)
    : CardModel(cost, type, rarity, target, show)
{
    public override CardPoolModel Pool => Rarity == CardRarity.Token ? ModelDb.CardPool<TokenCardPool>() : ModelDb.CardPool<MobCards>();
    public override bool CanBeGeneratedInCombat => Rarity is not (CardRarity.Token or CardRarity.Ancient);
    public override bool CanBeGeneratedByModifiers => Rarity is not (CardRarity.Token or CardRarity.Ancient);
    public override string PortraitPath => OriginalAssets.Icon(this) ?? base.PortraitPath;
    public override string BetaPortraitPath => OriginalAssets.Icon(this, beta: true) ?? PortraitPath;
    // The native inspect screen loads this path and falls back to PortraitPath when it is absent.
    protected override string PortraitPngPath => OriginalAssets.LargeCardPortrait(this) ?? base.PortraitPngPath;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => LegacyLocalization.KeywordTips(this).Concat(OriginalCardPreview.GetTips(this));
    internal string DisplayName => LegacyLocalization.Title(this, "cards");
    protected override void AddExtraArgsToDescription(LocString description)
    {
        if (DynamicVars.TryGetValue("Amount", out var magic))
        {
            description.AddObj("OriginalMagic", magic);
            return;
        }
        int amount = this switch
        {
            MobDance or MobWizardSchool or MobPocketLift => 2,
            MobOpenTheWound => 3,
            MobPatchworkArmor => IsUpgraded ? 1 : 0,
            _ => 0
        };
        description.Add(new DynamicVar("OriginalMagic", amount));
    }

    private protected MobRosterRelic? Crew => MobRosterRelic.For(Owner);
    protected Creature Body => Owner.Creature;
    protected int ChargeCount => Body.GetPowerAmount<MobChargePower>();
    protected int CardsPlayed => CombatManager.Instance.History.CardPlaysStarted.Count(entry => entry.Actor == Body && entry.HappenedThisTurn(CombatState!));
    protected Task Rotate(PlayerChoiceContext context) => Crew?.Rotate(context) ?? Task.CompletedTask;
    private protected Task Rotate(PlayerChoiceContext context, Fighter fighter) => Crew?.Rotate(context, fighter) ?? Task.CompletedTask;
    protected Task Wards(int count, bool upgraded = false) => AddCards<MobPaperWard>(count, upgraded);

    protected Task Attack(PlayerChoiceContext context, CardPlay play, decimal damage, int hits = 1, bool all = false)
    {
        var command = DamageCmd.Attack(damage).FromCard(this, play).WithHitCount(hits);
        if (all) command.TargetingAllOpponents(CombatState!);
        else if (play.Target is { } target) command.Targeting(target);
        else return Task.CompletedTask;
        return command.WithHitFx("vfx/vfx_attack_slash").Execute(context);
    }

    protected Task Block(CardPlay play, decimal amount) => CreatureCmd.GainBlock(Body, amount, ValueProp.Move, play);
    private protected async Task PlayDance(PlayerChoiceContext context, CardPlay play, Fighter[] dancers, bool nob)
    {
        bool all = dancers.Contains(Fighter.Brute);
        var targets = all ? CombatState!.HittableEnemies.ToArray() : play.Target is { } target ? [target] : [];
        if (dancers.Contains(Fighter.Guard)) await Block(play, DynamicVars.Block.BaseValue);
        await Attack(context, play, DynamicVars.Damage.BaseValue, all: all);
        if (dancers.Contains(Fighter.Bruiser))
            foreach (var enemy in targets.Where(enemy => enemy.IsAlive))
                await Apply<MobWeakenedStrength>(context, 2, enemy);
        if (dancers.Contains(Fighter.Scout)) await Draw(context, 2);
        if (dancers.Contains(Fighter.Mage)) await Charge(context, 2);
        if (nob) await Apply<StrengthPower>(context, 2);
    }

    protected Task Apply<TPower>(PlayerChoiceContext context, decimal amount, Creature? target = null) where TPower : PowerModel =>
        PowerCmd.Apply<TPower>(context, target ?? Body, amount, Body, this);
    protected Task Charge(PlayerChoiceContext context, int amount) => Apply<MobChargePower>(context, amount);
    protected Task Draw(PlayerChoiceContext context, int amount) => CardPileCmd.Draw(context, amount, Owner);
    protected Task Energy(int amount) => PlayerCmd.GainEnergy(amount, Owner);
    protected Task Shivs(int count, bool upgraded = false) => AddCards<Shiv>(count, upgraded);
    protected async Task AddCards<TCard>(int count, bool upgraded = false, PileType pile = PileType.Hand, CardPilePosition position = CardPilePosition.Bottom) where TCard : CardModel
    {
        if (CombatState == null) return;
        var cards = Enumerable.Range(0, Math.Max(0, count)).Select(_ => CombatState.CreateCard<TCard>(Owner)).ToList();
        if (upgraded)
            foreach (var card in cards) CardCmd.Upgrade(card, CardPreviewStyle.None);
        await CardPileCmd.AddGeneratedCardsToCombat(cards, pile, Owner, position);
    }

    private protected async Task ChooseFighter(PlayerChoiceContext context)
    {
        if (Crew is not { } crew || CombatState is not { } combat || crew.Reserves.Length == 0) return;
        var options = crew.Reserves.Select(fighter => combat.CreateCard(FighterOption.For(fighter), Owner)).ToArray();
        var selection = await CardSelectCmd.FromSimpleGrid(context, options, Owner, new CardSelectorPrefs(new LocString("card_selection", "GREMLIN_MOB_CHOOSE_FRONT"), 1));
        if (selection.FirstOrDefault() is FighterOption selected)
            await crew.Rotate(context, selected.Fighter);
    }
}
