using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace GremlinMob2;

public sealed class MobRotationBell : MobRosterRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override string IconBaseName => "lantern";
    public override bool IsAllowedInShops => false;
    public override bool IsAllowedAtNeow(Player player) => false;
}
