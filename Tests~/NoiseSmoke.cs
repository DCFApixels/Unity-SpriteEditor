// Opt-in after manual compilation. Transient GPU textures only; no imports, saves or Undo.
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var type = typeof(DCFApixels.SpriteEditor.TextureCompositor);
var document = UnityEngine.ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositor>();
document.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
document.width = 99; document.height = 63;
var layer = new DCFApixels.SpriteEditor.NoiseLayerBehaviour { encoding = DCFApixels.SpriteEditor.NoiseLayerBehaviour.OutputEncoding.LinearData };
document.layers.Add(layer);
type.GetMethod("NormalizeModel", flags).Invoke(document, null);
int checks = 0;
void Check(bool value, string message) { if (!value) throw new System.Exception(message); checks++; }
UnityEngine.Color[] Render(int size = 33)
{
    var previous = UnityEngine.RenderTexture.active;
    var rt = (UnityEngine.RenderTexture)type.GetMethod("RenderLayerPreview", flags).Invoke(document, new object[] { layer.Owner, size });
    var read = new UnityEngine.Texture2D(rt.width, rt.height, UnityEngine.TextureFormat.RGBAFloat, false, true);
    try
    {
        UnityEngine.RenderTexture.active = rt;
        read.ReadPixels(new UnityEngine.Rect(0, 0, rt.width, rt.height), 0, 0, false);
        return read.GetPixels();
    }
    finally { UnityEngine.RenderTexture.active = previous; UnityEngine.RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(read); }
}
float Difference(UnityEngine.Color[] a, UnityEngine.Color[] b)
{
    float sum = 0;
    for (int i = 0; i < a.Length; i++) sum += UnityEngine.Mathf.Abs(a[i].r - b[i].r);
    return sum / a.Length;
}
try
{
    var shader = UnityEngine.Shader.Find("Hidden/TextureCompositor/Noise");
    Check(shader != null && shader.isSupported, "Noise shader supported");
    foreach (var message in UnityEditor.ShaderUtil.GetShaderMessages(shader))
        Check(message.severity.ToString() != "Error", message.message);
    var a = Render();
    Check(Difference(a, Render()) == 0f, "Stable seed");
    var full = Render(99);
    for (int y = 0; y < 21; y++) for (int x = 0; x < 33; x++)
        Check(UnityEngine.Mathf.Abs(a[y * 33 + x].r - full[(y * 3 + 1) * 99 + x * 3 + 1].r) < .003f, "Resolution-independent coordinates");
    layer.seed++;
    Check(Difference(a, Render()) > .01f, "Seed changes pattern");
    layer.seed = 16777216;
    var wideSeed = Render(); layer.seed++;
    Check(Difference(wideSeed, Render()) > .01f, "Adjacent seeds above float integer precision remain distinct");
    layer.seed = 1337;
    layer.inverted = true;
    var inverse = Render();
    for (int i = 0; i < a.Length; i++) Check(UnityEngine.Mathf.Abs(a[i].r + inverse[i].r - 1) < .002f, "Invert");
    layer.inverted = false;
    layer.encoding = DCFApixels.SpriteEditor.NoiseLayerBehaviour.OutputEncoding.ColorValues;
    var color = Render();
    for (int i = 0; i < a.Length; i++)
        Check(UnityEngine.Mathf.Abs(color[i].r - UnityEngine.Mathf.GammaToLinearSpace(a[i].r)) < .003f, "Color encoding");
    layer.encoding = DCFApixels.SpriteEditor.NoiseLayerBehaviour.OutputEncoding.LinearData;
    foreach (DCFApixels.SpriteEditor.NoiseLayerBehaviour.NoiseType algorithm in System.Enum.GetValues(typeof(DCFApixels.SpriteEditor.NoiseLayerBehaviour.NoiseType)))
    foreach (DCFApixels.SpriteEditor.NoiseLayerBehaviour.FractalType fractal in System.Enum.GetValues(typeof(DCFApixels.SpriteEditor.NoiseLayerBehaviour.FractalType)))
    {
        layer.noiseType = algorithm; layer.fractal = fractal;
        foreach (var pixel in Render())
            Check(!float.IsNaN(pixel.r) && !float.IsInfinity(pixel.r) && pixel.r >= 0 && pixel.r <= 1 &&
                UnityEngine.Mathf.Abs(pixel.r - pixel.g) < .001f && UnityEngine.Mathf.Abs(pixel.r - pixel.b) < .001f && pixel.a > .999f,
                "Finite opaque grayscale: " + algorithm + "/" + fractal);
    }
    layer.noiseType = DCFApixels.SpriteEditor.NoiseLayerBehaviour.NoiseType.OpenSimplex2;
    layer.fractal = DCFApixels.SpriteEditor.NoiseLayerBehaviour.FractalType.FBm;
    foreach (DCFApixels.SpriteEditor.NoiseLayerBehaviour.WarpType warp in System.Enum.GetValues(typeof(DCFApixels.SpriteEditor.NoiseLayerBehaviour.WarpType)))
    {
        layer.warp = warp;
        if (warp != DCFApixels.SpriteEditor.NoiseLayerBehaviour.WarpType.None)
            Check(Difference(a, Render()) > .005f, "Warp changes pattern: " + warp);
    }
    return "Noise GPU checks passed: " + checks;
}
finally { UnityEngine.Object.DestroyImmediate(document); }
