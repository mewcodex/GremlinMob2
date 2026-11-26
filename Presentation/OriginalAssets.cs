using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;

namespace GremlinMob2;

internal static class OriginalAssets
{
    private const string Root = "res://gremlin_mob_2/original/";
    private static readonly Lazy<Dictionary<string, string>> Bindings = new(() =>
        Godot.FileAccess.FileExists(Root + "catalog.json")
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(Godot.FileAccess.GetFileAsString(Root + "catalog.json")) ?? []
            : []);
    private static readonly Dictionary<string, Resource> Rigs = [];

    internal static string? Icon(object model, bool beta = false) => Bindings.Value.GetValueOrDefault(model.GetType().Name + (beta ? ":beta" : ""));
    internal static string? LargeCardPortrait(MobCard card) => Bindings.Value.GetValueOrDefault(card.GetType().Name + ":large");
    internal static string? RelicIcon(MobRelic relic, string variant) => Bindings.Value.GetValueOrDefault(relic.GetType().Name + ":" + variant);

    internal static string? Picture(string name)
    {
        string path = Root + name + ".res";
        return ResourceLoader.Exists(path) ? path : null;
    }

    internal static Node2D? Spine(string fighter, bool alternate = false)
    {
        string path = Root + (alternate ? "feminization/" : "") + "char/" + fighter + "/skeleton";
        if (!Godot.FileAccess.FileExists(path + ".atlas")) return null;

        if (!Rigs.TryGetValue(path, out var rig))
        {
            var atlas = ClassDB.Instantiate("SpineAtlasResource").As<Resource>();
            var source = ClassDB.Instantiate("SpineSkeletonFileResource").As<Resource>();
            int atlasError = atlas.Call("load_from_atlas_file", path + ".atlas").AsInt32();
            string skeletonPath = path + (Godot.FileAccess.FileExists(path + ".skel") ? ".skel" : ".spine-json");
            int skeletonError = source.Call("load_from_file", skeletonPath).AsInt32();
            if (atlasError != (int)Error.Ok || skeletonError != (int)Error.Ok)
            {
                Log.Error($"Gremlin Mob: Spine import failed for {fighter} (atlas {atlasError}, skeleton {skeletonError}).");
                return null;
            }
            rig = ClassDB.Instantiate("SpineSkeletonDataResource").As<Resource>();
            rig.Set("atlas_res", atlas);
            rig.Set("skeleton_file_res", source);
            Rigs.Add(path, rig);
        }

        var sprite = ClassDB.Instantiate("SpineSprite").As<Node2D>();
        sprite.Name = (alternate ? "FeminizationSpine_" : "OriginalSpine_") + fighter;
        sprite.Call("set_skeleton_data_res", rig);
        return sprite;
    }

    internal static void PlayIdle(Node2D sprite, string fighter)
    {
        string animation = fighter is "angry" or "shield" ? "idle" : "animation";
        sprite.RunWhenSpineReady(new MegaSprite(sprite), state =>
        {
            state.SetAnimation(animation);
            using var track = state.GetCurrent(0);
            if (track != null) track.SetTrackTime(track.GetAnimationDuration() * GD.Randf());
        });
    }
}
