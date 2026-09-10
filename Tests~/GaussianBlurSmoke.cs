// Opt-in after manual compilation. Transient objects only; no imports, saves or Undo operations.
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var compositorType = typeof(DCFApixels.SpriteEditor.TextureCompositor);
var document = UnityEngine.ScriptableObject.CreateInstance<DCFApixels.SpriteEditor.TextureCompositor>();
document.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
document.width = 33; document.height = 25;
var texture = new UnityEngine.Texture2D(33,25,UnityEngine.TextureFormat.RGBAFloat,false,true);
texture.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
var source = new DCFApixels.SpriteEditor.FileLayer { sourceTexture = texture, colorRange = DCFApixels.SpriteEditor.LayerColorRange.HDR };
var gaussian = new DCFApixels.SpriteEditor.GaussianBlurLayer { radius = 4, colorRange = DCFApixels.SpriteEditor.LayerColorRange.HDR };
document.layers.Add(gaussian); document.layers.Add(source);
compositorType.GetMethod("NormalizeModel",flags).Invoke(document,null);
int checks = 0;
void Check(bool value,string message) { if(!value) throw new System.Exception(message); checks++; }
void Upload(UnityEngine.Color[] values) { texture.SetPixels(values); texture.Apply(false,false); }
UnityEngine.Color[] Render()
{
    var rt = (UnityEngine.RenderTexture)compositorType.GetMethod("RenderLayerPreview",flags).Invoke(document,new object[]{gaussian,33});
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
void Same(UnityEngine.Color[] a,UnityEngine.Color[] b,float tolerance,string message)
{
    Check(a.Length==b.Length,message+" length");
    for(int i=0;i<a.Length;i++) for(int c=0;c<4;c++) Check(System.Math.Abs(a[i][c]-b[i][c])<=tolerance,message);
}
try
{
    var input = new UnityEngine.Color[33*25];
    // Hidden green must not bleed into the red impulse.
    for(int i=0;i<input.Length;i++) input[i] = new UnityEngine.Color(0,8,0,0);
    input[12*33+16] = new UnityEngine.Color(4,0,0,1);
    Upload(input);
    var pixels = Render();
    double sigma = gaussian.radius/3d, sum=0;
    int extent = UnityEngine.Mathf.CeilToInt(gaussian.radius);
    var kernel = new double[2*extent+1];
    for(int k=-extent;k<=extent;k++) { kernel[k+extent]=System.Math.Exp(-k*k/(2*sigma*sigma)); sum+=kernel[k+extent]; }
    for(int k=0;k<kernel.Length;k++) kernel[k]/=sum;
    for(int y=0;y<25;y++) for(int x=0;x<33;x++)
    {
        int dx=x-16,dy=y-12;
        double alpha=System.Math.Abs(dx)<=extent && System.Math.Abs(dy)<=extent ? kernel[dx+extent]*kernel[dy+extent] : 0;
        var p=pixels[y*33+x];
        Check(System.Math.Abs(p.a-alpha)<.001,"Gaussian alpha matches CPU kernel");
        if(alpha>.0001) { Check(System.Math.Abs(p.r-4)<.02,"HDR red preserved"); Check(System.Math.Abs(p.g)<.001,"Transparent RGB excluded"); }
    }
    source.enabled=false;
    Same(pixels,Render(),.001f,"Hidden input remains usable");
    source.enabled=true;
    gaussian.radius=0;
    pixels=Render();
    Check(pixels[12*33+16].r>3.99f && pixels[12*33+16].a>.999f,"Zero radius identity");
    gaussian.radius=4;
    for(int i=0;i<input.Length;i++) input[i]=new UnityEngine.Color(.25f,.5f,2f,1);
    Upload(input);
    foreach(var edge in new[]{DCFApixels.SpriteEditor.GaussianBlurLayer.EdgeMode.Clamp,
        DCFApixels.SpriteEditor.GaussianBlurLayer.EdgeMode.Repeat,DCFApixels.SpriteEditor.GaussianBlurLayer.EdgeMode.Mirror})
    {
        gaussian.edges=edge;
        Same(input,Render(),.003f,"Constant image under "+edge);
    }
    gaussian.edges=DCFApixels.SpriteEditor.GaussianBlurLayer.EdgeMode.Transparent;
    Check(Render()[0].a<.9f,"Transparent boundary fades");
    System.Array.Clear(input,0,input.Length); input[12*33]=new UnityEngine.Color(1,0,0,1); Upload(input);
    gaussian.edges=DCFApixels.SpriteEditor.GaussianBlurLayer.EdgeMode.Repeat;
    pixels=Render(); Check(pixels[12*33+32].a>.001f,"Repeat crosses seam");
    gaussian.edges=DCFApixels.SpriteEditor.GaussianBlurLayer.EdgeMode.Clamp;
    Check(Render()[12*33+32].a<.0001f,"Clamp does not wrap");
    gaussian.edges=DCFApixels.SpriteEditor.GaussianBlurLayer.EdgeMode.Mirror;
    Check(Render()[12*33+32].a<.0001f,"Mirror does not wrap to opposite edge");
    return "Gaussian GPU checks passed: "+checks;
}
finally { UnityEngine.Object.DestroyImmediate(document); UnityEngine.Object.DestroyImmediate(texture); }
