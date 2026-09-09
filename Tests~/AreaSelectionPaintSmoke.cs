// Opt-in after manual compilation. Transient textures/documents only, no saves/imports or Undo operations.
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var drawingType = typeof(DCFApixels.SpriteEditor.DrawingLayer);
var settingsType = drawingType.Assembly.GetType("DCFApixels.SpriteEditor.PaintToolSettings", true);
object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, flags).Invoke(target, args);
int checks = 0;
void Check(bool value, string message) { if (!value) throw new System.Exception(message); checks++; }
var mask = new UnityEngine.Texture2D(16, 16, UnityEngine.TextureFormat.RGBA32, false, true);
mask.filterMode = UnityEngine.FilterMode.Point; mask.wrapMode = UnityEngine.TextureWrapMode.Clamp;
var colors = new UnityEngine.Color32[256];
for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) colors[y * 16 + x] = new UnityEngine.Color32((byte)(x < 8 ? 255 : 0), 0, 0, 255);
mask.SetPixels32(colors); mask.Apply(false, false);
var settings = System.Activator.CreateInstance(settingsType, true);
settingsType.GetField("brushSize").SetValue(settings, 16f);
settingsType.GetField("brushHardness").SetValue(settings, 1f);
settingsType.GetField("pencilSize").SetValue(settings, 16);
UnityEngine.Texture2D Stored(DCFApixels.SpriteEditor.DrawingLayer layer) =>
    (UnityEngine.Texture2D)drawingType.GetProperty("StoredTexture", flags).GetValue(layer);
void Point(DCFApixels.SpriteEditor.DrawingLayer layer, object parameters)
{
    var center = new UnityEngine.Vector2(.5f, .5f);
    Call(layer, "BeginStroke", center);
    try { Call(layer, "PaintPoint", center, 16, 16, parameters); }
    finally { Call(layer, "EndStroke"); }
    Call(layer, "SyncSurfaceToTexture");
}
try
{
    foreach (bool pencil in new[] { false, true })
    foreach (bool transformed in new[] { false, true })
    {
        var layer = new DCFApixels.SpriteEditor.DrawingLayer();
        if (transformed) layer.transform.rotation = 180f;
        try
        {
            object parameters = Call(settings, pencil ? "GetPencilParameters" : "GetStrokeParameters", false, UnityEngine.Color.white);
            parameters = Call(parameters, "WithSelectionMask", mask);
            Point(layer, parameters);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                bool outside = transformed ? x < 8 : x >= 8;
                if (outside) Check(Stored(layer).GetPixel(x, y).a < .001f, "Brush/pencil obeys transformed canvas selection");
            }
            Check(Stored(layer).GetPixel(transformed ? 10 : 5, 8).a > .9f, "Selected source pixels are painted");
            object eraser = Call(settings, pencil ? "GetPencilParameters" : "GetStrokeParameters", true, UnityEngine.Color.white);
            eraser = Call(eraser, "WithSelectionMask", mask);
            Point(layer, eraser);
            Check(Stored(layer).GetPixel(transformed ? 10 : 5, 8).a < .001f, "Eraser uses selection coverage");
        }
        finally { Call(layer, "ReleaseTransientResources"); }
    }
    var document = UnityEngine.ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositor>();
    document.hideFlags = UnityEngine.HideFlags.HideAndDontSave; document.width = document.height = 16;
    try
    {
        var fill = new DCFApixels.SpriteEditor.ColorFillLayer { color = new UnityEngine.Color(1,0,0,.5f), enabled = false, opacity = 0f };
        document.layers.Add(fill);
        var alpha = (UnityEngine.Texture2D)Call(document, "RenderAreaSelectionAlphaSource", fill);
        try { Check(System.Math.Abs(alpha.GetPixel(8,8).a - .5f) < .01f, "Alpha selection ignores outer visibility and opacity"); }
        finally { UnityEngine.Object.DestroyImmediate(alpha); }
        fill.enabled = true; fill.opacity = .5f;
        var copy = (UnityEngine.Texture2D)Call(document, "RenderAreaSelectionSource", fill);
        try { Check(System.Math.Abs(copy.GetPixel(8,8).a - .25f) < .01f, "Copy source bakes opacity"); }
        finally { UnityEngine.Object.DestroyImmediate(copy); }
    }
    finally { UnityEngine.Object.DestroyImmediate(document); }
    return "Area selection painting checks passed: " + checks;
}
finally { UnityEngine.Object.DestroyImmediate(mask); }
