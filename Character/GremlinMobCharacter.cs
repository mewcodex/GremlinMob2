using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;

namespace GremlinMob2;

public sealed class GremlinMobCharacter : CharacterModel
{
    internal static CharacterModel Placeholder => ModelDb.Character<Ironclad>();
    public override CharacterGender Gender => CharacterGender.Neutral;
    protected override CharacterModel? UnlocksAfterRunAs => null;
    public override Color NameColor => new("b9d686");
    public override Color MapDrawingColor => NameColor;
    public override Color DreadNameColor => Color.FromHsv(NameColor.H, Math.Clamp(NameColor.S, .65f, .9f), 1, .75f);
    protected override string IconPath => "res://gremlin_mob_2/character_icon.tscn";
    protected override string CharacterSelectIconPath => "res://gremlin_mob_2/char_select.ctex";
    protected override string CharacterSelectLockedIconPath => "res://gremlin_mob_2/char_select_locked.ctex";
    protected override string MapMarkerPath => "res://gremlin_mob_2/map_marker.ctex";
    protected override IEnumerable<string> ExtraAssetPaths =>
    [
        "res://images/ui/top_panel/character_icon_gremlin_mob_character_outline.png",
        "res://gremlin_mob_2/original/char/shoulder2.res",
        "res://gremlin_mob_2/original/char/corpse.res"
    ];
    public override int StartingHp => 16;
    public override int StartingGold => 99;
    public override float AttackAnimDelay => Placeholder.AttackAnimDelay;
    public override float CastAnimDelay => Placeholder.CastAnimDelay;
    public override CardPoolModel CardPool => ModelDb.CardPool<MobCards>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<MobRelics>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<MobPotions>();
    public override IEnumerable<CardModel> StartingDeck => Enumerable.Repeat<CardModel>(ModelDb.Card<MobStrike>(), 4)
        .Concat(Enumerable.Repeat(ModelDb.Card<MobDefend>(), 4))
        .Concat([ModelDb.Card<MobPickFront>(), ModelDb.Card<MobDance>()]);
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<MobRotationBell>()];
    public override string DeathSfx => Placeholder.DeathSfx;
    public override string CharacterSelectSfx => Placeholder.CharacterSelectSfx;
    public override string CharacterTransitionSfx => Placeholder.CharacterTransitionSfx;
    public override List<string> GetArchitectAttackVfx() => Placeholder.GetArchitectAttackVfx();
    public override CreatureAnimator GenerateAnimator(MegaSprite controller, Creature creature) =>
        Placeholder.GenerateAnimator(controller, creature);
}
