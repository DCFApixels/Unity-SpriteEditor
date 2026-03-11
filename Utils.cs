using System.Collections.Generic;
using UnityEngine;


public enum BlendMode
{
    Overwrite,
    Multiply
    // Добавьте другие режимы по необходимости
}

[System.Serializable]
public abstract class Layer
{
    public string layerName = "New Layer";
    public bool enabled = true;
    public float opacity = 1;
    public BlendMode blendMode = BlendMode.Overwrite;
    public List<Material> modifiers = new List<Material>();

    public abstract RenderTexture GetRenderTexture(TextureCompositor compositor, int layerIndex, int width, int height);
    public virtual Texture2D GetPreviewTexture(int size)
    {
        return null;
    }
}

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
        if (sourceTexture == null)
            return null;

        // Создаём RenderTexture нужного размера и копируем исходную текстуру с масштабированием
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(sourceTexture, rt);

        // Применяем модификаторы последовательно
        foreach (var modifier in modifiers)
        {
            if (modifier != null)
            {
                RenderTexture temp = RenderTexture.GetTemporary(rt.width, rt.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(rt, temp, modifier);
                RenderTexture.ReleaseTemporary(rt);
                rt = temp;
            }
        }

        return rt;
    }
}


[System.Serializable]
public abstract class GeneratedLayer : Layer
{

}

public static class GradientUtility
{
    private static readonly GradientAlphaKey[] _alpha = new GradientAlphaKey[] { new(1, 0), new(1, 1) };
    public static readonly Gradient WhiteToBlack = Create(new GradientColorKey[] { new(Color.white, 0f), new(Color.black, 1f) });
    public static Gradient Clone(Gradient gradient)
    {
        var result = new Gradient();
        result.SetKeys(gradient.colorKeys, gradient.alphaKeys);
        return result; 
    }
    public static Gradient Create(GradientColorKey[] colorKeys)
    {
        return Create(colorKeys, _alpha);
    }
    public static Gradient Create(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys)
    {
        var result = new Gradient();
        result.SetKeys(colorKeys, alphaKeys);
        return result;
    }
}