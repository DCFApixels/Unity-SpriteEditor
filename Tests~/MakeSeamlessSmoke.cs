// Opt-in only AFTER the user manually compiles. Transient objects; no imports or asset writes.
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var type = typeof(DCFApixels.SpriteEditor.TextureCompositor);
var document = UnityEngine.ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositor>();
document.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
document.width = 33; document.height = 25;
var texture = new UnityEngine.Texture2D(33,25,UnityEngine.TextureFormat.RGBAFloat,false,true);
texture.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
var source = new DCFApixels.SpriteEditor.FileLayerBehaviour {
    sourceTexture = texture, colorRange = DCFApixels.SpriteEditor.LayerColorRange.HDR };
var effect = new DCFApixels.SpriteEditor.MakeSeamlessLayerBehaviour {
    colorRange = DCFApixels.SpriteEditor.LayerColorRange.HDR };
document.layers.Add(effect); document.layers.Add(source);
type.GetMethod("NormalizeModel",flags).Invoke(document,null);
int checks = 0;
void Same(UnityEngine.Color a, UnityEngine.Color b, string message)
{
    for(int c=0;c<4;c++)
    {
        if(float.IsNaN(a[c]) || System.Math.Abs(a[c]-b[c])>.003f) throw new System.Exception(message);
        checks++;
    }
}
UnityEngine.Color[] Render()
{
    var rt = (UnityEngine.RenderTexture)type.GetMethod("RenderLayerPreview",flags).Invoke(document,new object[]{effect.Owner,33});
    var previous = UnityEngine.RenderTexture.active;
    var read = new UnityEngine.Texture2D(33,25,UnityEngine.TextureFormat.RGBAFloat,false,true);
    try
    {
        UnityEngine.RenderTexture.active = rt;
        read.ReadPixels(new UnityEngine.Rect(0,0,33,25),0,0,false);
        return read.GetPixels();
    }
    finally { UnityEngine.RenderTexture.active = previous; UnityEngine.RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(read); }
}
try
{
    var input = new UnityEngine.Color[33*25];
    for(int y=0;y<25;y++) for(int x=0;x<33;x++)
        input[y*33+x] = new UnityEngine.Color(x/8f,y/12f,.3f,.2f + ((x+y)%5)*.2f);
    texture.SetPixels(input); texture.Apply(false,false);
    foreach(var h in new[]{DCFApixels.SpriteEditor.MakeSeamlessLayerBehaviour.HorizontalDirection.Off,
        DCFApixels.SpriteEditor.MakeSeamlessLayerBehaviour.HorizontalDirection.LeftToRight,
        DCFApixels.SpriteEditor.MakeSeamlessLayerBehaviour.HorizontalDirection.RightToLeft})
    foreach(var v in new[]{DCFApixels.SpriteEditor.MakeSeamlessLayerBehaviour.VerticalDirection.Off,
        DCFApixels.SpriteEditor.MakeSeamlessLayerBehaviour.VerticalDirection.BottomToTop,
        DCFApixels.SpriteEditor.MakeSeamlessLayerBehaviour.VerticalDirection.TopToBottom})
    foreach(float width in new[]{.001f,.2f,.5f}) foreach(float falloff in new[]{.25f,1f,4f})
    {
        effect.horizontal=h; effect.vertical=v; effect.blendWidth=width; effect.falloff=falloff;
        var pixels=Render();
        if((int)h!=0) for(int y=0;y<25;y++) Same(pixels[y*33],pixels[y*33+32],"Horizontal seam");
        if((int)v!=0) for(int x=0;x<33;x++) Same(pixels[x],pixels[24*33+x],"Vertical seam");
        if((int)h==0 && (int)v==0) for(int i=0;i<input.Length;i++) Same(input[i],pixels[i],"Bypass");
    }
    var before=Render(); source.enabled=false; var hidden=Render();
    for(int i=0;i<before.Length;i++) Same(before[i],hidden[i],"Hidden input");
    var json=UnityEngine.JsonUtility.ToJson(document);
    var copy=UnityEngine.ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositor>();
    try
    {
        UnityEngine.JsonUtility.FromJsonOverwrite(json,copy);
        var roundtrip=copy.layers[0]?.Behaviour as DCFApixels.SpriteEditor.MakeSeamlessLayerBehaviour;
        if(roundtrip==null || roundtrip.horizontal!=effect.horizontal || roundtrip.falloff!=effect.falloff)
            throw new System.Exception("Seamless settings serialization");
    }
    finally { UnityEngine.Object.DestroyImmediate(copy); }
    return "Make Seamless GPU/serialization checks passed: "+checks;
}
finally { UnityEngine.Object.DestroyImmediate(document); UnityEngine.Object.DestroyImmediate(texture); }
