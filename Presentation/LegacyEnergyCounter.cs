using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace GremlinMob2;

internal static class LegacyEnergyCounter
{
    private sealed record Layer(TextureRect Node, Texture2D Lit, Texture2D Dark);
    private sealed record Orb(Control Host, Layer[] Layers, TextureRect Flash);
    private static readonly ConditionalWeakTable<NEnergyCounter, Orb> Orbs = new();

    private static Texture2D Load(string name) =>
        ResourceLoader.Load<Texture2D>("res://gremlin_mob_2/original/char/orb/" + name + ".res");

    private static TextureRect Create(Texture2D texture, Vector2 size) => new()
    {
        Texture = texture,
        Size = size,
        PivotOffset = size / 2,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        MouseFilter = Control.MouseFilterEnum.Ignore
    };

    private static void Refresh(Orb orb, bool dark)
    {
        orb.Host.Modulate = Colors.White;
        foreach (var layer in orb.Layers)
        {
            layer.Node.Material = null;
            layer.Node.Texture = dark ? layer.Dark : layer.Lit;
        }
    }

    [HarmonyPatch(typeof(NEnergyCounter), nameof(NEnergyCounter._Ready))]
    private static class Install
    {
        private static void Postfix(NEnergyCounter __instance, Player ____player)
        {
            if (____player.Character is not GremlinMobCharacter || OriginalAssets.Picture("char/orb/layer6") == null) return;
            Control host = __instance.GetNode<Control>("%Layers");
            Control rotating = __instance.GetNode<Control>("%RotationLayers");
            foreach (var node in host.GetChildren().Where(child => child != rotating).Concat(rotating.GetChildren()).ToArray())
            {
                node.GetParent().RemoveChild(node);
                node.QueueFree();
            }
            __instance.GetNodeOrNull<CanvasItem>("%EnergyVfxBack")?.Hide();
            __instance.GetNodeOrNull<CanvasItem>("%EnergyVfxFront")?.Hide();

            var layers = new List<Layer>(6);
            Vector2 size = new(128, 128);
            for (int i = 1; i <= 6; i++)
            {
                var lit = Load("layer" + i);
                var dark = i < 6 ? Load("layer" + i + "d") : lit;
                var view = Create(lit, size);
                view.Name = "GremlinOrbLayer" + i;
                (i < 6 ? rotating : host).AddChild(view);
                layers.Add(new(view, lit, dark));
            }
            var flash = Create(Load("vfx"), new Vector2(256, 256));
            flash.Name = "GremlinOrbFlash";
            flash.Position = -size / 2;
            flash.Modulate = Colors.Transparent;
            host.AddChild(flash);
            var orb = new Orb(host, layers.ToArray(), flash);
            Orbs.Add(__instance, orb);
            Refresh(orb, ____player.PlayerCombatState?.Energy == 0);
        }
    }

    [HarmonyPatch(typeof(NEnergyCounter), "RefreshLabel")]
    private static class UseOriginalZeroEnergyLayer
    {
        private static void Postfix(NEnergyCounter __instance, Player ____player)
        {
            if (Orbs.TryGetValue(__instance, out var orb)) Refresh(orb, ____player.PlayerCombatState?.Energy == 0);
        }
    }

    [HarmonyPatch(typeof(NEnergyCounter), "OnEnergyChanged")]
    private static class ShowOriginalFlash
    {
        private static void Postfix(NEnergyCounter __instance, int oldEnergy, int newEnergy)
        {
            if (!Orbs.TryGetValue(__instance, out var orb)) return;
            Refresh(orb, newEnergy == 0);
            if (newEnergy <= oldEnergy) return;
            orb.Flash.Modulate = Colors.White;
            orb.Flash.CreateTween().TweenProperty(orb.Flash, "modulate:a", 0f, .36);
        }
    }
}
