var checks = 0;
var type = typeof(DCFApixels.SpriteEditor.DrawingLayer);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var normalize = type.GetMethod("NormalizeSettings", flags);
var build = type.GetMethod("BuildPatternStamps", flags);
var stampsField = type.GetField("patternStamps", flags);
var begin = type.GetMethod("BeginStroke", flags);
var clipped = type.GetField("clipStrokeToInitialShape", flags);
var point = new UnityEngine.Vector2(0.61f, 0.72f);

void Check(bool condition, string message)
{
    if (!condition) throw new System.Exception(message);
    checks++;
}
void Normalize(DCFApixels.SpriteEditor.DrawingLayer layer) => normalize.Invoke(layer, null);
System.Collections.IList Stamps(DCFApixels.SpriteEditor.DrawingLayer layer)
{
    build.Invoke(layer, new object[] { point, 512, 256 });
    return (System.Collections.IList)stampsField.GetValue(layer);
}
UnityEngine.Vector2 Center(object stamp) =>
    (UnityEngine.Vector2)stamp.GetType().GetField("center").GetValue(stamp);

var legacy = UnityEngine.JsonUtility.FromJson<DCFApixels.SpriteEditor.DrawingLayer>(
    "{\"mirrorAcrossVerticalAxis\":true,\"repeatMode\":0}");
Normalize(legacy);
Check(legacy.repeatMode == DCFApixels.SpriteEditor.PaintRepeatMode.Mirror, "Legacy mirror-only migration");
legacy.repeatMode = DCFApixels.SpriteEditor.PaintRepeatMode.None;
Normalize(legacy);
Check(legacy.repeatMode == DCFApixels.SpriteEditor.PaintRepeatMode.None, "Migration must not re-enable Mirror");

var mirror = new DCFApixels.SpriteEditor.DrawingLayer
{
    repeatMode = DCFApixels.SpriteEditor.PaintRepeatMode.Mirror,
    mirrorAcrossVerticalAxis = true,
    mirrorAcrossHorizontalAxis = true,
    patternCenter = new UnityEngine.Vector2(0.4f, 0.6f),
    repeatBoundaryMode = DCFApixels.SpriteEditor.PaintRepeatBoundaryMode.Clip
};
Normalize(mirror);
var mirrored = Stamps(mirror);
Check(mirrored.Count == 4, "Mirror XY produces four stamps");
var expected = new[]
{
    point,
    new UnityEngine.Vector2(0.19f, 0.72f),
    new UnityEngine.Vector2(0.61f, 0.48f),
    new UnityEngine.Vector2(0.19f, 0.48f)
};
for (int i = 0; i < expected.Length; i++)
    Check((Center(mirrored[i]) - expected[i]).sqrMagnitude < 0.00000001f, "Mirror uses movable center");
begin.Invoke(mirror, new object[] { point });
Check(!(bool)clipped.GetValue(mirror), "Hidden Clip setting must not clip Mirror");
var restored = UnityEngine.JsonUtility.FromJson<DCFApixels.SpriteEditor.DrawingLayer>(
    UnityEngine.JsonUtility.ToJson(mirror));
Normalize(restored);
Check(restored.repeatMode == mirror.repeatMode && Stamps(restored).Count == 4, "Mirror survives JSON round-trip");

foreach (DCFApixels.SpriteEditor.PaintRepeatMode mode in System.Enum.GetValues(typeof(DCFApixels.SpriteEditor.PaintRepeatMode)))
{
    if (mode == DCFApixels.SpriteEditor.PaintRepeatMode.Mirror) continue;
    foreach (DCFApixels.SpriteEditor.PaintRepeatElementMode elements in System.Enum.GetValues(typeof(DCFApixels.SpriteEditor.PaintRepeatElementMode)))
    {
        var layer = new DCFApixels.SpriteEditor.DrawingLayer
        {
            repeatMode = mode, repeatCount = 8, repeatSecondaryCount = 3,
            repeatElementMode = elements
        };
        Normalize(layer);
        var baseline = Stamps(layer);
        var centers = new UnityEngine.Vector2[baseline.Count];
        for (int i = 0; i < centers.Length; i++) centers[i] = Center(baseline[i]);
        layer.mirrorAcrossVerticalAxis = true;
        layer.mirrorAcrossHorizontalAxis = true;
        Normalize(layer);
        var actual = Stamps(layer);
        Check(actual.Count == centers.Length, mode + ": hidden mirrors cannot add stamps");
        for (int i = 0; i < centers.Length; i++)
            Check(Center(actual[i]) == centers[i], mode + ": hidden mirrors cannot change stamp positions");
        int expectedCount = mode == DCFApixels.SpriteEditor.PaintRepeatMode.None ? 1 :
            mode == DCFApixels.SpriteEditor.PaintRepeatMode.Grid ? 24 : 8;
        Check(actual.Count == expectedCount, mode + ": repeat count");
        bool underCursor = false;
        foreach (var center in centers) underCursor |= (center - point).sqrMagnitude < 0.00000001f;
        Check(underCursor, mode + ": source stamp stays under cursor");
    }
}
var legacyRadial = UnityEngine.JsonUtility.FromJson<DCFApixels.SpriteEditor.DrawingLayer>(
    "{\"mirrorAcrossVerticalAxis\":true,\"mirrorAcrossHorizontalAxis\":true,\"repeatMode\":4,\"repeatCount\":8}");
Normalize(legacyRadial);
Check(legacyRadial.repeatMode == DCFApixels.SpriteEditor.PaintRepeatMode.Radial && Stamps(legacyRadial).Count == 8,
    "Legacy Radial+Mirror keeps Radial without extra reflections");
var sectorMethod = type.GetMethod("GetRadialRepeatSector", flags);
foreach (float degrees in new[] { 0f, 17.5f, 90f, 359f, 360f })
foreach (int count in new[] { 3, 8 })
foreach (DCFApixels.SpriteEditor.PaintRepeatElementMode elements in System.Enum.GetValues(typeof(DCFApixels.SpriteEditor.PaintRepeatElementMode)))
{
    var radial = new DCFApixels.SpriteEditor.DrawingLayer
    {
        repeatMode = DCFApixels.SpriteEditor.PaintRepeatMode.Radial,
        repeatCount = count, radialStartAngle = degrees, repeatElementMode = elements,
        repeatBoundaryMode = DCFApixels.SpriteEditor.PaintRepeatBoundaryMode.Clip
    };
    Normalize(radial);
    var rotatedStamps = Stamps(radial);
    Check(rotatedStamps.Count == count, "Rotated Radial stamp count");
    bool underCursor = false;
    foreach (var stamp in rotatedStamps)
    {
        var stampCenter = Center(stamp);
        underCursor |= (stampCenter - point).sqrMagnitude < 0.00000001f;
        var delta = UnityEngine.Vector2.Scale(stampCenter - radial.patternCenter, new UnityEngine.Vector2(512f, 256f));
        float clipCenter = (float)stamp.GetType().GetField("clipAngleCenter").GetValue(stamp);
        float halfWidth = (float)stamp.GetType().GetField("clipAngleHalfWidth").GetValue(stamp);
        float angleDelta = UnityEngine.Mathf.Atan2(delta.y, delta.x) - clipCenter;
        float wrappedDelta = UnityEngine.Mathf.Atan2(UnityEngine.Mathf.Sin(angleDelta), UnityEngine.Mathf.Cos(angleDelta));
        Check(UnityEngine.Mathf.Abs(wrappedDelta) <= halfWidth + 0.00001f, "Rotated stamp inside GPU clip wedge");
    }
    Check(underCursor, "Rotated Radial stays under cursor");
    float start = -UnityEngine.Mathf.PI + UnityEngine.Mathf.Repeat(degrees, 360f) * UnityEngine.Mathf.Deg2Rad;
    int[] sectors = new int[3];
    float[] offsets = { 0.15f, 0.85f, 1.15f };
    for (int i = 0; i < offsets.Length; i++)
    {
        float angle = start + offsets[i] * UnityEngine.Mathf.PI * 2f / count;
        var uv = radial.patternCenter + new UnityEngine.Vector2(
            UnityEngine.Mathf.Cos(angle) * 0.1f, UnityEngine.Mathf.Sin(angle) * 0.2f);
        sectors[i] = (int)sectorMethod.Invoke(radial, new object[] { uv, 512, 256 });
    }
    Check(sectors[0] == sectors[1] && sectors[0] != sectors[2], "CPU Clip follows rotated boundaries");
    var copy = UnityEngine.JsonUtility.FromJson<DCFApixels.SpriteEditor.DrawingLayer>(UnityEngine.JsonUtility.ToJson(radial));
    Check(copy.radialStartAngle == degrees, "Start angle survives JSON round-trip");
}
return "Drawing pattern checks passed: " + checks + ". No assets or GPU resources created.";
