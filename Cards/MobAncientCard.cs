using MegaCrit.Sts2.Core.Entities.Cards;

namespace GremlinMob2;

public abstract class MobAncientCard(int cost, CardType type, TargetType target)
    : MobCard(cost, type, CardRarity.Ancient, target)
{
    public override string PortraitPath => "res://gremlin_mob_2/original/cards/ancient_placeholder.res";
}
