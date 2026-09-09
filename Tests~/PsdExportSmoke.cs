// Opt-in after manual compilation. Requires graphics; never builds or recompiles the project.
// Creates temporary in-memory documents and PSDs under Temp/SpriteEditor only.
var document = ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositor>();
string folder = System.IO.Path.GetFullPath("Temp/SpriteEditor/PsdSmoke-" + Guid.NewGuid().ToString("N"));
System.IO.Directory.CreateDirectory(folder);
int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
try
{
    document.width = 32; document.height = 24;
    var fill = new DCFApixels.SpriteEditor.ColorFillLayer { layerName = "Color", color = new Color(1, 0, 0.25f, 0.6f) };
    fill.transform.scale = new Vector2(0.7f, 0.8f);
    fill.transform.rotation = 20;
    var gradient = new DCFApixels.SpriteEditor.GradientLayer { layerName = "Gradient", gradientType = DCFApixels.SpriteEditor.GradientLayer.GradientType.Horizontal };
    gradient.transform.rotation = 15;
    gradient.transform.scale = new Vector2(0.7f, 1f);
    document.layers = new List<DCFApixels.SpriteEditor.Layer>
    {
        new DCFApixels.SpriteEditor.OutlineLayer { layerName = "Outline", outlineWidth = 3 },
        new DCFApixels.SpriteEditor.GroupLayer { layerName = "Группа 💗", layers = new List<DCFApixels.SpriteEditor.Layer>
        {
            new DCFApixels.SpriteEditor.SDFLayer { layerName = "SDF" }, fill,
            new DCFApixels.SpriteEditor.GroupLayer { layerName = "Nested", layers = new List<DCFApixels.SpriteEditor.Layer>
            {
                gradient, new DCFApixels.SpriteEditor.ColorFillLayer { layerName = "Hidden", enabled = false, color = Color.blue }
            } }
        } }
    };
    string before = EditorJsonUtility.ToJson(document);
    bool dirty = EditorUtility.IsDirty(document);
    RenderTexture active = RenderTexture.active;
    string path = System.IO.Path.Combine(folder, "composition.psd");
    var report = DCFApixels.SpriteEditor.SpriteEditorPsdExporter.Export(document, path);
    Check(report.layerCount == 5 && report.groupCount == 2, "Layer/group counts");
    Check(report.editableFillCount == 3 && report.editableOutlineCount == 1, "Native fills and stroke");
    Check(before == EditorJsonUtility.ToJson(document), "Source serialization unchanged");
    Check(dirty == EditorUtility.IsDirty(document), "Source dirty state unchanged");
    Check(active == RenderTexture.active, "Active render target restored");
    byte[] bytes = System.IO.File.ReadAllBytes(path);
    Check(bytes.Length > 100 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "8BPS", "Real rendered export");
    bool refused = false;
    try { DCFApixels.SpriteEditor.SpriteEditorPsdExporter.Export(document, path); }
    catch (System.IO.IOException) { refused = true; }
    Check(refused, "Overwrite requires opt-in");
    bool canceled = false;
    try
    {
        DCFApixels.SpriteEditor.SpriteEditorPsdExporter.Export(document, path, true,
            (name, fraction) => { if (fraction > 0.2f) throw new OperationCanceledException(); });
    }
    catch (OperationCanceledException) { canceled = true; }
    Check(canceled, "Cancellation propagated");
    Check(Convert.ToBase64String(bytes) == Convert.ToBase64String(System.IO.File.ReadAllBytes(path)), "Canceled export preserves destination");
    Check(System.IO.Directory.GetFiles(folder, "*.tmp").Length == 0, "No temporary siblings after cancellation");
    Check(before == EditorJsonUtility.ToJson(document), "Canceled export preserves source");
    DCFApixels.SpriteEditor.SpriteEditorPsdExporter.Export(document, path, true);
    Check(System.IO.File.ReadAllBytes(path).Length == bytes.Length, "Successful replacement");
    Debug.Log($"PSD Editor smoke: {checks} checks passed. Output: {path}");
}
finally { UnityEngine.Object.DestroyImmediate(document); }
