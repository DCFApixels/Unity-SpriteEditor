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



    // Параметры трансформации
    public Vector2 pivot = new Vector2(0.5f, 0.5f);
    public Vector2 position = Vector2.zero;
    public Vector2 scale = Vector2.one;
    public float rotation = 0f; // в градусах

    private Material transformMaterial;
    protected Material GetTransformMaterial()
    {
        if (transformMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/TextureCompositor/Transform");
            if (shader != null)
                transformMaterial = new Material(shader);
            else
                Debug.LogError("Transform shader not found!");
        }
        return transformMaterial;
    }
    protected Matrix4x4 GetTransformMatrix()
    {
        // Строим матрицу преобразования UV:
        // 1. Сдвиг к центру (pivot)
        // 2. Масштабирование
        // 3. Поворот
        // 4. Сдвиг обратно + позиция
        // UV -> новые UV
        Vector2 p = pivot;
        float cos = Mathf.Cos(rotation * Mathf.Deg2Rad);
        float sin = Mathf.Sin(rotation * Mathf.Deg2Rad);

        Matrix4x4 mat = Matrix4x4.identity;
        // Сдвиг к центру
        mat.m00 = 1; mat.m03 = -p.x;
        mat.m11 = 1; mat.m13 = -p.y;
        // Масштаб
        Matrix4x4 scaleMat = Matrix4x4.Scale(new Vector3(scale.x, scale.y, 1));
        mat = scaleMat * mat;
        // Поворот
        Matrix4x4 rotMat = Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotation));
        mat = rotMat * mat;
        // Сдвиг обратно + позиция
        mat.m03 += p.x + position.x;
        mat.m13 += p.y + position.y;

        return mat;
    }
    protected bool IsTransformIdentity()
    {
        return position == Vector2.zero && scale == Vector2.one && rotation == 0f;
    }
    protected RenderTexture ApplyTransform(Texture source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        if (IsTransformIdentity())
        {
            Graphics.Blit(source, rt);
        }
        else
        {
            Material mat = GetTransformMaterial();
            if (mat != null)
            {
                mat.SetMatrix("_Transform", GetTransformMatrix());
                Graphics.Blit(source, rt, mat);
            }
            else
            {
                Graphics.Blit(source, rt); // fallback
            }
        }
        return rt;
    }



    public abstract RenderTexture GetRenderTexture(TextureCompositor compositor, int layerIndex, int width, int height);
    public virtual Texture2D GetPreviewTexture(int size)
    {
        return null;
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