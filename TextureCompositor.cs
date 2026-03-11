using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TextureCompositor", menuName = "Texture Compositor/Compositor")]
public class TextureCompositor : ScriptableObject
{
    public int width = 512;
    public int height = 512;
    public List<Layer> layers = new List<Layer>();

    private Material blendMaterial;

    private void OnEnable()
    {
        // Инициализация материала смешивания
        Shader shader = Shader.Find("Hidden/TextureCompositor/Blend");
        if (shader != null)
            blendMaterial = new Material(shader);
        else
            Debug.LogError("Blend shader not found! Make sure 'Hidden/TextureCompositor/Blend' exists.");
    }

    public RenderTexture GetAccumulationUpTo(int index)
    {
        if (index <= 0) return null;

        RenderTexture accumulator = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(Texture2D.whiteTexture, accumulator);

        for (int i = 0; i < index; i++)
        {
            var layer = layers[i];
            if (!layer.enabled) continue;

            RenderTexture layerRT = layer.GetRenderTexture(this, i, width, height);
            if (layerRT == null) continue;

            RenderTexture resultRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            blendMaterial.SetTexture("_LayerTex", layerRT);
            blendMaterial.SetFloat("_BlendMode", (int)layer.blendMode);
            Graphics.Blit(accumulator, resultRT, blendMaterial);

            RenderTexture.ReleaseTemporary(accumulator);
            accumulator = resultRT;
            RenderTexture.ReleaseTemporary(layerRT);
        }

        return accumulator;
    }

    public Texture2D Compose()
    {
        if (layers.Count == 0 || blendMaterial == null)
            return null;

        // Начинаем с белого фона (нейтральный для multiply)
        RenderTexture accumulator = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);

        Graphics.Blit(Texture2D.blackTexture, accumulator);

        // Проходим слои в порядке от нижнего к верхнему (индекс 0 - нижний)
        int i = -1;
        foreach (var layer in layers)
        {
            i++;
            if (!layer.enabled)
                continue;

            RenderTexture layerRT = layer.GetRenderTexture(this, i, width, height);

            if (layerRT == null)
                continue;

            // Смешивание
            RenderTexture resultRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            blendMaterial.SetTexture("_Blend", layerRT);
            blendMaterial.SetFloat("_Mode", (int)layer.blendMode);
            blendMaterial.SetFloat("_Opacity", layer.opacity);
            Graphics.Blit(accumulator, resultRT, blendMaterial);

            RenderTexture.ReleaseTemporary(accumulator);
            accumulator = resultRT;
            RenderTexture.ReleaseTemporary(layerRT);
        }

        // Конвертируем результат в Texture2D
        Texture2D final = new Texture2D(width, height, TextureFormat.RGBA32, false);
        RenderTexture.active = accumulator;
        final.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        final.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(accumulator);

        return final;
    }
}