using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [Serializable]
    public sealed class OutlineLayer : TargetedLayerEffect
    {
        public DistanceMetric metric = DistanceMetric.EuclideanExact;
        public Color outlineColor = Color.white;
        public float outlineWidth = 4f;
        public float outlineSoftness = 1f;
        public OutlinePosition outlinePosition = OutlinePosition.Outside;

        internal override RenderTexture Render(in LayerRenderContext context)
        {
            if (context.input == null)
                return null;

            Texture2D inputTexture = TextureCompositor.CopyToTexture2D(context.input, uploadToGpu: false);
            NativeArray<float> signedDistances = default;
            Texture2D resultTexture = null;
            try
            {
                NativeArray<Color32> inputPixels = inputTexture.GetRawTextureData<Color32>();
                signedDistances = new NativeArray<float>(
                    inputPixels.Length,
                    Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory);
                DistanceFieldUtility.ComputeSignedDistance(
                    inputPixels,
                    signedDistances,
                    context.width,
                    context.height,
                    128,
                    (int)SDFLayer.SourceChannel.Alpha,
                    metric);

                resultTexture = new Texture2D(context.width, context.height, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                NativeArray<Color32> outputPixels = resultTexture.GetRawTextureData<Color32>();
                OutlineJob job = new OutlineJob
                {
                    signedDistances = signedDistances,
                    output = outputPixels,
                    outlineWidth = Mathf.Max(0f, outlineWidth / context.scaleMultiplier),
                    outlineSoftness = Mathf.Max(0f, outlineSoftness / context.scaleMultiplier),
                    outlineColor = (Color32)outlineColor,
                    outlinePosition = (int)outlinePosition
                };
                job.Schedule(outputPixels.Length, 128).Complete();

                resultTexture.Apply(false, false);
                return ApplyTransformAndModifiers(resultTexture, context);
            }
            finally
            {
                if (signedDistances.IsCreated)
                    signedDistances.Dispose();
                if (inputTexture != null)
                    UnityEngine.Object.DestroyImmediate(inputTexture);
                if (resultTexture != null)
                    UnityEngine.Object.DestroyImmediate(resultTexture);
            }
        }

        public override string ToString()
        {
            return $"Outline: {outlineWidth:0.##} px";
        }

        public enum OutlinePosition
        {
            Outside,
            Inside,
            Center
        }
    }

    [BurstCompile]
    internal struct OutlineJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> signedDistances;
        public NativeArray<Color32> output;
        public float outlineWidth;
        public float outlineSoftness;
        public Color32 outlineColor;
        public int outlinePosition;

        public void Execute(int index)
        {
            float distance = signedDistances[index];
            float range = outlinePosition == (int)OutlineLayer.OutlinePosition.Center
                ? outlineWidth * 0.5f
                : outlineWidth;
            float edgeDistance;
            bool insideRange;

            if (outlinePosition == (int)OutlineLayer.OutlinePosition.Outside)
            {
                edgeDistance = distance;
                insideRange = distance > 0f && distance <= range;
            }
            else if (outlinePosition == (int)OutlineLayer.OutlinePosition.Inside)
            {
                edgeDistance = -distance;
                insideRange = distance < 0f && -distance <= range;
            }
            else
            {
                edgeDistance = math.abs(distance);
                insideRange = edgeDistance <= range;
            }

            float alpha = 0f;
            if (insideRange)
            {
                float softness = math.min(outlineSoftness, range);
                alpha = softness <= 0f
                    ? 1f
                    : math.saturate((range - edgeDistance) / softness);
            }

            output[index] = new Color32(
                outlineColor.r,
                outlineColor.g,
                outlineColor.b,
                (byte)math.round(outlineColor.a * alpha));
        }
    }
}
