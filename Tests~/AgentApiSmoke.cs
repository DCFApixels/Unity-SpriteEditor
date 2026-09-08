var checks = 0;
var fixture = "Assets/SpriteEditorApiSmoke_" + System.Guid.NewGuid().ToString("N");
var projectRoot = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
var temp = System.IO.Path.Combine(projectRoot, "Temp/SpriteEditor");
System.IO.Directory.CreateDirectory(temp);
var sourcePng = System.IO.Path.Combine(temp, System.Guid.NewGuid().ToString("N") + ".png");
var image = new UnityEngine.Texture2D(32, 16, UnityEngine.TextureFormat.RGBA32, false);
try
{
    var colors = new UnityEngine.Color32[32 * 16];
    for (int i = 0; i < colors.Length; i++) colors[i] = new UnityEngine.Color32(0, 0, 255, 255);
    image.SetPixels32(colors);
    image.Apply();
    System.IO.File.WriteAllBytes(sourcePng, UnityEngine.ImageConversion.EncodeToPNG(image));
}
finally { UnityEngine.Object.DestroyImmediate(image); }

Newtonsoft.Json.Linq.JObject Json(string value) => Newtonsoft.Json.Linq.JObject.Parse(value);
void Check(bool condition, string message)
{
    if (!condition) throw new System.Exception(message + " | Fixtures: " + fixture);
    checks++;
}
Newtonsoft.Json.Linq.JObject Ok(string response)
{
    var result = Json(response);
    Check((bool)result["success"], result.ToString());
    return result;
}
Newtonsoft.Json.Linq.JObject Inspect() => Ok(DCFApixels.SpriteEditor.SpriteEditorApi.Inspect(fixture + "/Icon.asset"));
Newtonsoft.Json.Linq.JObject Batch(string operations)
{
    var request = Json("{\"apiVersion\":1,\"save\":false,\"operations\":" + operations + "}");
    request["assetPath"] = fixture + "/Icon.asset";
    request["expectedRevision"] = Inspect()["document"]["revision"];
    return request;
}
void Reject(Newtonsoft.Json.Linq.JObject request, string code)
{
    var result = Json(DCFApixels.SpriteEditor.SpriteEditorApi.ExecuteJson(request.ToString()));
    Check(!(bool)result["success"] && (string)result["errorCode"] == code, "Expected " + code + ": " + result);
}
string LayerId(Newtonsoft.Json.Linq.JObject document, string name)
{
    foreach (var layer in document["layers"])
        if ((string)layer["settings"]["name"] == name) return (string)layer["id"];
    throw new System.Exception("Missing layer: " + name);
}
UnityEngine.Color Pixel(string suffix, int x, int y)
{
    var result = Ok(DCFApixels.SpriteEditor.SpriteEditorApi.Render(fixture + "/Icon.asset",
        "Temp/SpriteEditor/" + System.Guid.NewGuid().ToString("N") + "-" + suffix + ".png", 64));
    var texture = new UnityEngine.Texture2D(2, 2);
    try
    {
        Check(UnityEngine.ImageConversion.LoadImage(texture, System.IO.File.ReadAllBytes((string)result["outputPath"])), "Preview decodes");
        return texture.GetPixel(x, y);
    }
    finally { UnityEngine.Object.DestroyImmediate(texture); }
}

Ok(DCFApixels.SpriteEditor.SpriteEditorApi.ImportImage(sourcePng, fixture + "/source.png"));
var create = Json(@"{
  'apiVersion':1, 'create':true, 'width':64, 'height':64,
  'operations':[
    {'op':'add','type':'group','as':'art','settings':{'name':'Art'}},
    {'op':'add','type':'file','as':'image','parent':'@art','settings':{'name':'Image'}},
    {'op':'add','type':'drawing','as':'ink','settings':{'name':'Ink'}},
    {'op':'stroke','layer':'@ink','brush':{'size':5,'hardness':1,'color':[1,0,0,1]},'points':[[8,8],[20,8]]},
    {'op':'add','type':'outline','as':'edge','settings':{'name':'Edge','enabled':false}},
    {'op':'target','layer':'@edge','target':'@art'}
  ]
}".Replace('\'', '"'));
create["assetPath"] = fixture + "/Icon.asset";
create["operations"][1]["settings"]["source"] = fixture + "/source.png";
create["dryRun"] = true;
Ok(DCFApixels.SpriteEditor.SpriteEditorApi.ExecuteJson(create.ToString()));
Check(!System.IO.File.Exists(System.IO.Path.Combine(projectRoot, fixture, "Icon.asset")), "Dry run does not create a document");
create["dryRun"] = false;
var created = Ok(DCFApixels.SpriteEditor.SpriteEditorApi.ExecuteJson(create.ToString()));
var doc = (Newtonsoft.Json.Linq.JObject)created["document"];
Check((bool)doc["hasOutputTexture"] && (bool)doc["hasOutputSprite"], "Output texture and sprite exist");
Check(((Newtonsoft.Json.Linq.JArray)doc["layers"]).Count == 4, "Four layers including a group child");
var inkId = LayerId(doc, "Ink");
var imageId = LayerId(doc, "Image");
var artId = LayerId(doc, "Art");
foreach (var layer in doc["layers"])
    if ((string)layer["id"] == imageId)
        Check(System.Math.Abs((float)layer["transform"]["scale"][1] - 0.5f) < 0.0001f, "Initial File assignment fits source aspect");
var pixel = Pixel("initial", 12, 56);
Check(pixel.r > 0.9f && pixel.a > 0.9f, "Top-left canvas stroke paints expected pixel");
Reject(create, "already_exists");

var invalid = Batch("[{\"op\":\"set\",\"layer\":\"" + inkId + "\",\"settings\":{\"opacity\":0.2}}, {\"op\":\"set\",\"layer\":\"missing\",\"settings\":{\"opacity\":0.1}}]");
var beforeInvalid = (string)Inspect()["document"]["revision"];
Reject(invalid, "layer_not_found");
Check((string)Inspect()["document"]["revision"] == beforeInvalid, "Failed preflight leaves model unchanged");
var typo = Batch("[{\"op\":\"set\",\"layer\":\"" + inkId + "\",\"settings\":{\"opactiy\":0.2}}]");
Reject(typo, "invalid_request");
var duplicateAlias = Batch("[{\"op\":\"add\",\"type\":\"color\",\"as\":\"same\"},{\"op\":\"add\",\"type\":\"color\",\"as\":\"same\"}]");
Reject(duplicateAlias, "invalid_request");
var cyclic = Batch("[{\"op\":\"move\",\"layer\":\"" + artId + "\",\"parent\":\"" + artId + "\"}]");
Reject(cyclic, "invalid_request");
var effectCycle = Batch("[{\"op\":\"add\",\"type\":\"sdf\",\"as\":\"a\"},{\"op\":\"add\",\"type\":\"sdf\",\"as\":\"b\"},{\"op\":\"target\",\"layer\":\"@a\",\"target\":\"@b\"},{\"op\":\"target\",\"layer\":\"@b\",\"target\":\"@a\"}]");
Reject(effectCycle, "invalid_target");

var edit = Batch("[{\"op\":\"set\",\"layer\":\"" + inkId + "\",\"settings\":{\"opacity\":0.5}}]");
Ok(DCFApixels.SpriteEditor.SpriteEditorApi.ExecuteJson(edit.ToString()));
Reject(edit, "revision_conflict");
UnityEditor.Undo.PerformUndo();
Check(Pixel("undo-opacity", 12, 56).a > 0.9f, "Undo restores opacity");
UnityEditor.Undo.PerformRedo();
Check(System.Math.Abs(Pixel("redo-opacity", 12, 56).a - 0.5f) < 0.03f, "Redo restores opacity edit");

var paint = Batch("[{\"op\":\"stroke\",\"layer\":\"" + inkId + "\",\"brush\":{\"color\":[0,1,0,1],\"size\":5,\"hardness\":1},\"points\":[[40,8],[52,8]]}]");
Ok(DCFApixels.SpriteEditor.SpriteEditorApi.ExecuteJson(paint.ToString()));
Check(Pixel("paint", 44, 56).g > 0.9f, "API brush paints a second stroke");
UnityEditor.Undo.PerformUndo();
Check(Pixel("undo-paint", 44, 56).a < 0.01f, "Undo restores pixels without a Sprite Editor window");
UnityEditor.Undo.PerformRedo();
Check(Pixel("redo-paint", 44, 56).g > 0.9f, "Redo restores drawing pixels");

var transform = Batch("[{\"op\":\"transform\",\"layer\":\"" + imageId + "\",\"transform\":{\"position\":[10,-4],\"rotation\":30}}]");
Ok(DCFApixels.SpriteEditor.SpriteEditorApi.ExecuteJson(transform.ToString()));
var save = Batch("[]");
save["save"] = true;
Ok(DCFApixels.SpriteEditor.SpriteEditorApi.ExecuteJson(save.ToString()));
var reloaded = Inspect();
Check((bool)reloaded["document"]["hasOutputTexture"] && (bool)reloaded["document"]["hasOutputSprite"], "Rebaked subassets remain available");
return new { success = true, checks, fixture, sourcePng };
