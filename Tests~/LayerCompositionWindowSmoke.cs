// Pipeline eval_file: separate unsaved window, no Show(), project assets or existing documents.
var windowType = typeof(DCFApixels.SpriteEditor.TextureCompositorWindow);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var window = ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositorWindow>();
var doc = (DCFApixels.SpriteEditor.TextureCompositor)windowType.GetField("compositor", flags).GetValue(window);
doc.width = doc.height = 8;
int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
var textures = new List<Texture2D>();
try
{
    var child = new DCFApixels.SpriteEditor.Layer(new DCFApixels.SpriteEditor.ColorFillLayerBehaviour { color = Color.red });
    var group = new DCFApixels.SpriteEditor.Layer(new DCFApixels.SpriteEditor.GroupLayerBehaviour()) { layerName = "Group" };
    group.children.Add(child);
    var other = new DCFApixels.SpriteEditor.Layer(new DCFApixels.SpriteEditor.ColorFillLayerBehaviour { color = Color.green }) { layerName = "Other", enabled = false };
    doc.layers.Add(group); doc.layers.Add(other);
    doc.GetType().GetMethod("NormalizeModel", flags).Invoke(doc, null);
    string groupId = group.Id, otherId = other.Id;
    windowType.GetMethod("ConvertLayersToDrawing", flags).Invoke(window, new object[] { new List<DCFApixels.SpriteEditor.Layer>{ group, other }, true, true });
    Check(ReferenceEquals(doc.layers[0], group) && ReferenceEquals(doc.layers[1], other), "Conversion retains both wrapper instances");
    Check(group.Behaviour is DCFApixels.SpriteEditor.DrawingLayerBehaviour && other.Behaviour is DCFApixels.SpriteEditor.DrawingLayerBehaviour, "Group and ordinary source become Drawing behaviours");
    Check(group.children == null && group.layerName == "Group" && !other.enabled, "Baked group drops children but keeps name/visibility");
    foreach (var layer in doc.layers) textures.Add(layer.GetPreviewTexture(8));
    Check(textures[0].GetPixel(4,4).r > .99f && textures[0].GetPixel(4,4).a > .99f, "Converted group keeps its pixels");
    Undo.PerformUndo();
    Check(doc.layers[0].Id == groupId && doc.layers[0].Behaviour is DCFApixels.SpriteEditor.GroupLayerBehaviour && doc.layers[0].children.Count == 1, "One Undo restores group and descendants");
    Check(doc.layers[1].Id == otherId && doc.layers[1].Behaviour is DCFApixels.SpriteEditor.ColorFillLayerBehaviour, "One Undo restores the second source too");
    Undo.PerformRedo();
    foreach (var layer in doc.layers) textures.Add(layer.GetPreviewTexture(8));
    Check(doc.layers[0].Behaviour is DCFApixels.SpriteEditor.DrawingLayerBehaviour && doc.layers[1].Behaviour is DCFApixels.SpriteEditor.DrawingLayerBehaviour, "One Redo restores both Drawing behaviours");
    Check(doc.layers[0].GetPreviewTexture(8) != null && doc.layers[0].GetPreviewTexture(8).GetPixel(4,4).r > .99f, "Redo restores native pixels");
    return new { success=true, checks };
}
finally
{
    foreach (var texture in textures) if (texture != null) Undo.ClearUndo(texture);
    if (doc != null) Undo.ClearUndo(doc);
    typeof(EditorWindow).GetProperty("hasUnsavedChanges").SetValue(window, false);
    UnityEngine.Object.DestroyImmediate(window);
}
