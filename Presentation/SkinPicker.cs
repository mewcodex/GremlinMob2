using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace GremlinMob2;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class SkinPicker
{
    private const string ConfigPath = "user://gremlin_mob_skin.cfg";
    private static readonly Lazy<bool> Available = new(() =>
        new[] { "angry", "fat", "shield", "sneak", "wizard", "nob" }.All(fighter => Godot.FileAccess.FileExists(
            $"res://gremlin_mob_2/original/feminization/char/{fighter}/skeleton.atlas")) &&
        OriginalAssets.Picture("feminization/ui/left") != null && OriginalAssets.Picture("feminization/ui/right") != null);
    private static bool _alternate = LoadChoice();
    internal static bool PreviewAlternate => Available.Value && _alternate;

    internal static bool UseFor(Player? player) => player != null && PreviewAlternate &&
        (!LocalContext.NetId.HasValue || LocalContext.IsMe(player));

    private static bool LoadChoice()
    {
        var config = new ConfigFile();
        return config.Load(ConfigPath) == Error.Ok && config.GetValue("appearance", "feminization", false).AsBool();
    }

    private static void Postfix(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton)
    {
        if (__instance.GetNodeOrNull<Control>("GremlinSkinPicker") is { } previous)
        {
            __instance.RemoveChild(previous);
            previous.QueueFree();
        }
        if (!Available.Value || charSelectButton.IsLocked || charSelectButton.Character is not GremlinMobCharacter)
            return;
        var selector = new HBoxContainer { Name = "GremlinSkinPicker", MouseFilter = Control.MouseFilterEnum.Ignore };
        selector.AddThemeConstantOverride("separation", 12);
        __instance.AddChild(selector);
        selector.AnchorLeft = selector.AnchorRight = .15f;
        selector.AnchorTop = selector.AnchorBottom = .64f;
        const string originalLabel = "原版";
        const string alternateLabel = "娘化皮肤 by anaertailin";
        var label = new Label
        {
            Name = "SkinChoice",
            Text = alternateLabel,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(104, 48),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", 23);
        label.AddThemeColorOverride("font_color", new Color("efc971"));
        var left = Arrow("left");
        var right = Arrow("right");
        selector.AddChild(left);
        selector.AddChild(label);
        selector.AddChild(right);

        label.CustomMinimumSize = label.GetCombinedMinimumSize();
        void Refresh() => label.Text = _alternate ? alternateLabel : originalLabel;
        void Toggle()
        {
            _alternate = !_alternate;
            var config = new ConfigFile();
            config.SetValue("appearance", "feminization", _alternate);
            config.Save(ConfigPath);
            Refresh();
        }
        left.Pressed += Toggle;
        right.Pressed += Toggle;
        Refresh();
    }

    private static TextureButton Arrow(string direction)
    {
        var button = new TextureButton
        {
            Name = direction == "left" ? "PreviousSkin" : "NextSkin",
            TextureNormal = ResourceLoader.Load<Texture2D>(OriginalAssets.Picture("feminization/ui/" + direction)!),
            IgnoreTextureSize = true,
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(56, 56),
            SelfModulate = Colors.LightGray
        };
        button.MouseEntered += () => button.SelfModulate = Colors.White;
        button.MouseExited += () => button.SelfModulate = button.HasFocus() ? Colors.White : Colors.LightGray;
        button.FocusEntered += () => button.SelfModulate = Colors.White;
        button.FocusExited += () => button.SelfModulate = Colors.LightGray;
        return button;
    }
}
