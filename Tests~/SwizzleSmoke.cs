// Opt-in after manual compilation. Uses temporary documents/textures, no persistent assets or preferences.
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var document = UnityEngine.ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositor>();
int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new System.Exception(message); checks++; }
bool Near(float a, float b) => UnityEngine.Mathf.Abs(a - b) < .012f;
UnityEngine.Color Pixel()
{
    var texture = document.Compose();
    try { return texture.GetPixel(2, 2); }
    finally { UnityEngine.Object.DestroyImmediate(texture); }
}
DCFApixels.SpriteEditor.LayerSwizzle Route(int output, int source)
{
    var result = new DCFApixels.SpriteEditor.LayerSwizzle();
    result[output] = (DCFApixels.SpriteEditor.SwizzleChannel)source;
    return result;
}
try
{
    document.width = document.height = 8;
    var layer = new DCFApixels.SpriteEditor.ColorFillLayer
    {
        color = new UnityEngine.Color(.3f, .6f, .8f, .7f),
        colorRange = DCFApixels.SpriteEditor.LayerColorRange.HDR,
        blendRange = DCFApixels.SpriteEditor.LayerBlendRange.HDR
    };
    document.layers.Add(layer);
    Check(layer.swizzle.IsIdentity, "New layers default to identity");
    Check(UnityEngine.JsonUtility.FromJson<DCFApixels.SpriteEditor.ColorFillLayer>("{}").swizzle.IsIdentity,
        "Missing serialized swizzle defaults to identity");
    UnityEngine.Color before = Pixel();
    for (int output = 0; output < 4; output++)
    for (int source = 0; source < 10; source++)
    {
        layer.swizzle = Route(output, source);
        var clone = UnityEngine.JsonUtility.FromJson<DCFApixels.SpriteEditor.ColorFillLayer>(UnityEngine.JsonUtility.ToJson(layer));
        Check(clone.swizzle[output] == (DCFApixels.SpriteEditor.SwizzleChannel)source, "Channel serialization round-trip");
        var expected = before;
        expected[output] = source == 8 ? 0f : source == 9 ? 1f : source >= 4 ? 1f - before[source - 4] : before[source];
        var actual = Pixel();
        if (expected.a == 0f) Check(Near(actual.a, 0f), "Zero alpha is transparent");
        else for (int c = 0; c < 4; c++) Check(Near(actual[c], expected[c]), "Swizzle output channel " + output + " source " + source);
    }
    layer.color = new UnityEngine.Color(2f, .5f, .25f, 1f);
    layer.swizzle = Route(0, 4);
    Check(Pixel().r < -3f, "HDR inverse preserves signed extended RGB");
    layer.colorRange = DCFApixels.SpriteEditor.LayerColorRange.Standard;
    Check(Near(Pixel().r, 0f), "Standard clamps after swizzle");
    layer.swizzle = default;
    layer.color = new UnityEngine.Color(.8f, .4f, .2f, 1f);
    layer.blendMode = DCFApixels.SpriteEditor.BlendMode.Multiply;
    var group = new DCFApixels.SpriteEditor.GroupLayer();
    group.layers.Add(layer);
    var backdrop = new DCFApixels.SpriteEditor.ColorFillLayer { color = new UnityEngine.Color(.5f, .5f, .5f, 1f) };
    document.layers.Clear(); document.layers.Add(group); document.layers.Add(backdrop);
    var pass = Pixel();
    group.swizzle = Route(0, 2);
    var automatic = Pixel();
    group.compositing = DCFApixels.SpriteEditor.GroupCompositing.Isolated;
    var isolated = Pixel();
    for (int c = 0; c < 4; c++) Check(Near(automatic[c], isolated[c]), "Swizzle forces equivalent isolation");
    group.compositing = DCFApixels.SpriteEditor.GroupCompositing.PassThrough;
    group.swizzle = default;
    var restored = Pixel();
    for (int c = 0; c < 4; c++) Check(Near(pass[c], restored[c]), "Identity restores Pass Through");
    document.layers.Remove(backdrop);
    layer.blendMode = DCFApixels.SpriteEditor.BlendMode.Normal;
    layer.color = UnityEngine.Color.red;
    group.swizzle = Route(3, 1);
    Check(Near(Pixel().a, 0f), "Group alpha is remapped");
    var coverage = (UnityEngine.RenderTexture)typeof(DCFApixels.SpriteEditor.TextureCompositor)
        .GetMethod("RenderGroupAlpha", flags).Invoke(document,
            new object[] { group, 8, 8, 1f, new System.Collections.Generic.HashSet<DCFApixels.SpriteEditor.Layer>() });
    var readback = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBAFloat, false, true);
    var previous = UnityEngine.RenderTexture.active;
    try
    {
        UnityEngine.RenderTexture.active = coverage;
        readback.ReadPixels(new UnityEngine.Rect(0, 0, 8, 8), 0, 0, false);
        Check(Near(readback.GetPixel(2, 2).a, 0f), "SDF/Outline group target uses swizzled alpha");
    }
    finally
    {
        UnityEngine.RenderTexture.active = previous;
        UnityEngine.RenderTexture.ReleaseTemporary(coverage);
        UnityEngine.Object.DestroyImmediate(readback);
    }
    var swizzled = Route(0, 2); swizzled[2] = DCFApixels.SpriteEditor.SwizzleChannel.R;
    layer.swizzle = swizzled;
    var rasterize = typeof(DCFApixels.SpriteEditor.TextureCompositor).GetMethod("RasterizeLayer", flags);
    var raw = (UnityEngine.Texture2D)rasterize.Invoke(document, new object[] { layer, true });
    try { Check(raw.GetPixel(2, 2).r > .98f && raw.GetPixel(2, 2).b < .01f, "Conversion keeps swizzle unbaked for non-group layers"); }
    finally { UnityEngine.Object.DestroyImmediate(raw); }
    var description = DCFApixels.SpriteEditor.SpriteEditorApi.Describe();
    Check(description.Contains("swizzleChannels") && description.Contains("1-A"), "Agent discovery includes swizzle choices");
    return "Swizzle checks passed: " + checks;
}
finally { UnityEngine.Object.DestroyImmediate(document); }
