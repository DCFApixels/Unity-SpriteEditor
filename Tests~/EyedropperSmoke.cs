// Opt-in eval after manual compilation. Temporary GPU textures only; no windows, preferences or asset writes.
var windowType = typeof(DCFApixels.SpriteEditor.TextureCompositorWindow);
const System.Reflection.BindingFlags Hidden = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
var map = windowType.GetMethod("PreviewSamplePixel", Hidden);
var read = windowType.GetMethod("ReadPreviewSampleColor", Hidden);
var hdr = windowType.Assembly.GetType("DCFApixels.SpriteEditor.HdrUtility", true);
var decode = hdr.GetMethod("Decode", Hidden);
int checks = 0;
void Check(bool value, string message)
{
    if (!value) throw new System.Exception(message);
    checks++;
}
var image = new UnityEngine.Rect(100, 50, 200, 100);
UnityEngine.Vector2Int Pixel(float x, float y, bool tiled) =>
    (UnityEngine.Vector2Int)map.Invoke(null, new object[] { new UnityEngine.Vector2(x, y), image, 8, 4, tiled });
Check(Pixel(101, 51, false) == new UnityEngine.Vector2Int(0, 3), "Top-left pixel and flipped vertical axis");
Check(Pixel(299, 149, false) == new UnityEngine.Vector2Int(7, 0), "Bottom-right pixel");
Check(Pixel(300, 50, false) == new UnityEngine.Vector2Int(7, 3), "Canvas boundary clamp");
for (int y = 0; y < 4; y++)
for (int x = 0; x < 8; x++)
for (int row = -2; row <= 2; row++)
for (int column = -2; column <= 2; column++)
    Check(Pixel(100 + (x + .5f) * 25 + column * 200,
        50 + (3 - y + .5f) * 25 + row * 100, true) == new UnityEngine.Vector2Int(x, y),
        "Repeated tiles map to the same source pixel");

var previous = UnityEngine.RenderTexture.active;
bool previousSrgbWrite = UnityEngine.GL.sRGBWrite;
foreach (bool full in new[] { false, true })
{
    var source = UnityEngine.RenderTexture.GetTemporary(2, 2, 0,
        full ? UnityEngine.RenderTextureFormat.ARGBFloat : UnityEngine.RenderTextureFormat.ARGBHalf,
        UnityEngine.RenderTextureReadWrite.Linear);
    var format = full ? UnityEngine.TextureFormat.RGBAFloat : UnityEngine.TextureFormat.RGBAHalf;
    var texture = new UnityEngine.Texture2D(2, 2, format, false, true);
    var pixel = new UnityEngine.Texture2D(1, 1, format, false, true);
    var values = new[] { new UnityEngine.Color(.18f, .4f, .8f, .25f),
        new UnityEngine.Color(4f, 2f, .5f, .75f), UnityEngine.Color.clear,
        new UnityEngine.Color(-.1f, .01f, .5f, 1f) };
    try
    {
        texture.SetPixels(values);
        texture.Apply();
        UnityEngine.GL.sRGBWrite = false;
        UnityEngine.Graphics.Blit(texture, source);
        UnityEngine.RenderTexture.active = previous;
        for (int i = 0; i < values.Length; i++)
        {
            var encoded = (UnityEngine.Color)read.Invoke(null,
                new object[] { source, pixel, new UnityEngine.Vector2Int(i % 2, i / 2) });
            var linear = (UnityEngine.Color)decode.Invoke(null, new object[] { encoded });
            for (int channel = 0; channel < 4; channel++)
                Check(System.Math.Abs(linear[channel] - values[i][channel]) < .003f,
                    "Sample preserves linear RGB, HDR and alpha: " + full + "/" + i + "/" + channel);
            Check(UnityEngine.RenderTexture.active == previous, "Readback restores active render target");
        }
    }
    finally
    {
        UnityEngine.RenderTexture.active = previous;
        UnityEngine.GL.sRGBWrite = previousSrgbWrite;
        UnityEngine.RenderTexture.ReleaseTemporary(source);
        UnityEngine.Object.DestroyImmediate(texture);
        UnityEngine.Object.DestroyImmediate(pixel);
    }
}
return "Eyedropper checks passed: " + checks;
