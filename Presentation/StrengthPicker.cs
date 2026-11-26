using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace GremlinMob2;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class StrengthPicker
{
    private const string ConfigPath = "user://gremlin_mob_strength.cfg";
    internal static bool Overpowered { get; private set; } = LoadChoice();

    private static bool LoadChoice()
    {
        var config = new ConfigFile();
        return config.Load(ConfigPath) == Error.Ok && config.GetValue("strength", "overpowered", false).AsBool();
    }

    private static void Postfix(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton)
    {
        if (__instance.GetNodeOrNull<OptionButton>("GremlinStrengthPicker") is { } previous)
        {
            __instance.RemoveChild(previous);
            previous.QueueFree();
        }
        if (charSelectButton.IsLocked || charSelectButton.Character is not GremlinMobCharacter ||
            __instance.Lobby.NetService.Type != NetGameType.Singleplayer) return;

        var selector = new OptionButton
        {
            Name = "GremlinStrengthPicker",
            CustomMinimumSize = new Vector2(160, 42)
        };
        selector.AddThemeFontSizeOverride("font_size", 22);
        selector.AddItem("传统模式");
        selector.AddItem("超模模式");
        selector.Select(Overpowered ? 1 : 0);
        selector.ItemSelected += index =>
        {
            Overpowered = index == 1;
            var config = new ConfigFile();
            config.SetValue("strength", "overpowered", Overpowered);
            config.Save(ConfigPath);
        };
        __instance.AddChild(selector);
        selector.AnchorLeft = selector.AnchorRight = .15f;
        selector.AnchorTop = selector.AnchorBottom = .64f;
        selector.OffsetTop = -50;
        selector.OffsetBottom = -8;
    }

    [HarmonyPatch(typeof(NGame), nameof(NGame.StartNewSingleplayerRun))]
    private static class ApplyStrengthToNewRun
    {
        private static void Prefix(Player player)
        {
            if (MobRosterRelic.For(player) is { } crew)
                crew.OverpoweredMode = Overpowered;
        }
    }
}
