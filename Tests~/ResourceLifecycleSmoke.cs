// Opt-in after manual Unity compilation. Creates temporary objects and uses Editor Undo.
// Does not save assets, compile shaders, open windows or render previews.

int checks = 0;
const System.Reflection.BindingFlags Hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
object Call(object target, string method, params object[] args) =>
    target.GetType().GetMethod(method, Hidden).Invoke(target, args);
Texture2D Pixels(DCFApixels.SpriteEditor.DrawingLayerBehaviour layer) =>
    (Texture2D)typeof(DCFApixels.SpriteEditor.DrawingLayerBehaviour).GetField("pixels", Hidden).GetValue(layer);
void Check(bool value, string message)
{
    if (!value) throw new Exception(message);
    checks++;
}
int Begin(string name)
{
    Undo.FlushUndoRecordObjects();
    Undo.IncrementCurrentGroup();
    Undo.SetCurrentGroupName(name);
    return Undo.GetCurrentGroup();
}
void End(int group)
{
    Undo.FlushUndoRecordObjects();
    Undo.CollapseUndoOperations(group);
    Undo.IncrementCurrentGroup();
}
DCFApixels.SpriteEditor.DrawingLayerBehaviour Drawing(Color color)
{
    var layer = new DCFApixels.SpriteEditor.DrawingLayerBehaviour();
    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        { hideFlags = HideFlags.HideAndDontSave };
    texture.SetPixels(new[] { color, color, color, color });
    texture.Apply();
    typeof(DCFApixels.SpriteEditor.DrawingLayerBehaviour).GetField("pixels", Hidden).SetValue(layer, texture);
    return layer;
}

for (int mode = 0; mode < 3; mode++)
{
    var window = ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositorWindow>();
    var document = (DCFApixels.SpriteEditor.TextureCompositor)typeof(DCFApixels.SpriteEditor.TextureCompositorWindow).GetField("compositor", Hidden).GetValue(window);
    try
    {
        Check(!window.hasUnsavedChanges, "A new untouched document has no close warning");
        document.width = 32;
        Call(document, "MarkChanged");
        Call(window, "UpdateUnsavedChangesState");
        Check(!window.hasUnsavedChanges, "Canvas changes without layers do not enable the close warning");
        DCFApixels.SpriteEditor.Layer root = Drawing(Color.red);
        if (mode == 2)
            root = new DCFApixels.SpriteEditor.GroupLayerBehaviour { layers = new List<DCFApixels.SpriteEditor.Layer> { root,
                new DCFApixels.SpriteEditor.GroupLayerBehaviour { layers = new List<DCFApixels.SpriteEditor.Layer> { Drawing(Color.green) } } } };
        document.layers.Add(root);
        Call(document, "MarkChanged");
        Check(window.hasUnsavedChanges, "Document edits enable the close warning");
        string id = root.Id;
        int group = Begin("Lifecycle deletion");
        Call(window, "DeleteLayers", new List<DCFApixels.SpriteEditor.Layer> { root });
        End(group);
        Check(document.layers.Count == 0, "Deletion removes the root");
        Call(window, "UpdateUnsavedChangesState");
        Check(!window.hasUnsavedChanges, "Deleting the last layer removes the close warning");
        Undo.PerformUndo();
        Check(document.layers.Count == 1 && document.layers[0].Id == id, "Undo restores the root identity");
        Call(window, "UpdateUnsavedChangesState");
        Check(window.hasUnsavedChanges, "Undo restoring a layer restores the close warning");
        DCFApixels.SpriteEditor.DrawingLayerBehaviour restored = mode == 2 ? (DCFApixels.SpriteEditor.DrawingLayerBehaviour)((DCFApixels.SpriteEditor.GroupLayerBehaviour)document.layers[0]).layers[0]
            : (DCFApixels.SpriteEditor.DrawingLayerBehaviour)document.layers[0];
        Check(Pixels(restored) != null && Pixels(restored).GetPixel(0, 0).r > 0.99f, "Undo restores temporary pixels");
        if (mode == 2)
        {
            var nested = (DCFApixels.SpriteEditor.DrawingLayerBehaviour)((DCFApixels.SpriteEditor.GroupLayerBehaviour)((DCFApixels.SpriteEditor.GroupLayerBehaviour)document.layers[0]).layers[1]).layers[0];
            Check(Pixels(nested) != null && Pixels(nested).GetPixel(0, 0).g > 0.99f, "Undo restores nested pixels");
        }
        Undo.PerformRedo();
        Check(document.layers.Count == 0, "Redo removes the root again");
        Undo.PerformUndo();
        Check(document.layers.Count == 1, "A second Undo remains valid");
        window.DiscardChanges();
        Check(!window.hasUnsavedChanges, "Explicit discard clears the close warning");
    }
    finally
    {
        Undo.ClearUndo(document);
        window.DiscardChanges();
        UnityEngine.Object.DestroyImmediate(window);
    }
}

var owner = ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositor>();
var external = ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.ShaderFX>();
try
{
    owner.hideFlags = HideFlags.HideAndDontSave;
    var first = new DCFApixels.SpriteEditor.ColorFillLayerBehaviour();
    var second = new DCFApixels.SpriteEditor.ColorFillLayerBehaviour();
    owner.layers.Add(first);
    owner.layers.Add(new DCFApixels.SpriteEditor.GroupLayerBehaviour { layers = new List<DCFApixels.SpriteEditor.Layer> { second } });
    var effect = (DCFApixels.SpriteEditor.ShaderFX)Call(owner, "AddEmbeddedShaderFX", first.Owner);
    second.modifiers.Add(effect);
    second.modifiers.Add(external);
    Shader template = Shader.Find("Hidden/InternalErrorShader");
    Check(template != null, "An existing shader is available for an in-memory clone");
    Shader shader = UnityEngine.Object.Instantiate(template);
    shader.hideFlags = HideFlags.HideAndDontSave;
    var compiled = typeof(DCFApixels.SpriteEditor.ShaderFX).GetField("compiledShader", Hidden);
    compiled.SetValue(effect, shader);
    first.modifiers.Remove(effect);
    Call(owner, "MarkChanged");
    Check(effect != null && shader != null, "Another nested layer keeps a shared FX alive");
    int group = Begin("Lifecycle FX deletion");
    Undo.RecordObject(owner, "Lifecycle FX deletion");
    second.modifiers.Clear();
    Call(owner, "MarkChanged");
    End(group);
    Check(effect == null && shader == null && external != null, "Only the unused owned FX and shader are destroyed");
    Undo.PerformUndo();
    var restoredLayer = ((DCFApixels.SpriteEditor.GroupLayerBehaviour)owner.layers[1]).layers[0];
    var restoredEffect = restoredLayer.modifiers[0] as DCFApixels.SpriteEditor.ShaderFX;
    Check(restoredEffect != null && (Shader)compiled.GetValue(restoredEffect) != null, "Undo restores FX and its shader reference");
    Undo.PerformRedo();
    Check(((DCFApixels.SpriteEditor.GroupLayerBehaviour)owner.layers[1]).layers[0].modifiers.Count == 0, "FX removal supports Redo");
    Undo.PerformUndo();
    restoredEffect = ((DCFApixels.SpriteEditor.GroupLayerBehaviour)owner.layers[1]).layers[0].modifiers[0] as DCFApixels.SpriteEditor.ShaderFX;
    Check(restoredEffect != null && (Shader)compiled.GetValue(restoredEffect) != null, "FX shader survives a second Undo");
}
finally
{
    Undo.ClearUndo(owner);
    UnityEngine.Object.DestroyImmediate(owner);
    UnityEngine.Object.DestroyImmediate(external);
}
return $"Resource lifecycle checks passed: {checks}.";
