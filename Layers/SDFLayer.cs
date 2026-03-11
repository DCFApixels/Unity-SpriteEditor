using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SDFLayer : Layer
{
    public int targetLayerIndex = -1;
    public bool useAccumulation = false;

    public override RenderTexture GetRenderTexture(TextureCompositor compositor, int layerIndex, int width, int height)
    {
        int targetIdx = targetLayerIndex;
        if (targetIdx < 0 || targetIdx >= compositor.layers.Count)
        {
            if (layerIndex > 0) targetIdx = layerIndex - 1;
            else return null;
        }

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

        Texture2D inputTex = ConvertRenderTextureToTexture2D(inputRT);
        RenderTexture.ReleaseTemporary(inputRT);

        Texture2D sdfTex = GenerateSDF(inputTex, width, height);

        RenderTexture result = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(sdfTex, result);

        Object.DestroyImmediate(inputTex);
        Object.DestroyImmediate(sdfTex);

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
        float[] distances = new float[width * height];
        float maxDist = Mathf.Sqrt(width * width + height * height);

        List<Vector2Int> opaquePixels = new List<Vector2Int>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (inputPixels[y * width + x].a > 0.5f)
                    opaquePixels.Add(new Vector2Int(x, y));
            }
        }

        if (opaquePixels.Count == 0)
        {
            for (int i = 0; i < distances.Length; i++)
                distances[i] = maxDist;
        }
        else
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float minDistSq = float.MaxValue;
                    foreach (var op in opaquePixels)
                    {
                        int dx = x - op.x;
                        int dy = y - op.y;
                        float distSq = dx * dx + dy * dy;
                        if (distSq < minDistSq)
                            minDistSq = distSq;
                    }
                    distances[y * width + x] = Mathf.Sqrt(minDistSq);
                }
            }
        }

        Color[] sdfPixels = new Color[width * height];
        for (int i = 0; i < distances.Length; i++)
        {
            float normDist = distances[i] / maxDist;
            sdfPixels[i] = new Color(normDist, normDist, normDist, 1);
        }
        sdf.SetPixels(sdfPixels);
        sdf.Apply();
        return sdf;
    }

    public override string ToString()
    {
        return "SDF";
    }
}