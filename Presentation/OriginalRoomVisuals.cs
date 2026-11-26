using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace GremlinMob2;

internal static class OriginalRoomVisuals
{
    private static readonly string[] Names = ["angry", "fat", "shield", "sneak", "wizard"];

    private static void ShowMerchantFigures(NMerchantCharacter host, Player player)
    {
        if (MobRosterRelic.For(player) is not { } roster || OriginalAssets.Picture("char/angry/skeleton") == null) return;
        if (host.HasNode("GremlinMobMerchantVisuals")) return;

        bool alternate = SkinPicker.UseFor(player);
        // The single-player merchant has a full rug for the party; multiplayer uses a grid of character slots.
        bool singlePlayer = player.RunState.Players.Count == 1;
        float spacing = singlePlayer ? (alternate ? 98f : 72f) : (alternate ? 76f : 48f);
        float scale = singlePlayer ? 1.10f : .60f;
        var formation = roster.Formation();
        var display = new Node2D { Name = "GremlinMobMerchantVisuals" };
        host.AddChild(display);
        for (int i = 0; i < formation.Length; i++)
        {
            string name = Names[(int)formation[i].Fighter];
            if ((OriginalAssets.Spine(name, alternate) ?? OriginalAssets.Spine(name)) is not { } sprite) continue;
            sprite.Scale = Vector2.One * scale;
            sprite.Position = new Vector2(((formation.Length - 1) * .5f - i) * spacing, 0);
            display.AddChild(sprite);
            OriginalAssets.PlayIdle(sprite, name);
        }

        if (display.GetChildCount() == 0)
        {
            display.QueueFree();
            return;
        }
        foreach (var old in host.GetChildren().OfType<Node2D>().Where(node => node.GetClass() == "SpineSprite"))
            old.Modulate = Colors.Transparent;
    }

    [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
    private static class RestFigures
    {
        private static void Postfix(NRestSiteCharacter __instance)
        {
            if (MobRosterRelic.For(__instance.Player) == null) return;
            foreach (var old in __instance.GetChildren().OfType<Node2D>().Where(node => node.GetClass() == "SpineSprite"))
                old.Modulate = Colors.Transparent;
            var texture = ResourceLoader.Load<Texture2D>("res://gremlin_mob_2/original/char/shoulder2.res");
            var region = texture.GetImage().GetUsedRect();
            __instance.AddChild(new Sprite2D
            {
                Name = "GremlinMobOriginalRoomVisuals",
                Texture = new AtlasTexture { Atlas = texture, Region = new Rect2(region.Position, region.Size) },
                Centered = false,
                Position = new Vector2(-region.Size.X * .5f, -region.Size.Y)
            });
        }
    }

    [HarmonyPatch(typeof(NMerchantRoom), "AfterRoomIsLoaded")]
    private static class MerchantFigures
    {
        private static void Postfix(List<Player> ____players, List<NMerchantCharacter> ____playerVisuals)
        {
            for (int i = 0; i < Math.Min(____players.Count, ____playerVisuals.Count); i++)
                ShowMerchantFigures(____playerVisuals[i], ____players[i]);
        }
    }
}
