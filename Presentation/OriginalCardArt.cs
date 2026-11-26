using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace GremlinMob2;

[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
internal static class OriginalCardArt
{
    private static readonly string[] Fighters = ["thief", "fat", "tsundere", "sneaky", "wiz", "nob"];
    private static readonly Lazy<Dictionary<string, string>> Art = new(() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(Godot.FileAccess.GetFileAsString(
            "res://gremlin_mob_2/original/card_art.json")) ?? []);

    private static void Postfix(NCard __instance, TextureRect ____frame)
    {
        if (!GodotObject.IsInstanceValid(____frame)) return;
        var overlay = ____frame.GetNodeOrNull<TextureRect>("GremlinCardArt");
        var model = __instance.Model;
        bool alternate = model is { IsMutable: true, Owner: not null } ? SkinPicker.UseFor(model.Owner) : SkinPicker.PreviewAlternate;
        string? fighter = model switch
        {
            MobDance { DisplayedFighter: > 0 } dance => Fighters[dance.DisplayedFighter - 1],
            MobCard card => Art.Value.GetValueOrDefault(card.GetType().Name),
            _ => null
        };
        if (fighter == null || __instance.Visibility != ModelVisibility.Visible)
        {
            overlay?.Hide();
            return;
        }
        string sticker = "card_art/" + model!.Type.ToString().ToLowerInvariant() + "_" + fighter;
        string? path = alternate ? OriginalAssets.Picture("feminization/" + sticker) : null;
        path ??= OriginalAssets.Picture(sticker);
        if (path == null)
        {
            overlay?.Hide();
            return;
        }
        if (overlay == null)
        {
            overlay = new TextureRect
            {
                Name = "GremlinCardArt",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
            };
            ____frame.AddChild(overlay);
            overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
        overlay.Texture = ResourceLoader.Load<Texture2D>(path);
        overlay.Show();
    }

    internal static void RefreshDance(Player player)
    {
        if (player.PlayerCombatState?.Hand is not { } hand) return;
        foreach (var card in hand.Cards.OfType<MobDance>())
            NCard.FindOnTable(card)?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
    }
}
