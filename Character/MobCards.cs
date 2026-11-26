using Godot;
using MegaCrit.Sts2.Core.Models;

namespace GremlinMob2;

public sealed class MobCards : CardPoolModel
{
    public override string Title => "gremlin_mob_2";
    public override string EnergyColorName => "gremlin_mob_2";
    public override string CardFrameMaterialPath => GremlinMobCharacter.Placeholder.CardPool.CardFrameMaterialPath;
    public override Color DeckEntryCardColor => new("b9d686");
    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards() => Bootstrap.CharacterCards
        .Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type))).ToArray();
}
