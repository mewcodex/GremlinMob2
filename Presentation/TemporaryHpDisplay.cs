using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace GremlinMob2;

[HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.SetCreature))]
internal static class TemporaryHpDisplay
{
    // StSLib draws the shield and all three original Block outline textures in this gold.
    private static readonly Color Gold = new("EFC851");
    private const string TextureRoot = "res://gremlin_mob_2/temporary_hp/";

    private static void Postfix(NHealthBar __instance, Creature creature)
    {
        if (creature.Player?.Character is not GremlinMobCharacter) return;
        if (__instance.GetNodeOrNull<MegaLabel>("%BlockLabel") is not { } blockLabel ||
            __instance.GetNodeOrNull<Control>("%BlockOutline") is not { } blockOutline ||
            __instance.GetNodeOrNull<Control>("%HpForegroundContainer") is not { } foreground ||
            !ResourceLoader.Exists(TextureRoot + "shield.res")) return;

        Control bar = __instance.HpBarContainer;
        var outline = new Control { Name = "TemporaryHpOutline", MouseFilter = Control.MouseFilterEnum.Ignore };
        bar.AddChild(outline);
        bar.MoveChild(outline, blockOutline.GetIndex());
        var additive = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        var left = AddTexture(outline, "blockLeft3", additive);
        var body = AddTexture(outline, "blockBody3", additive);
        var right = AddTexture(outline, "blockRight3", additive);

        var badge = AddTexture(bar, "shield");
        badge.Name = "TemporaryHpBadge";
        var label = new MegaLabel
        {
            Name = "Amount",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutoSizeEnabled = true,
            MinFontSize = 8,
            MaxFontSize = blockLabel.GetThemeFontSize("font_size")
        };
        label.AddThemeFontOverride("font", blockLabel.GetThemeFont("font"));
        label.AddThemeColorOverride("font_color", blockLabel.GetThemeColor("font_color"));
        label.AddThemeColorOverride("font_outline_color", new Color("523610"));
        label.AddThemeConstantOverride("outline_size", blockLabel.GetThemeConstant("outline_size"));
        badge.AddChild(label);

        void Layout()
        {
            Vector2 size = bar.Size;
            float height = size.Y;
            // The additive rim should surround the native blue Block frame instead of blending over it.
            float gutter = height * .375f;
            // The old texture has transparent padding on its outer side. Align the visible caps to the HP inset.
            float capPadding = height * .3125f;
            float leftJoin = foreground.OffsetLeft + capPadding;
            float rightJoin = size.X + foreground.OffsetRight - capPadding;
            left.Position = new Vector2(leftJoin - height, -gutter);
            left.Size = new Vector2(height, height + 2 * gutter);
            body.Position = new Vector2(leftJoin, -gutter);
            body.Size = new Vector2(Mathf.Max(0, rightJoin - leftJoin), left.Size.Y);
            right.Position = new Vector2(rightJoin, -gutter);
            right.Size = left.Size;

            float diameter = blockLabel.GetParent<Control>().Size.Y;
            badge.Size = new Vector2(diameter, diameter);
            badge.Position = new Vector2(size.X - diameter * (30f / 64f), (height - diameter) / 2f);
            label.Size = badge.Size;
        }

        MobTemporaryGuard? guard = null;
        void Refresh()
        {
            int amount = guard?.Amount ?? 0;
            outline.Visible = badge.Visible = amount > 0 && creature.IsAlive;
            if (badge.Visible)
                label.SetTextAutoSize(amount.ToString());
        }

        void SyncGuard()
        {
            if (guard != null) guard.DisplayAmountChanged -= Refresh;
            guard = creature.GetPower<MobTemporaryGuard>();
            if (guard != null) guard.DisplayAmountChanged += Refresh;
            Refresh();
        }

        void OnPowerChanged(PowerModel power)
        {
            if (power is MobTemporaryGuard) SyncGuard();
        }

        void OnHealthChanged(int _, int __) => Refresh();

        bar.Resized += Layout;
        creature.PowerApplied += OnPowerChanged;
        creature.PowerRemoved += OnPowerChanged;
        creature.CurrentHpChanged += OnHealthChanged;
        bar.TreeExiting += () =>
        {
            bar.Resized -= Layout;
            creature.PowerApplied -= OnPowerChanged;
            creature.PowerRemoved -= OnPowerChanged;
            creature.CurrentHpChanged -= OnHealthChanged;
            if (guard != null) guard.DisplayAmountChanged -= Refresh;
        };
        Layout();
        SyncGuard();
    }

    private static TextureRect AddTexture(Control parent, string name, Material? material = null)
    {
        var texture = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(TextureRoot + name + ".res"),
            SelfModulate = Gold,
            Material = material,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale
        };
        parent.AddChild(texture);
        return texture;
    }
}
