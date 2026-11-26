using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GremlinMob2;

public abstract class MobRelic : RelicModel
{
    public override string PackedIconPath => OriginalAssets.RelicIcon(this, "small") ?? base.PackedIconPath;
    protected override string PackedIconOutlinePath => OriginalAssets.RelicIcon(this, "outline") ?? base.PackedIconOutlinePath;
    protected override string BigIconPath => OriginalAssets.RelicIcon(this, "large") ?? base.BigIconPath;
}

public sealed class MobReserveTalisman : MobRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override string IconBaseName => "miniature_tent";
}

public sealed class MobReserveBomb : MobRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override string IconBaseName => "miniature_cannon";
}

public sealed class MobChargeMallet : MobRelic
{
    private int _used;
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override string IconBaseName => "girya";
    public override Task BeforeCombatStart() { _used = 0; return Task.CompletedTask; }
    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext context, Player player)
    {
        if (player == Owner) _used = 0;
        return Task.CompletedTask;
    }
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not WeakPower || amount <= 0 || applier != Owner.Creature || _used >= 3) return;
        _used++;
        if (MobRosterRelic.For(Owner) is { } crew) await crew.Charge(context, 1);
    }
}

public sealed class MobSpellStaff : MobRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override string IconBaseName => "vajra";
}

public sealed class MobRotatingBell : MobRosterRelic
{
    private int _turn = -1;
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override string IconBaseName => "lantern";
    public override bool IsAllowedAtNeow(Player player) => false;
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        if (Owner.Character is not GremlinMobCharacter ||
            Owner.Relics.OfType<MobRotationBell>().FirstOrDefault() is not { } starter) return;
        GremlinMobLine = starter.GremlinMobLine;
        OverpoweredMode = starter.OverpoweredMode;
        await RelicCmd.Remove(starter);
    }

    public override Task BeforeCombatStart()
    {
        _turn = -1;
        return base.BeforeCombatStart();
    }

    public override async Task AfterShuffle(PlayerChoiceContext context, Player shuffler)
    {
        if (IsMelted || shuffler != Owner || Owner.PlayerCombatState is not { } state || state.TurnNumber == _turn) return;
        _turn = state.TurnNumber;
        Flash();
        await PlayerCmd.GainEnergy(1, Owner);
        await CardPileCmd.Draw(context, 1, Owner);
        if (MobRosterRelic.For(Owner) is { } crew) await crew.Rotate(context, fromRelic: true);
    }
}

public sealed class MobRecruitPennant : MobRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    protected override string IconBaseName => "bag_of_preparation";
    public override async Task AfterObtained()
    {
        for (int i = 0; i < 2; i++)
            await CardPileCmd.Add(Owner.RunState.CreateCard<MobPickFront>(Owner), PileType.Deck);
    }
    public override Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw) =>
        card is MobPickFront && card.Owner == Owner ? CardPileCmd.Draw(context, 1, Owner) : Task.CompletedTask;
}
