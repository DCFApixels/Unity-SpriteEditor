// Opt-in after manual compilation. Tests only temporary in-memory layer trees.
var document = UnityEngine.ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositor>();
var operations = typeof(DCFApixels.SpriteEditor.TextureCompositor).Assembly.GetType(
    "DCFApixels.SpriteEditor.LayerSelectionOperations", true);
var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
int checks = 0;
void Check(bool value, string name) { if (!value) throw new System.Exception(name); checks++; }
object Call(string method, params object[] args) => operations.GetMethod(method, flags).Invoke(null, args);
System.Collections.Generic.List<DCFApixels.SpriteEditor.Layer> Layers(params DCFApixels.SpriteEditor.Layer[] values)
    => new System.Collections.Generic.List<DCFApixels.SpriteEditor.Layer>(values);
bool Same(System.Collections.Generic.List<DCFApixels.SpriteEditor.Layer> actual,
    params DCFApixels.SpriteEditor.Layer[] expected)
{
    if (actual.Count != expected.Length) return false;
    for (int i = 0; i < expected.Length; i++) if (!object.ReferenceEquals(actual[i], expected[i])) return false;
    return true;
}
try
{
    var a = new DCFApixels.SpriteEditor.ColorFillLayer { layerName = "A" };
    var b = new DCFApixels.SpriteEditor.ColorFillLayer { layerName = "B" };
    var c = new DCFApixels.SpriteEditor.ColorFillLayer { layerName = "C" };
    var d = new DCFApixels.SpriteEditor.ColorFillLayer { layerName = "D" };
    var e = new DCFApixels.SpriteEditor.ColorFillLayer { layerName = "E" };
    var group = new DCFApixels.SpriteEditor.GroupLayer();
    var nested = new DCFApixels.SpriteEditor.GroupLayer();

    // Every flat selection, including empty/all, at both boundaries and with gaps.
    var original = Layers(a, b, c, d, e);
    for (int mask = 0; mask < 32; mask++)
    foreach (int direction in new[] { -1, 1 })
    {
        document.layers = new System.Collections.Generic.List<DCFApixels.SpriteEditor.Layer>(original);
        var selected = Layers();
        foreach (var item in original)
            if ((mask & (1 << original.IndexOf(item))) != 0) selected.Add(item);
        bool available = (bool)Call("Move", document, selected, direction, false);
        Check(Same(document.layers, a, b, c, d, e), "Availability must not mutate the tree");
        bool moved = (bool)Call("Move", document, selected, direction, true);
        Check(moved == available, "Availability matches execution");
        var selectedAfter = Layers();
        var otherAfter = Layers();
        var otherBefore = Layers();
        foreach (var item in original) if (!selected.Contains(item)) otherBefore.Add(item);
        foreach (var item in document.layers)
            (selected.Contains(item) ? selectedAfter : otherAfter).Add(item);
        Check(Same(selectedAfter, selected.ToArray()), "Selected relative order is stable");
        Check(Same(otherAfter, otherBefore.ToArray()), "Unselected relative order is stable");
        bool blocked = false;
        for (int step = 0; step < original.Count; step++)
        {
            int i = direction < 0 ? step : original.Count - 1 - step;
            var item = original[i];
            if (!selected.Contains(item)) { blocked = false; continue; }
            if (i == (direction < 0 ? 0 : original.Count - 1)) blocked = true;
            int expectedIndex = blocked ? i : i + direction;
            Check(document.layers.IndexOf(item) == expectedIndex, "Move by one slot or stay at boundary");
        }
    }

    nested.layers = Layers(b, c);
    group.layers = Layers(a, nested);
    document.layers = Layers(group, d);
    var chosen = new System.Collections.Generic.HashSet<DCFApixels.SpriteEditor.Layer>(Layers(group, nested, b, d));
    var roots = (System.Collections.Generic.List<DCFApixels.SpriteEditor.Layer>)Call("Collect", document.layers, chosen, true);
    var all = (System.Collections.Generic.List<DCFApixels.SpriteEditor.Layer>)Call("Collect", document.layers, chosen, false);
    Check(Same(roots, group, d), "Selected descendants must not be processed twice");
    Check(Same(all, group, nested, b, d), "Per-layer actions keep explicit nested selection in tree order");
    var expanded = (System.Collections.Generic.List<DCFApixels.SpriteEditor.Layer>)Call("Ungroup", document, all);
    Check(Same(document.layers, a, b, c, d), "Nested ungroup processes deepest groups first");
    Check(expanded.Count == 4 && !expanded.Contains(group) && !expanded.Contains(nested), "Ungroup selects resulting layers once");

    group.layers = Layers(a);
    document.layers = Layers(group, b, c, d);
    var plan = Call("PlanGroupMoves", document, Layers(b, c), true);
    Check(Same(document.layers, group, b, c, d) && Same(group.layers, a), "Group move planning is read-only");
    Call("ApplyGroupMoves", plan, true);
    Check(Same(document.layers, group, d) && Same(group.layers, a, b, c), "Move selected run into preceding group");
    plan = Call("PlanGroupMoves", document, Layers(b, c), false);
    Call("ApplyGroupMoves", plan, false);
    Check(Same(document.layers, group, b, c, d) && Same(group.layers, a), "Move out preserves sibling order");

    group.layers = Layers(a, b);
    nested.layers = Layers(c, d);
    document.layers = Layers(group, nested, e);
    plan = Call("PlanGroupMoves", document, Layers(a, b, c, d), false);
    Call("ApplyGroupMoves", plan, false);
    Check(Same(document.layers, group, a, b, nested, c, d, e), "Move out of multiple groups preserves anchors");
    Check(group.layers.Count == 0 && nested.layers.Count == 0, "All eligible children moved");
    return new { success = true, checks };
}
finally { UnityEngine.Object.DestroyImmediate(document); }
