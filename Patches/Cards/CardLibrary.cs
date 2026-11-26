using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace GremlinMob2;

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
internal static class GremlinCardLibrary
{
    private static readonly Action<NCardLibrary, NCardPoolFilter> UpdatePool =
        AccessTools.MethodDelegate<Action<NCardLibrary, NCardPoolFilter>>(
            AccessTools.Method(typeof(NCardLibrary), "UpdateCardPoolFilter"));

    private static void Postfix(NCardLibrary __instance, NCardPoolFilter ____foolFilter,
        Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters,
        Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters)
    {
        var character = ModelDb.Character<GremlinMobCharacter>();
        if (____cardPoolFilters.ContainsKey(character)) return;
        var filter = (NCardPoolFilter)____foolFilter.Duplicate((int)Node.DuplicateFlags.Scripts);
        filter.Name = "GremlinMobPool";
        filter.Visible = true;
        filter.Loc = new LocString("characters", character.Id.Entry + ".title");

        var image = filter.GetNode<TextureRect>("Image");
        image.Texture = character.IconTexture;
        image.Material = (Material)image.Material.Duplicate();

        var reticle = filter.FindChild("SelectionReticle", owned: false);
        if (reticle != null)
        {
            reticle.Owner = filter;
            reticle.UniqueNameInOwner = true;
        }
        var row = ____foolFilter.GetParent();
        row.AddChild(filter);
        row.MoveChild(filter, ____foolFilter.GetIndex() + 1);

        ____cardPoolFilters.Add(character, filter);
        ____poolFilters.Add(filter, card => card.Pool == character.CardPool && card.Rarity != CardRarity.Ancient);
        filter.Toggled += chosen => UpdatePool(__instance, chosen);
    }
}
