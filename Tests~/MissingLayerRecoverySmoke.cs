// Opt-in Pipeline eval_file AFTER the user recompiles Unity. No files or user documents changed.
const System.Reflection.BindingFlags Hidden = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var assembly = typeof(DCFApixels.SpriteEditor.TextureCompositor).Assembly;
var parser = assembly.GetType("DCFApixels.SpriteEditor.MissingLayerData", true).GetMethod("Parse", Hidden);
var recovery = assembly.GetType("DCFApixels.SpriteEditor.MissingLayerRecovery", true);
var recordType = recovery.GetNestedType("Record", Hidden);
var copy = recovery.GetMethod("Copy", Hidden);
int checks = 0;
void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
object Record(string yaml)
{
    var record = Activator.CreateInstance(recordType, true);
    recordType.GetField("Data", Hidden).SetValue(record, parser.Invoke(null, new object[] { yaml }));
    return record;
}
var doc = ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositor>();
doc.hideFlags = HideFlags.HideAndDontSave;
try
{
    var blur = new DCFApixels.SpriteEditor.BlurLayer();
    object report = copy.Invoke(null, new object[] { Record("id: 00000000000000000000000000000012\nlayerName: \"Glow \\u2014 saved\"\nenabled: 0\nopacity: 0.7\ntransform:\n  position: {x: 4, y: -10}\n  rotation: 35\ntargetLayerId: target\nstrength: 3.413\nradius: 51.4\noldSetting: 123\n"), blur, doc });
    Check(blur.Id == "00000000000000000000000000000012", "Numeric-looking identity retains leading zeroes");
    Check(blur.layerName == "Glow — saved" && !blur.enabled, "Name, escaped Unicode, boolean");
    Check(Mathf.Abs(blur.opacity - .7f) < .00001f, "Opacity");
    Check(blur.transform.position == new Vector2(4, -10) && blur.transform.rotation == 35, "Nested exact paths");
    Check(blur.transform.scale == Vector2.one, "Absent nested fields keep defaults");
    Check(blur.TargetLayerId == "target", "Private inherited serialized field");
    Check(Mathf.Abs(blur.radius - 51.4f) < .00001f && Mathf.Abs(blur.strength - 3.413f) < .00001f, "Type-specific compatible values");
    Check(((List<string>)report.GetType().GetField("Skipped", Hidden).GetValue(report)).Contains("oldSetting"), "Unknown field reported");

    var bad = new DCFApixels.SpriteEditor.BlurLayer();
    float defaultRadius = bad.radius;
    copy.Invoke(null, new object[] { Record("radius: wrong\nstrength: 1e300\nmode: 700\ntransform:\n  rotation: nope\n  position: {x: 3}\n"), bad, doc });
    Check(bad.radius == defaultRadius && bad.strength == 1, "Invalid and overflowing numeric values keep defaults");
    Check((int)bad.mode == 0 && bad.transform.rotation == 0 && bad.transform.position.x == 3, "Invalid enum and nested mismatch do not block other fields");
    doc.layers.Add(blur);
    bool rejected = false;
    try { copy.Invoke(null, new object[] { Record("id: 00000000000000000000000000000012\n"), new DCFApixels.SpriteEditor.BlurLayer(), doc }); }
    catch (System.Reflection.TargetInvocationException e) when (e.InnerException is InvalidOperationException) { rejected = true; }
    Check(rejected, "Duplicate identity rejected");

    var fill = new DCFApixels.SpriteEditor.ColorFillLayer();
    copy.Invoke(null, new object[] { Record("modifiers:\n- {fileID: 0}\n"), fill, doc });
    Check(fill.modifiers.Count == 1 && fill.modifiers[0] == null, "Unity indentless YAML list");
    var group = new DCFApixels.SpriteEditor.GroupLayer();
    report = copy.Invoke(null, new object[] { Record("layers:\n- {rid: 1000}\n"), group, doc });
    Check(group.layers.Count == 0, "Unresolved managed children are not fabricated");
    Check(((List<string>)report.GetType().GetField("Skipped", Hidden).GetValue(report)).Contains("layers"), "Unresolved children reported");

    var gradient = new DCFApixels.SpriteEditor.GradientLayer();
    copy.Invoke(null, new object[] { Record("gradient:\n  serializedVersion: 2\n  key0: {r: 2, g: 0, b: 0, a: 0.25}\n  key1: {r: 0, g: 0, b: 1, a: 1}\n  ctime0: 0\n  ctime1: 65535\n  atime0: 0\n  atime1: 65535\n  m_NumColorKeys: 2\n  m_NumAlphaKeys: 2\n  m_Mode: 0\n  m_ColorSpace: -1\n"), gradient, doc });
    Check(gradient.gradient.colorKeys[0].color.r == 2 && gradient.gradient.colorKeys[1].color.b == 1, "HDR gradient keys");
    Check(gradient.gradient.alphaKeys[0].alpha == .25f && gradient.gradient.alphaKeys[1].time == 1, "Alpha keys/time normalization");
    foreach (string invalid in new[] { "field: {x: 1", "field: |\n  multiline", "a: 1\na: 2" })
    {
        rejected = false;
        try { Record(invalid); }
        catch (System.Reflection.TargetInvocationException e) when (e.InnerException is FormatException) { rejected = true; }
        Check(rejected, "Unsupported/malformed data is rejected without rewriting it");
    }
    return new { success = true, checks };
}
finally { UnityEngine.Object.DestroyImmediate(doc); }
