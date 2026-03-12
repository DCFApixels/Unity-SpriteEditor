using UnityEngine;

[System.Serializable]
public class FileLayer : Layer
{
    public Texture2D sourceTexture;
    public override Texture2D GetPreviewTexture(int size)
    {
        return sourceTexture; 
    }
    public override RenderTexture GetRenderTexture(TextureCompositor compositor, int layerIndex, int width, int height)
    {
        if (sourceTexture == null) return null;

        // Сначала получаем исходную текстуру (можно сразу применить трансформацию)
        RenderTexture transformed = ApplyTransform(sourceTexture, width, height);

        // Затем применяем модификаторы
        RenderTexture rt = transformed;
        foreach (var modifier in modifiers)
        {
            if (modifier != null)
            {
                RenderTexture temp = RenderTexture.GetTemporary(rt.width, rt.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(rt, temp, modifier);
                if (rt != transformed) RenderTexture.ReleaseTemporary(rt);
                rt = temp;
            }
        }
        // Если модификаторы не применялись, rt == transformed, иначе rt новый
        if (rt != transformed)
        {
            RenderTexture.ReleaseTemporary(transformed);
        }
        return rt;
    }
}
