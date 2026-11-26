using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace GremlinMob2;

internal sealed class MobFormation
{
    private sealed class Figure(Node2D root, Node2D art, Node2D spine, float? skinHoverHalfWidth, Label hp, Control hover)
    {
        internal Node2D Root { get; } = root;
        internal Node2D Art { get; } = art;
        internal Node2D Spine { get; } = spine;
        internal float? SkinHoverHalfWidth { get; } = skinHoverHalfWidth;
        internal Label Hp { get; } = hp;
        internal Control Hover { get; } = hover;
        internal Vector2 Target { get; set; }
        internal int Slot { get; set; } = -2;
        internal float ChannelTime { get; set; }
        internal bool HasHoverBounds { get; set; }
    }

    private const float ReserveSpacing = 90;
    private static readonly ConditionalWeakTable<NCreature, MobFormation> Formations = new();
    private static readonly string[] SpineNames = ["angry", "fat", "shield", "sneak", "wizard"];
    private static readonly Action<NCreature, Node> UpdateBounds =
        AccessTools.MethodDelegate<Action<NCreature, Node>>(AccessTools.Method(typeof(NCreature), "UpdateBounds", [typeof(Node)]));
    private readonly Dictionary<Fighter, Figure> _figures = new();
    private readonly NCreature _actor;
    private readonly Vector2 _origin;
    private readonly bool _alternate;
    private Figure? _front;
    private Node2D? _nob;
    private Sprite2D? _corpse;
    private bool _initialized;
    private float _reactionTime;
    private bool _attack;

    private MobFormation(NCreature actor)
    {
        _actor = actor;
        _alternate = SkinPicker.UseFor(actor.Entity.Player);
        _origin = actor.Visuals.Position;
        actor.Body.Modulate = Colors.Transparent;
        actor.Visuals.Bounds.Position = new Vector2(-70, -100);
        actor.Visuals.Bounds.Size = new Vector2(180, 110);
        actor.Visuals.VfxSpawnPosition.Position = new Vector2(20, -45);
        UpdateBounds(actor, actor.Visuals);
        _ = UpdateMotion(actor.GetTree());
    }

    internal static void Update(Creature body, MobRosterRelic roster)
    {
        if (NCombatRoom.Instance?.GetCreatureNode(body) is { } actor && actor.IsNodeReady())
            Attach(actor, roster);
    }

    internal static void Attach(NCreature actor, MobRosterRelic roster) =>
        Formations.GetValue(actor, node => new MobFormation(node)).Arrange(roster);

    internal static void React(Creature body, bool attack)
    {
        if (NCombatRoom.Instance?.GetCreatureNode(body) is not { } actor || !Formations.TryGetValue(actor, out var formation)) return;
        formation._attack = attack;
        formation._reactionTime = attack ? .4f : .3f;
    }

    private Figure Create(Fighter fighter)
    {
        var root = new Node2D { Name = "MobFighter_" + fighter };
        _actor.AddChild(root);
        var art = new Node2D();
        root.AddChild(art);
        var name = SpineNames[(int)fighter];
        var alternateSpine = _alternate ? OriginalAssets.Spine(name, alternate: true) : null;
        var spine = alternateSpine ?? OriginalAssets.Spine(name) ??
            throw new InvalidOperationException($"Missing original Spine for {name}");
        art.AddChild(spine);
        // Use the bodies for horizontal targeting. The alternate rigs include wide weapons and ribbons.
        float? skinHoverHalfWidth = alternateSpine == null ? null : fighter switch
        {
            Fighter.Scout => 29,
            Fighter.Guard => 34,
            Fighter.Brute => 38,
            Fighter.Mage => 39,
            _ => ReserveSpacing / 2 - 2
        };

        var hp = new Label
        {
            Name = "ReserveHp", Position = new(-47, -4), Size = new(96, 32),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        hp.AddThemeFontSizeOverride("font_size", 24);
        hp.AddThemeColorOverride("font_color", new Color("fff6de"));
        hp.AddThemeColorOverride("font_outline_color", Colors.Black);
        var nativeHp = _actor.GetNodeOrNull<NCreatureStateDisplay>("%HealthBar")
            ?.GetNodeOrNull<NHealthBar>("%HealthBar")?.GetNodeOrNull<Label>("%HpLabel");
        hp.AddThemeConstantOverride("outline_size", nativeHp?.GetThemeConstant("outline_size") ?? 16);
        root.AddChild(hp);
        float hoverHalfWidth = skinHoverHalfWidth ?? 42;
        var hover = new Control
        {
            Name = "ReserveHover", Position = new(-hoverHalfWidth, -110), Size = new(hoverHalfWidth * 2, 144),
            MouseFilter = Control.MouseFilterEnum.Pass
        };
        hover.MouseEntered += () =>
        {
            var tips = NHoverTipSet.CreateAndShow(hover, new HoverTip(new LocString("powers", LegacyLocalization.FighterTitle(fighter)),
                new LocString("powers", LegacyLocalization.FighterDescription(fighter))));
            if (tips == null) return;
            var hit = hover.GetGlobalRect();
            var size = tips.GetNode<Control>("textHoverTipContainer").Size;
            float width = Mathf.Max(0, hover.GetViewportRect().Size.X - size.X);
            tips.SetAlignmentBelow(new Vector2(Mathf.Clamp(hit.GetCenter().X - size.X / 2, 0, width),
                hit.Position.Y - size.Y - 12));
            tips.SetFollowOwner();
        };
        hover.MouseExited += () => NHoverTipSet.Remove(hover);
        root.AddChild(hover);
        return _figures[fighter] = new(root, art, spine, skinHoverHalfWidth, hp, hover);
    }

    private void Arrange(MobRosterRelic roster)
    {
        if (_corpse != null)
        {
            _corpse.QueueFree();
            _corpse = null;
        }
        var living = roster.Formation();
        foreach (var (fighter, figure) in _figures)
        {
            if (living.Any(member => member.Fighter == fighter)) continue;
            figure.Root.Hide();
            NHoverTipSet.Remove(figure.Hover);
            figure.Slot = -2;
        }
        _nob?.Hide();
        for (int i = 0; i < living.Length; i++)
        {
            var member = living[i];
            if (!_figures.TryGetValue(member.Fighter, out var figure)) figure = Create(member.Fighter);
            bool front = i == 0;
            int previousSlot = figure.Slot;
            figure.Slot = i - 1;
            figure.Root.Visible = member.Hp > 0;
            figure.Spine.Show();
            figure.Hp.Visible = figure.Hover.Visible = !front;
            if (front)
            {
                NHoverTipSet.Remove(figure.Hover);
                if (_front != figure)
                {
                    _reactionTime = 0;
                    _front = figure;
                }
                figure.Root.Position = _origin;
                figure.Art.Position = Vector2.Zero;
                if (_actor.Entity.HasPower<MobRampageForm>())
                {
                    if (_nob == null)
                    {
                        _nob = OriginalAssets.Spine("nob", _alternate) ?? OriginalAssets.Spine("nob") ??
                            throw new InvalidOperationException("Missing original Nob Spine");
                        figure.Art.AddChild(_nob);
                        OriginalAssets.PlayIdle(_nob, "nob");
                    }
                    else if (_nob.GetParent() != figure.Art)
                        _nob.Reparent(figure.Art, false);
                    figure.Spine.Hide();
                    _nob.Show();
                }
            }
            else
            {
                figure.Target = _origin + new Vector2(-ReserveSpacing * i, -30);
                figure.Art.Position = new Vector2(0, 25);
                figure.Hp.Text = member.Hp.ToString();
                if (previousSlot < 0)
                {
                    // The outgoing leader is channeled into the first free orb slot.
                    figure.Root.Position = !_initialized || previousSlot == -1 && living.Length == 5
                        ? _origin + new Vector2(20, -45) : figure.Target;
                    figure.ChannelTime = .5f;
                }
            }
            if (previousSlot == -2 || (previousSlot == -1) != front)
                OriginalAssets.PlayIdle(figure.Spine, SpineNames[(int)member.Fighter]);
        }
        _initialized = true;
    }

    private async Task UpdateMotion(SceneTree tree)
    {
        while (GodotObject.IsInstanceValid(_actor) && _actor.IsInsideTree())
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            if (!GodotObject.IsInstanceValid(_actor) || !_actor.IsInsideTree()) return;

            float delta = (float)_actor.GetProcessDeltaTime();
            foreach (var figure in _figures.Values)
            {
                if (figure.Slot < 0 || !figure.Root.Visible) continue;
                var position = figure.Root.Position.Lerp(figure.Target, Mathf.Min(1, 6 * delta));
                if (Mathf.Abs(position.X - figure.Target.X) < 1) position.X = figure.Target.X;
                if (Mathf.Abs(position.Y - figure.Target.Y) < 1) position.Y = figure.Target.Y;
                figure.Root.Position = position;
                if (!figure.HasHoverBounds) UpdateHoverBounds(figure);
                figure.ChannelTime = Mathf.Max(0, figure.ChannelTime - delta);
                float ratio = figure.ChannelTime / .5f;
                figure.Hp.Modulate = new Color(1, 1, 1, 1 - .99f * ratio * ratio);
            }
            if (_front == null || _reactionTime <= 0) continue;
            _reactionTime = Mathf.Max(0, _reactionTime - delta);
            float t = _attack ? _reactionTime * 2 : _reactionTime / .3f;
            float x = _attack ? 90 * t * t * t * (t * (t * 6 - 15) + 10) : -20 * 2 * t * t;
            if (!_attack && t > .5f) x = -20 * (1 - 2 * (1 - t) * (1 - t));
            _front.Art.Position = new Vector2(x, 0);
        }
    }

    private static void UpdateHoverBounds(Figure figure)
    {
        using var skeleton = new MegaSprite(figure.Spine).GetSkeleton();
        if (skeleton?.GetBounds() is not { Size.X: > 1, Size.Y: > 1 } bounds) return;
        bounds = (figure.Art.Transform * figure.Spine.Transform) * bounds;

        float left = -42, right = 42;
        float padding = figure.SkinHoverHalfWidth.HasValue ? 6 : 0;
        if (figure.SkinHoverHalfWidth is { } edge)
        {
            left = Mathf.Clamp(bounds.Position.X - padding, -edge, -18);
            right = Mathf.Clamp(bounds.End.X + padding, 18, edge);
        }
        float top = Mathf.Min(-48, bounds.Position.Y - padding);
        float bottom = Mathf.Max(34, bounds.End.Y + padding);
        figure.Hover.Position = new Vector2(left, top);
        figure.Hover.Size = new Vector2(right - left, bottom - top);
        figure.HasHoverBounds = true;
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
    private static class ShowCorpse
    {
        private static void Postfix(NCreature __instance)
        {
            if (!Formations.TryGetValue(__instance, out var formation) || formation._corpse != null) return;
            foreach (var figure in formation._figures.Values)
            {
                NHoverTipSet.Remove(figure.Hover);
                figure.Root.Hide();
            }
            var texture = ResourceLoader.Load<Texture2D>("res://gremlin_mob_2/original/char/corpse.res");
            var corpse = new Sprite2D { Name = "GremlinOriginalCorpse", Texture = texture, Centered = false,
                Position = formation._origin - new Vector2(256, 512) };
            __instance.AddChild(corpse);
            formation._corpse = corpse;
        }
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
internal static class AttachMob
{
    private static void Postfix(NCreature __instance)
    {
        if (MobRosterRelic.For(__instance.Entity.Player) is { } roster)
            MobFormation.Attach(__instance, roster);
    }
}
