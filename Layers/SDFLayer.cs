using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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
            if (targetLayer != null)
            {
                inputRT = targetLayer.GetRenderTexture(compositor, targetIdx, width, height);
            }
        }
        if (inputRT == null) return null;

        Texture2D inputTex = ConvertToTexture2D(inputRT);
        RenderTexture.ReleaseTemporary(inputRT);

        NativeArray<Color32> inputPixels = new NativeArray<Color32>(inputTex.GetPixels32(), Allocator.TempJob);
        int pixelCount = inputPixels.Length;
        NativeArray<float> signedDistances = new NativeArray<float>(pixelCount, Allocator.TempJob);

        var sdfJob = new ComputeSDFJob
        {
            input = inputPixels,
            distances = signedDistances,
            width = width,
            height = height,
            threshold = 128
        };
        sdfJob.Run();

        // Для SDF слоя мы выводим нормализованное расстояние в красном канале
        float maxDist = Mathf.Sqrt(width * width + height * height);
        NativeArray<Color32> outputPixels = new NativeArray<Color32>(pixelCount, Allocator.TempJob);
        for (int i = 0; i < pixelCount; i++)
        {
            float normDist = math.clamp((signedDistances[i] + maxDist) / (2 * maxDist), 0, 1); // отображаем в 0..1
            byte val = (byte)(normDist * 255);
            outputPixels[i] = new Color32(val, val, val, 255);
        }

        Texture2D resultTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        resultTex.SetPixels32(outputPixels.ToArray());
        resultTex.Apply();

        inputPixels.Dispose();
        signedDistances.Dispose();
        outputPixels.Dispose();

        RenderTexture resultRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(resultTex, resultRT);
        Object.DestroyImmediate(inputTex);
        Object.DestroyImmediate(resultTex);

        foreach (var modifier in modifiers)
        {
            if (modifier != null)
            {
                RenderTexture temp = RenderTexture.GetTemporary(resultRT.width, resultRT.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(resultRT, temp, modifier);
                RenderTexture.ReleaseTemporary(resultRT);
                resultRT = temp;
            }
        }

        return resultRT;
    }

    private Texture2D ConvertToTexture2D(RenderTexture rt)
    {
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        return tex;
    }

    public override string ToString()
    {
        return "SDF";
    }
}