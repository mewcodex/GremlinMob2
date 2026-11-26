using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace GremlinMob2;

public abstract class DescribedMobCard(
    int cost, CardType type, CardRarity rarity, TargetType target,
    int damage = 0, int damageUpgrade = 0, int block = 0, int blockUpgrade = 0,
    int effect = 0, int effectUpgrade = 0, int costUpgrade = 0,
    CardKeyword keyword = CardKeyword.None, CardKeyword secondKeyword = CardKeyword.None,
    CardKeyword upgradeKeyword = CardKeyword.None, CardKeyword removeOnUpgrade = CardKeyword.None)
    : MobCard(cost, type, rarity, target)
{
    protected int DamageAmount => DynamicVars.Damage.IntValue;
    protected int BlockAmount => DynamicVars.Block.IntValue;
    protected int EffectAmount => DynamicVars["Amount"].IntValue;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { keyword, secondKeyword }.Where(value => value != CardKeyword.None);
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            if (damage != 0 || Type == CardType.Attack) yield return new DamageVar(damage, ValueProp.Move);
            if (block != 0 || blockUpgrade != 0) yield return new BlockVar(block, ValueProp.Move);
            if (effect != 0 || effectUpgrade != 0) yield return new DynamicVar("Amount", effect);
        }
    }

    protected override void OnUpgrade()
    {
        if (damageUpgrade != 0) DynamicVars.Damage.UpgradeValueBy(damageUpgrade);
        if (blockUpgrade != 0) DynamicVars.Block.UpgradeValueBy(blockUpgrade);
        if (effectUpgrade != 0) DynamicVars["Amount"].UpgradeValueBy(effectUpgrade);
        if (costUpgrade != 0) EnergyCost.UpgradeBy(costUpgrade);
        if (upgradeKeyword != CardKeyword.None) AddKeyword(upgradeKeyword);
        if (removeOnUpgrade != CardKeyword.None) RemoveKeyword(removeOnUpgrade);
    }
}
