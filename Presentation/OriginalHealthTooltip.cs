using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace GremlinMob2;

[HarmonyPatch(typeof(NTopBarHp), "OnFocus")]
internal static class OriginalHealthTooltip
{
    private static bool Prefix(NTopBarHp __instance, Player? ____player)
    {
        if (MobRosterRelic.For(____player) is not { } crew) return true;
        int maxHp = ____player!.Creature.MaxHp;
        var lines = crew.HealthByMember().Select(member =>
        {
            string name = ((FighterOption)FighterOption.For(member.Fighter)).DisplayName;
            return member.Hp switch
            {
                < 0 => $"[purple]{name}: TAKEN[/purple]",
                0 => $"[red]{name}: 0/{maxHp}[/red]",
                _ => $"{name}: {member.Hp}/{maxHp}"
            };
        });
        var tip = new HoverTip(new LocString("static_hover_tips", "HIT_POINTS.title"), string.Join('\n', lines));
        NHoverTipSet.CreateAndShow(__instance, tip)?.SetGlobalPosition(__instance.GlobalPosition + new Vector2(0, __instance.Size.Y + 20));
        return false;
    }
}
