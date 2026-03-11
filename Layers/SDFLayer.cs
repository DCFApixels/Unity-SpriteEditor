using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;



[System.Serializable]
public class SDFLayer : Layer
{
    public enum DistanceMetric
    {
        Euclidean,
        Manhattan,
        Chebyshev
    }

    public enum SourceChannel
    {
        Alpha,
        Red,
        Green,
        Blue,
        Luminance
    }
    public enum DistancePosition { Outside, Inside, Center, Signed }

    public int targetLayerIndex = -1;
    public bool useAccumulation = false;
    public DistanceMetric metric = DistanceMetric.Euclidean;
    public SourceChannel sourceChannel = SourceChannel.Alpha;
    [Range(0, 255)]
    public byte threshold = 128;
    public DistancePosition distancePosition = DistancePosition.Signed;
    public bool inverted = false;
    public float maxDistanceNormalization = 0f; // 0 = auto (диагональ)

    public override RenderTexture GetRenderTexture(TextureCompositor compositor, int layerIndex, int width, int height)
    {
        // Определение целевого слоя (как раньше)
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
                inputRT = targetLayer.GetRenderTexture(compositor, targetIdx, width, height);
        }
        if (inputRT == null) return null;

        Texture2D inputTex = ConvertToTexture2D(inputRT);
        RenderTexture.ReleaseTemporary(inputRT);

        NativeArray<Color32> inputPixels = new NativeArray<Color32>(inputTex.GetPixels32(), Allocator.TempJob);
        int pixelCount = inputPixels.Length;
        NativeArray<float> signedDistances = new NativeArray<float>(pixelCount, Allocator.TempJob);

        // Выбираем job в зависимости от метрики
        if (metric == DistanceMetric.Euclidean)
        {
            var sdfJob = new ComputeSDFEuclideanJob
            {
                input = inputPixels,
                distances = signedDistances,
                width = width,
                height = height,
                threshold = threshold,
                sourceChannel = (int)sourceChannel
            };
            sdfJob.Run();
        }
        else if (metric == DistanceMetric.Manhattan)
        {
            var sdfJob = new ComputeSDFManhattanJob
            {
                input = inputPixels,
                distances = signedDistances,
                width = width,
                height = height,
                threshold = threshold,
                sourceChannel = (int)sourceChannel
            };
            sdfJob.Run();
        }
        else // Chebyshev
        {
            var sdfJob = new ComputeSDFChebyshevJob
            {
                input = inputPixels,
                distances = signedDistances,
                width = width,
                height = height,
                threshold = threshold,
                sourceChannel = (int)sourceChannel
            };
            sdfJob.Run();
        }

        // Нормализация для вывода (как раньше, но теперь с учётом метрики? Нормируем на максимальное возможное расстояние в этой метрике)
        // Для простоты нормируем на диагональ (maxDist), что даст диапазон [0,1] для всех метрик, но для L1 и L∞ реальное максимальное расстояние может быть другим.
        // Можно вычислить максимальное расстояние в данной метрике: для L1 это width+height, для L∞ это max(width,height). 
        // Но для визуализации лучше нормировать на диагональ, чтобы сохранить пропорции. Оставим как есть.
        NativeArray<float> displayDistances = new NativeArray<float>(pixelCount, Allocator.Temp);
        for (int i = 0; i < pixelCount; i++)
        {
            float d = signedDistances[i];
            switch (distancePosition)
            {
                case DistancePosition.Outside:
                    d = math.max(d, 0);
                    break;
                case DistancePosition.Inside:
                    d = math.max(-d, 0);
                    break;
                case DistancePosition.Center:
                    d = math.abs(d);
                    break;
                case DistancePosition.Signed:
                    // Оставляем как есть (знаковое)
                    break;
            }
            displayDistances[i] = d;
        }

        // Нормализуем displayDistances
        //float maxDist = maxDistanceNormalization > 0 ? maxDistanceNormalization : Mathf.Sqrt(width * width + height * height);
        //NativeArray<Color32> outputPixels = new NativeArray<Color32>(pixelCount, Allocator.TempJob);
        //for (int i = 0; i < pixelCount; i++)
        //{
        //    float norm = math.clamp(displayDistances[i] / maxDist, 0, 1);
        //    byte val = (byte)(norm * 255);
        //    outputPixels[i] = new Color32(val, val, val, 255);
        //}
        //displayDistances.Dispose();
        // Нормализуем displayDistances
        float maxDist = maxDistanceNormalization > 0 ? maxDistanceNormalization : Mathf.Sqrt(width * width + height * height);
        NativeArray<Color32> outputPixels = new NativeArray<Color32>(pixelCount, Allocator.TempJob);
        for (int i = 0; i < pixelCount; i++)
        {
            float norm;
            if (distancePosition == DistancePosition.Signed)
            {
                // Знаковое: отображаем в диапазон [0,1] так, чтобы -maxDist -> 0, +maxDist -> 1
                norm = (displayDistances[i] + maxDist) / (2 * maxDist);
            }
            else
            {
                // Для Outside, Inside, Center: диапазон [0, maxDist] -> [0,1]
                norm = displayDistances[i] / maxDist;
            }
            norm = math.clamp(norm, 0, 1);
            if (inverted) norm = 1 - norm;
            byte val = (byte)(norm * 255);
            outputPixels[i] = new Color32(val, val, val, 255);
        }
        displayDistances.Dispose();

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
}

// Базовый интерфейс для всех SDF job-ов
public interface ISDFJob
{
    void Execute();
}

[BurstCompile]
public struct ComputeSDFEuclideanJob : IJob
{
    [ReadOnly] public NativeArray<Color32> input;
    public NativeArray<float> distances;
    public int width;
    public int height;
    public byte threshold;
    public int sourceChannel; // 0=Alpha,1=Red,2=Green,3=Blue,4=Luminance

    private float GetValue(Color32 c)
    {
        if (sourceChannel == 0) return c.a / 255f;
        if (sourceChannel == 1) return c.r / 255f;
        if (sourceChannel == 2) return c.g / 255f;
        if (sourceChannel == 3) return c.b / 255f;
        // luminance
        return (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
    }

    public void Execute()
    {
        int total = width * height;
        float inf = 1e10f;

        NativeArray<float> distObj = new NativeArray<float>(total, Allocator.Temp);
        NativeArray<float> distBg = new NativeArray<float>(total, Allocator.Temp);

        // Инициализация
        for (int i = 0; i < total; i++)
        {
            float val = GetValue(input[i]);
            bool isObj = val > (threshold / 255f);
            distObj[i] = isObj ? 0 : inf;
            distBg[i] = isObj ? inf : 0;
        }

        // 8SSEDT для объекта и фона (евклидово)
        Compute8SSEDT(distObj, width, height);
        Compute8SSEDT(distBg, width, height);

        // Комбинирование в знаковое расстояние
        for (int i = 0; i < total; i++)
        {
            bool isObj = GetValue(input[i]) > (threshold / 255f);
            distances[i] = isObj ? -distBg[i] : distObj[i];
        }

        distObj.Dispose();
        distBg.Dispose();
    }

    private void Compute8SSEDT(NativeArray<float> d, int w, int h)
    {
        float sqrt2 = math.sqrt(2f);
        // Прямой проход
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                float best = d[idx];
                if (x > 0) best = math.min(best, d[idx - 1] + 1);
                if (y > 0)
                {
                    best = math.min(best, d[idx - w] + 1);
                    if (x > 0) best = math.min(best, d[idx - w - 1] + sqrt2);
                    if (x < w - 1) best = math.min(best, d[idx - w + 1] + sqrt2);
                }
                d[idx] = best;
            }
        }
        // Обратный проход
        for (int y = h - 1; y >= 0; y--)
        {
            for (int x = w - 1; x >= 0; x--)
            {
                int idx = y * w + x;
                float best = d[idx];
                if (x < w - 1) best = math.min(best, d[idx + 1] + 1);
                if (y < h - 1)
                {
                    best = math.min(best, d[idx + w] + 1);
                    if (x > 0) best = math.min(best, d[idx + w - 1] + sqrt2);
                    if (x < w - 1) best = math.min(best, d[idx + w + 1] + sqrt2);
                }
                d[idx] = best;
            }
        }
    }
}

[BurstCompile]
public struct ComputeSDFManhattanJob : IJob
{
    [ReadOnly] public NativeArray<Color32> input;
    public NativeArray<float> distances;
    public int width;
    public int height;
    public byte threshold;
    public int sourceChannel;

    private float GetValue(Color32 c) // аналогично
    {
        if (sourceChannel == 0) return c.a / 255f;
        if (sourceChannel == 1) return c.r / 255f;
        if (sourceChannel == 2) return c.g / 255f;
        if (sourceChannel == 3) return c.b / 255f;
        return (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
    }

    public void Execute()
    {
        int total = width * height;
        float inf = 1e10f;

        NativeArray<float> distObj = new NativeArray<float>(total, Allocator.Temp);
        NativeArray<float> distBg = new NativeArray<float>(total, Allocator.Temp);

        for (int i = 0; i < total; i++)
        {
            float val = GetValue(input[i]);
            bool isObj = val > (threshold / 255f);
            distObj[i] = isObj ? 0 : inf;
            distBg[i] = isObj ? inf : 0;
        }

        ComputeManhattanDT(distObj, width, height);
        ComputeManhattanDT(distBg, width, height);

        for (int i = 0; i < total; i++)
        {
            bool isObj = GetValue(input[i]) > (threshold / 255f);
            distances[i] = isObj ? -distBg[i] : distObj[i];
        }

        distObj.Dispose();
        distBg.Dispose();
    }

    private void ComputeManhattanDT(NativeArray<float> d, int w, int h)
    {
        // Для Манхэттенского расстояния: горизонтальные/вертикальные соседи добавляют 1, диагональные добавляют 2 (|dx|+|dy| = 2)
        float diagCost = 2f;
        // Прямой проход
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                float best = d[idx];
                if (x > 0) best = math.min(best, d[idx - 1] + 1);
                if (y > 0)
                {
                    best = math.min(best, d[idx - w] + 1);
                    if (x > 0) best = math.min(best, d[idx - w - 1] + diagCost);
                    if (x < w - 1) best = math.min(best, d[idx - w + 1] + diagCost);
                }
                d[idx] = best;
            }
        }
        // Обратный проход
        for (int y = h - 1; y >= 0; y--)
        {
            for (int x = w - 1; x >= 0; x--)
            {
                int idx = y * w + x;
                float best = d[idx];
                if (x < w - 1) best = math.min(best, d[idx + 1] + 1);
                if (y < h - 1)
                {
                    best = math.min(best, d[idx + w] + 1);
                    if (x > 0) best = math.min(best, d[idx + w - 1] + diagCost);
                    if (x < w - 1) best = math.min(best, d[idx + w + 1] + diagCost);
                }
                d[idx] = best;
            }
        }
    }
}

[BurstCompile]
public struct ComputeSDFChebyshevJob : IJob
{
    [ReadOnly] public NativeArray<Color32> input;
    public NativeArray<float> distances;
    public int width;
    public int height;
    public byte threshold;
    public int sourceChannel;

    private float GetValue(Color32 c) // аналогично
    {
        if (sourceChannel == 0) return c.a / 255f;
        if (sourceChannel == 1) return c.r / 255f;
        if (sourceChannel == 2) return c.g / 255f;
        if (sourceChannel == 3) return c.b / 255f;
        return (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
    }

    public void Execute()
    {
        int total = width * height;
        float inf = 1e10f;

        NativeArray<float> distObj = new NativeArray<float>(total, Allocator.Temp);
        NativeArray<float> distBg = new NativeArray<float>(total, Allocator.Temp);

        for (int i = 0; i < total; i++)
        {
            float val = GetValue(input[i]);
            bool isObj = val > (threshold / 255f);
            distObj[i] = isObj ? 0 : inf;
            distBg[i] = isObj ? inf : 0;
        }

        ComputeChebyshevDT(distObj, width, height);
        ComputeChebyshevDT(distBg, width, height);

        for (int i = 0; i < total; i++)
        {
            bool isObj = GetValue(input[i]) > (threshold / 255f);
            distances[i] = isObj ? -distBg[i] : distObj[i];
        }

        distObj.Dispose();
        distBg.Dispose();
    }

    private void ComputeChebyshevDT(NativeArray<float> d, int w, int h)
    {
        // Для Чебышёвского: все соседи добавляют 1 (max(|dx|,|dy|)=1)
        float diagCost = 1f;
        // Прямой проход
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                float best = d[idx];
                if (x > 0) best = math.min(best, d[idx - 1] + 1);
                if (y > 0)
                {
                    best = math.min(best, d[idx - w] + 1);
                    if (x > 0) best = math.min(best, d[idx - w - 1] + diagCost);
                    if (x < w - 1) best = math.min(best, d[idx - w + 1] + diagCost);
                }
                d[idx] = best;
            }
        }
        // Обратный проход
        for (int y = h - 1; y >= 0; y--)
        {
            for (int x = w - 1; x >= 0; x--)
            {
                int idx = y * w + x;
                float best = d[idx];
                if (x < w - 1) best = math.min(best, d[idx + 1] + 1);
                if (y < h - 1)
                {
                    best = math.min(best, d[idx + w] + 1);
                    if (x > 0) best = math.min(best, d[idx + w - 1] + diagCost);
                    if (x < w - 1) best = math.min(best, d[idx + w + 1] + diagCost);
                }
                d[idx] = best;
            }
        }
    }
}