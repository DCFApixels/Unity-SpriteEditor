// Opt-in after manual compilation. No imports, saves, rendering or Undo.
var type = typeof(DCFApixels.SpriteEditor.SpriteEditorApi);
var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
var setter = type.GetMethod("SetMotionBlur", flags);
var snapshot = type.GetMethod("MotionBlurSnapshot", flags);
var jsonType = setter.GetParameters()[1].ParameterType;
object Json(string text) => jsonType.GetMethod("Parse", new[]{typeof(string)}).Invoke(null, new object[]{text});
var layer = new DCFApixels.SpriteEditor.MotionBlurLayer();
int checks = 0;
void Check(bool value, string message) { if (!value) throw new System.Exception(message); checks++; }
void Set(string json) => setter.Invoke(null, new object[]{layer, Json(json)});
void Reject(string json)
{
    bool rejected = false;
    try { Set(json); }
    catch (System.Reflection.TargetInvocationException e) { rejected = e.InnerException?.GetType().Name == "SpriteEditorApiException"; }
    Check(rejected, "Reject " + json);
}
Check(layer.strength == 1 && layer.distance == 16 && layer.arc == 15 && layer.center == new UnityEngine.Vector2(.5f, .5f) &&
    layer.mode == DCFApixels.SpriteEditor.MotionBlurLayer.BlurMode.Linear, "Defaults");
Set("{\"mode\":\"Circular\",\"distance\":128,\"angle\":45,\"arc\":72,\"center\":[0.2,0.8],\"direction\":\"Backward\",\"edges\":\"Repeat\"}");
Check(layer.arc == 72 && layer.center == new UnityEngine.Vector2(.2f, .8f) &&
    layer.direction == DCFApixels.SpriteEditor.MotionBlurLayer.MotionDirection.Backward, "Set settings");
Set("{\"arc\":90}");
Set("{\"strength\":2.5}");
Check(layer.strength == 2.5f && layer.arc == 90, "Strength partial update");
Check(layer.arc == 90 && layer.distance == 128 && layer.angle == 45 && layer.center.x == .2f, "Partial update preserves other values");
var copy = new DCFApixels.SpriteEditor.MotionBlurLayer();
setter.Invoke(null, new object[]{copy, snapshot.Invoke(null, new object[]{layer})});
Check(UnityEngine.JsonUtility.ToJson(layer) == UnityEngine.JsonUtility.ToJson(copy), "Snapshot round trip");
Reject("{\"distance\":-1}"); Reject("{\"distance\":513}"); Reject("{\"angle\":181}");
Reject("{\"arc\":361}"); Reject("{\"center\":[0,2]}"); Reject("{\"center\":[0]}");
Reject("{\"mode\":\"Zoom\"}"); Reject("{\"direction\":\"Unknown\"}"); Reject("{\"edges\":\"Unknown\"}");
Reject("{\"unused\":true}"); Reject("{\"arc\":null}");
Reject("{\"strength\":-0.1}"); Reject("{\"strength\":4.1}"); Reject("{\"strength\":null}");
string description = DCFApixels.SpriteEditor.SpriteEditorApi.Describe();
Check(description.Contains("motionBlurDefaults") && description.Contains("motionBlurModes") &&
    description.Contains("motionBlurDirections") && description.Contains("motionBlurEdges"), "Discovery");
var document = UnityEngine.ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositor>();
document.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
try
{
    var aliases = new System.Collections.Generic.Dictionary<string, DCFApixels.SpriteEditor.Layer>();
    object Apply(string json) => type.GetMethod("ApplyOperation", flags).Invoke(null, new object[]{document, Json(json), aliases, false});
    var added = (DCFApixels.SpriteEditor.MotionBlurLayer)Apply("{\"op\":\"add\",\"type\":\"motionBlur\",\"as\":\"blur\",\"settings\":{\"motionBlur\":{\"distance\":37}}}");
    Check(added.distance == 37 && document.layers.Count == 1, "Factory and settings routing");
    Apply("{\"op\":\"set\",\"layer\":\"@blur\",\"settings\":{\"motionBlur\":{\"mode\":\"Circular\",\"arc\":20}}}");
    Check(added.mode == DCFApixels.SpriteEditor.MotionBlurLayer.BlurMode.Circular && added.arc == 20, "Set routing");
    string state = UnityEngine.JsonUtility.ToJson(document);
    var reopened = UnityEngine.ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositor>();
    try
    {
        UnityEngine.JsonUtility.FromJsonOverwrite(state, reopened);
        Check(reopened.layers[0] is DCFApixels.SpriteEditor.MotionBlurLayer restored && restored.arc == 20 && restored.distance == 37,
            "Serialized document preserves layer type and parameters");
    }
    finally { UnityEngine.Object.DestroyImmediate(reopened); }
}
finally { UnityEngine.Object.DestroyImmediate(document); }
return "Motion Blur API checks passed: " + checks;
