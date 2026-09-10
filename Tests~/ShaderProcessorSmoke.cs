// Opt-in only after the user has compiled Unity. Transient objects; no imports or asset saves.
using System;
using UnityEngine;
using DCFApixels.SpriteEditor;

var document = ScriptableObject.CreateInstance<TextureCompositor>();
document.hideFlags = HideFlags.HideAndDontSave;
document.width = document.height = 8;
var texture = new Texture2D(8, 8, TextureFormat.RGBAFloat, false, true) { hideFlags = HideFlags.HideAndDontSave };
var source = new FileLayer { sourceTexture = texture, colorRange = LayerColorRange.HDR, blendRange = LayerBlendRange.HDR };
var processor = new ShaderProcessorLayer();
int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
void SetPixels(Color value)
{
    var pixels = new Color[64];
    Array.Fill(pixels, value); texture.SetPixels(pixels); texture.Apply(false, false);
}
Color Render()
{
    var output = document.Compose();
    try { return output.GetPixel(4, 4); }
    finally { UnityEngine.Object.DestroyImmediate(output); }
}
void Near(Color actual, Color expected, string message)
{
    for (int channel = 0; channel < 4; channel++) Check(Mathf.Abs(actual[channel] - expected[channel]) < .006f, message);
}
try
{
    SetPixels(new Color(4, 0, 0, .25f));
    document.layers.Add(processor); document.layers.Add(source);
    Near(Render(), new Color(4, 0, 0, .25f), "Identity preserves HDR and partial alpha");
    processor.opacity = .5f;
    Near(Render(), new Color(4, 0, 0, .25f), "Identity at partial opacity does not accumulate alpha");
    SetPixels(new Color(1, 0, 0, .25f));
    processor.swizzle[0] = SwizzleChannel.Zero;
    processor.swizzle[2] = SwizzleChannel.R;
    processor.swizzle[3] = SwizzleChannel.One;
    Near(Render(), new Color(.2f, 0, .8f, .625f), "Before/after opacity uses premultiplied interpolation");
    processor.opacity = 1;
    Near(Render(), Color.blue, "Full opacity replaces RGB and alpha");
    processor.enabled = false;
    Near(Render(), new Color(1, 0, 0, .25f), "Hidden processor bypasses processing");
    processor.enabled = true;
    processor.opacity = 0;
    Near(Render(), new Color(1, 0, 0, .25f), "Zero opacity bypasses processing");
    processor.opacity = 1;
    processor.swizzle[3] = SwizzleChannel.A;

    var group = new GroupLayer(); group.layers.Add(processor);
    document.layers[0] = group;
    Near(Render(), new Color(0, 0, 1, .25f), "Pass Through processor sees external backdrop");
    group.compositing = GroupCompositing.Isolated;
    Near(Render(), new Color(1, 0, 0, .25f), "Isolated processor does not touch external backdrop");
    document.layers.Remove(source); group.layers.Add(source);
    Near(Render(), new Color(0, 0, 1, .25f), "Isolated processor sees lower children");

    document.layers.Clear(); document.layers.Add(processor); document.layers.Add(source);
    var second = new ShaderProcessorLayer();
    second.swizzle[0] = SwizzleChannel.B; second.swizzle[2] = SwizzleChannel.Zero;
    document.layers.Insert(0, second);
    Near(Render(), new Color(1, 0, 0, .25f), "Processor chain follows stack order");
    string serialized = JsonUtility.ToJson(document);
    var copy = ScriptableObject.CreateInstance<TextureCompositor>();
    try
    {
        JsonUtility.FromJsonOverwrite(serialized, copy);
        Check(copy.layers[0] is ShaderProcessorLayer && copy.layers[1] is ShaderProcessorLayer, "Processor type survives document serialization");
    }
    finally { UnityEngine.Object.DestroyImmediate(copy); }
    return "Shader Processor checks passed: " + checks;
}
finally
{
    UnityEngine.Object.DestroyImmediate(document);
    UnityEngine.Object.DestroyImmediate(texture);
}
