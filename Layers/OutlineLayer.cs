using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OutlineLayer : Layer
{
    public int targetLayerIndex = -1; // -1 означает "предыдущий слой"
    public bool useAccumulation = false;
    public Color outlineColor = Color.white;
    public float outlineWidth = 10f;
    public float outlineSoftness = 0.5f;
    public enum OutlinePosition { Inside, Outside, Center }
    public OutlinePosition outlinePosition = OutlinePosition.Outside;

    private Texture2D cachedSDF;
    private Texture2D lastInputTexture;
    private int lastInputWidth, lastInputHeight;

    public override RenderTexture GetRenderTexture(TextureCompositor compositor, int layerIndex, int width, int height)
    {
        // Определяем индекс целевого слоя
        int targetIdx = targetLayerIndex;
        if (targetIdx < 0 || targetIdx >= compositor.layers.Count)
        {
            // Если индекс некорректен, используем предыдущий слой (если есть)
            if (layerIndex > 0) targetIdx = layerIndex - 1;
            else return null; // Нет слоя для обработки
        }

        // Получаем входную текстуру
        RenderTexture inputRT = null;
        if (useAccumulation)
        {
            inputRT = compositor.GetAccumulationUpTo(targetIdx);
        }
        else
        {
            Layer targetLayer = compositor.layers[targetIdx];
            if (targetLayer != null && targetLayer.enabled)
            {
                inputRT = targetLayer.GetRenderTexture(compositor, targetIdx, width, height);
            }
        }

        if (inputRT == null)
            return null;

        // Конвертируем входную RenderTexture в Texture2D для CPU-обработки
        Texture2D inputTex = ConvertRenderTextureToTexture2D(inputRT);
        RenderTexture.ReleaseTemporary(inputRT);

        // Генерируем SDF, если необходимо
        Texture2D sdfTex = GenerateSDF(inputTex, width, height);

        // Генерируем обводку на основе SDF
        Texture2D outlineTex = GenerateOutlineFromSDF(sdfTex, width, height);

        // Создаём RenderTexture и копируем результат
        RenderTexture result = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(outlineTex, result);

        // Очистка
        Object.DestroyImmediate(inputTex);
        Object.DestroyImmediate(sdfTex);
        Object.DestroyImmediate(outlineTex);

        // Применяем модификаторы
        foreach (var modifier in modifiers)
        {
            if (modifier != null)
            {
                RenderTexture temp = RenderTexture.GetTemporary(result.width, result.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(result, temp, modifier);
                RenderTexture.ReleaseTemporary(result);
                result = temp;
            }
        }

        return result;
    }

    private Texture2D ConvertRenderTextureToTexture2D(RenderTexture rt)
    {
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        return tex;
    }

    private Texture2D GenerateSDF(Texture2D input, int width, int height)
    {
        Texture2D sdf = new Texture2D(width, height, TextureFormat.RFloat, false);
        Color[] inputPixels = input.GetPixels();
        float maxDist = Mathf.Sqrt(width * width + height * height);

        List<Vector2Int> objectPixels = new List<Vector2Int>();
        List<Vector2Int> backgroundPixels = new List<Vector2Int>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (inputPixels[y * width + x].a > 0.5f)
                    objectPixels.Add(new Vector2Int(x, y));
                else
                    backgroundPixels.Add(new Vector2Int(x, y));
            }
        }

        float[] signedDistances = new float[width * height];

        if (objectPixels.Count == 0)
        {
            for (int i = 0; i < signedDistances.Length; i++)
                signedDistances[i] = maxDist;
        }
        else if (backgroundPixels.Count == 0)
        {
            for (int i = 0; i < signedDistances.Length; i++)
                signedDistances[i] = -maxDist;
        }
        else
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    bool isInside = inputPixels[idx].a > 0.5f;

                    float minDistToObject = float.MaxValue;
                    float minDistToBackground = float.MaxValue;

                    if (isInside)
                    {
                        foreach (var bg in backgroundPixels)
                        {
                            int dx = x - bg.x;
                            int dy = y - bg.y;
                            float distSq = dx * dx + dy * dy;
                            if (distSq < minDistToBackground)
                                minDistToBackground = distSq;
                        }
                        signedDistances[idx] = -Mathf.Sqrt(minDistToBackground);
                    }
                    else
                    {
                        foreach (var obj in objectPixels)
                        {
                            int dx = x - obj.x;
                            int dy = y - obj.y;
                            float distSq = dx * dx + dy * dy;
                            if (distSq < minDistToObject)
                                minDistToObject = distSq;
                        }
                        signedDistances[idx] = Mathf.Sqrt(minDistToObject);
                    }
                }
            }
        }

        Color[] sdfPixels = new Color[width * height];
        for (int i = 0; i < signedDistances.Length; i++)
        {
            float normDist = signedDistances[i] / maxDist; // [-1, 1]
            sdfPixels[i] = new Color(normDist, 0, 0, 1);
        }
        sdf.SetPixels(sdfPixels);
        sdf.Apply();
        return sdf;
    }

    private Texture2D GenerateOutlineFromSDF(Texture2D sdf, int width, int height)
    {
        Texture2D outline = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Color[] sdfPixels = sdf.GetPixels();
        Color[] outlinePixels = new Color[width * height];

        float maxDist = Mathf.Sqrt(width * width + height * height);
        float halfWidth = outlineWidth * 0.5f;

        for (int i = 0; i < sdfPixels.Length; i++)
        {
            float signedNorm = sdfPixels[i].r; // [-1, 1]
            float signedDist = signedNorm * maxDist;
            float absDist = Mathf.Abs(signedDist);
            float alpha = 0;

            switch (outlinePosition)
            {
                case OutlinePosition.Outside:
                    if (signedDist > 0 && signedDist < outlineWidth)
                    {
                        if (outlineSoftness <= 0 || signedDist <= outlineWidth - outlineSoftness)
                            alpha = 1;
                        else
                            alpha = 1 - (signedDist - (outlineWidth - outlineSoftness)) / outlineSoftness;
                    }
                    break;

                case OutlinePosition.Inside:
                    if (signedDist < 0 && -signedDist < outlineWidth)
                    {
                        float distInside = -signedDist;
                        if (outlineSoftness <= 0 || distInside <= outlineWidth - outlineSoftness)
                            alpha = 1;
                        else
                            alpha = 1 - (distInside - (outlineWidth - outlineSoftness)) / outlineSoftness;
                    }
                    break;

                case OutlinePosition.Center:
                    if (absDist < halfWidth)
                    {
                        if (outlineSoftness <= 0 || absDist <= halfWidth - outlineSoftness)
                            alpha = 1;
                        else
                            alpha = 1 - (absDist - (halfWidth - outlineSoftness)) / outlineSoftness;
                    }
                    break;
            }

            outlinePixels[i] = new Color(outlineColor.r, outlineColor.g, outlineColor.b, alpha * outlineColor.a);
        }
        outline.SetPixels(outlinePixels);
        outline.Apply();
        return outline;
    }



    public override string ToString()
    {
        return outlineWidth.ToString("0.00");
    }
}