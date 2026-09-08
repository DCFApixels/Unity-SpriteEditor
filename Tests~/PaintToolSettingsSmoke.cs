var windowType = typeof(DCFApixels.SpriteEditor.TextureCompositorWindow);
var type = windowType.Assembly.GetType("DCFApixels.SpriteEditor.PaintToolSettings", true);
var settings = System.Activator.CreateInstance(type, true);
int checks = 0;
void Check(bool value, string message)
{
    if (!value) throw new System.Exception(message);
    checks++;
}
T Read<T>(object source, string name) => (T)type.GetField(name).GetValue(source);
var first = new DCFApixels.SpriteEditor.DrawingLayer();
var second = new DCFApixels.SpriteEditor.DrawingLayer();
first.brushSize = 9;
second.brushSize = 91;
first.repeatCount = 3;
second.repeatCount = 8;
string beforeFirst = UnityEngine.JsonUtility.ToJson(first);
string beforeSecond = UnityEngine.JsonUtility.ToJson(second);
Check(Read<float>(settings, "brushSize") == 32f, "Shared brush starts with its own default size");
Check(Read<bool>(settings, "fillContiguous"), "Shared fill defaults to contiguous");
Check(Read<DCFApixels.SpriteEditor.FillSampleMode>(settings, "fillSampleMode") ==
    DCFApixels.SpriteEditor.FillSampleMode.CurrentLayer, "Shared fill defaults to current-layer sampling");
type.GetField("brushSize").SetValue(settings, 47f);
type.GetField("fillTolerance").SetValue(settings, 123);
type.GetField("brushColor").SetValue(settings, UnityEngine.Color.red);
type.GetMethod("SwapBrushColors", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
    .Invoke(settings, null);
Check(Read<UnityEngine.Color>(settings, "secondaryBrushColor") == UnityEngine.Color.red, "Color swap uses shared settings");
var restored = UnityEngine.JsonUtility.FromJson(UnityEngine.JsonUtility.ToJson(settings), type);
Check(Read<float>(restored, "brushSize") == 47f && Read<int>(restored, "fillTolerance") == 123,
    "Shared brush/fill settings survive preference serialization");
Check(UnityEngine.JsonUtility.ToJson(first) == beforeFirst && UnityEngine.JsonUtility.ToJson(second) == beforeSecond,
    "Changing shared tools does not overwrite either layer's legacy or local settings");
Check(type.GetField("repeatMode") == null && type.GetField("transform") == null,
    "Shared settings do not own layer repetition or transforms");
var field = windowType.GetField("paintSettings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
Check(field.FieldType == type && System.Attribute.IsDefined(field, typeof(UnityEngine.SerializeField)),
    "The window owns serialized shared settings");
return "Paint tool settings checks passed: " + checks + ". No assets, windows or GPU resources created.";
