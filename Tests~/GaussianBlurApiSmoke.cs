// Opt-in after manual compilation. No imports, saves, rendering or Undo.
var type=typeof(DCFApixels.SpriteEditor.SpriteEditorApi);
var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
var setter=type.GetMethod("SetGaussianBlur",flags);
var snapshot=type.GetMethod("GaussianBlurSnapshot",flags);
var jsonType=setter.GetParameters()[1].ParameterType;
object Json(string text)=>jsonType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{text});
var layer=new DCFApixels.SpriteEditor.GaussianBlurLayer();int checks=0;
void Check(bool value,string message){if(!value)throw new System.Exception(message);checks++;}
void Set(string json)=>setter.Invoke(null,new object[]{layer,Json(json)});
void Reject(string json)
{
    bool rejected=false;try{Set(json);}catch(System.Reflection.TargetInvocationException e){rejected=e.InnerException?.GetType().Name=="SpriteEditorApiException";}
    Check(rejected,"Reject "+json);
}
Check(layer.radius==8 && layer.edges==DCFApixels.SpriteEditor.GaussianBlurLayer.EdgeMode.Transparent,"Defaults");
Set("{\"radius\":32.5,\"edges\":\"Repeat\"}");Check(layer.radius==32.5f && layer.edges==DCFApixels.SpriteEditor.GaussianBlurLayer.EdgeMode.Repeat,"Set parameters");
var copy=new DCFApixels.SpriteEditor.GaussianBlurLayer();setter.Invoke(null,new object[]{copy,snapshot.Invoke(null,new object[]{layer})});
Check(UnityEngine.JsonUtility.ToJson(layer)==UnityEngine.JsonUtility.ToJson(copy),"Settings round trip");
Reject("{\"radius\":-1}");Reject("{\"radius\":257}");Reject("{\"edges\":\"Unknown\"}");Reject("{\"unused\":true}");
Check(DCFApixels.SpriteEditor.SpriteEditorApi.Describe().Contains("gaussianBlurDefaults"),"Discovery");
return "Gaussian API checks passed: "+checks;
