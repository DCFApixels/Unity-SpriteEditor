using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

[System.Serializable]
public class OutlineLayer : Layer
{
    public int targetLayerIndex = -1;
    public bool useAccumulation = false;
    public Color outlineColor = Color.white;
    public float outlineWidth = 0.05f;
    public float outlineSoftness = 0.01f;
    public enum OutlinePosition { Outside, Inside, Center }
    public OutlinePosition outlinePosition = OutlinePosition.Outside;

    public override RenderTexture GetRenderTexture(TextureCompositor compositor, int layerIndex, int width, int height)
    {
        // Определяем целевой слой
        int targetIdx = targetLayerIndex;
        if (targetIdx < 0 || targetIdx >= compositor.layers.Count)
        {
            if (layerIndex > 0) targetIdx = layerIndex - 1;
            else return null;
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
            if (targetLayer != null)
            {
                inputRT = targetLayer.GetRenderTexture(compositor, targetIdx, width, height);
            }
        }
        if (inputRT == null) return null;

        // Конвертируем в Texture2D для доступа к пикселям
        Texture2D inputTex = ConvertToTexture2D(inputRT);
        RenderTexture.ReleaseTemporary(inputRT);

        // Получаем пиксели как NativeArray<Color32>
        NativeArray<Color32> inputPixels = new NativeArray<Color32>(inputTex.GetPixels32(), Allocator.TempJob);
        int pixelCount = inputPixels.Length;

        // Массив для знаковых расстояний (float)
        NativeArray<float> signedDistances = new NativeArray<float>(pixelCount, Allocator.TempJob);

        // Запускаем SDF job
        var sdfJob = new ComputeSDFJob
        {
            input = inputPixels,
            distances = signedDistances,
            width = width,
            height = height,
            threshold = 128 // альфа > 0.5
        };
        sdfJob.Run(); // выполняется синхронно (можно Schedule, но для простоты Run)

        // Массив для выходных пикселей
        NativeArray<Color32> outputPixels = new NativeArray<Color32>(pixelCount, Allocator.TempJob);

        // Запускаем Outline job параллельно
        var outlineJob = new OutlineJob
        {
            signedDistances = signedDistances,
            output = outputPixels,
            width = width,
            height = height,
            outlineWidth = outlineWidth * Mathf.Sqrt(width * width + height * height), // переводим в пиксели
            outlineSoftness = outlineSoftness * Mathf.Sqrt(width * width + height * height),
            outlineColor = (Color32)outlineColor,
            outlinePosition = (int)outlinePosition
        };
        JobHandle handle = outlineJob.Schedule(pixelCount, 64); // батч 64
        handle.Complete();

        // Создаём Texture2D из результата
        Texture2D resultTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        resultTex.SetPixels32(outputPixels.ToArray());
        resultTex.Apply();

        // Освобождаем NativeArray
        inputPixels.Dispose();
        signedDistances.Dispose();
        outputPixels.Dispose();

        // Копируем в RenderTexture
        RenderTexture resultRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(resultTex, resultRT);
        Object.DestroyImmediate(inputTex);
        Object.DestroyImmediate(resultTex);

        // Применяем модификаторы
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
        return $"Outline: {outlineWidth}";
    }
}

[BurstCompile]
public struct ComputeSDFJob : IJob
{
    [ReadOnly] public NativeArray<Color32> input;
    public NativeArray<float> distances; // выходные знаковые расстояния в пикселях
    public int width;
    public int height;
    public byte threshold;

    public void Execute()
    {
        int total = width * height;
        float inf = 1e10f;

        // Массивы для расстояний до объекта и до фона
        NativeArray<float> distObj = new NativeArray<float>(total, Allocator.Temp);
        NativeArray<float> distBg = new NativeArray<float>(total, Allocator.Temp);

        // Инициализация
        for (int i = 0; i < total; i++)
        {
            bool isObj = input[i].a > threshold;
            distObj[i] = isObj ? 0 : inf;
            distBg[i] = isObj ? inf : 0;
        }

        // 8SSEDT для объекта
        Compute8SSEDT(distObj, width, height);
        // 8SSEDT для фона
        Compute8SSEDT(distBg, width, height);

        // Комбинируем в знаковое расстояние
        for (int i = 0; i < total; i++)
        {
            bool isObj = input[i].a > threshold;
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
public struct OutlineJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> signedDistances;
    public NativeArray<Color32> output;
    public int width;
    public int height;
    public float outlineWidth; // в пикселях
    public float outlineSoftness; // в пикселях
    public Color32 outlineColor;
    public int outlinePosition; // 0=outside,1=inside,2=center

    public void Execute(int index)
    {
        float d = signedDistances[index];
        float absD = math.abs(d);
        float alpha = 0;

        if (outlinePosition == 0) // outside
        {
            if (d > 0 && d < outlineWidth)
            {
                if (outlineSoftness <= 0 || d <= outlineWidth - outlineSoftness)
                    alpha = 1;
                else
                    alpha = 1 - (d - (outlineWidth - outlineSoftness)) / outlineSoftness;
            }
        }
        else if (outlinePosition == 1) // inside
        {
            if (d < 0 && -d < outlineWidth)
            {
                float insideDist = -d;
                if (outlineSoftness <= 0 || insideDist <= outlineWidth - outlineSoftness)
                    alpha = 1;
                else
                    alpha = 1 - (insideDist - (outlineWidth - outlineSoftness)) / outlineSoftness;
            }
        }
        else // center
        {
            float half = outlineWidth * 0.5f;
            if (absD < half)
            {
                if (outlineSoftness <= 0 || absD <= half - outlineSoftness)
                    alpha = 1;
                else
                    alpha = 1 - (absD - (half - outlineSoftness)) / outlineSoftness;
            }
        }

        output[index] = new Color32(
            (byte)(outlineColor.r * alpha),
            (byte)(outlineColor.g * alpha),
            (byte)(outlineColor.b * alpha),
            (byte)(outlineColor.a * alpha)
        );
    }
}