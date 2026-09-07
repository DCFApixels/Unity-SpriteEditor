using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [Serializable]
    public sealed class SDFLayer : TargetedLayerEffect
    {
        public DistanceMetric metric = DistanceMetric.EuclideanExact;
        public SourceChannel sourceChannel = SourceChannel.Alpha;
        [Range(0, 255)] public byte threshold = 128;
        public DistancePosition distancePosition = DistancePosition.Signed;
        public bool inverted;
        public float maxDistanceNormalization;
        public Gradient gradient = GradientUtility.Create(GradientUtility.WhiteToBlack);

        internal override RenderTexture Render(in LayerRenderContext context)
        {
            if (context.input == null)
                return null;

            Texture2D inputTexture = TextureCompositor.CopyToTexture2D(context.input);
            NativeArray<float> signedDistances = default;
            NativeArray<Color32> outputPixels = default;
            Texture2D resultTexture = null;
            try
            {
                NativeArray<Color32> inputPixels = inputTexture.GetRawTextureData<Color32>();
                signedDistances = new NativeArray<float>(inputPixels.Length, Allocator.TempJob);
                DistanceFieldUtility.ComputeSignedDistance(
                    inputPixels,
                    signedDistances,
                    context.width,
                    context.height,
                    threshold,
                    (int)sourceChannel,
                    metric);

                outputPixels = new NativeArray<Color32>(inputPixels.Length, Allocator.TempJob);
                float maxDistance = GetNormalizationDistance(context);
                bool isTwoColorGradient = GradientUtility.IsTwoColorGradient(gradient, out Color left, out Color right);
                Gradient evaluatedGradient = gradient ?? GradientUtility.WhiteToBlack;

                for (int i = 0; i < signedDistances.Length; i++)
                {
                    float distance = ConvertDistance(signedDistances[i]);
                    float normalized = distancePosition == DistancePosition.Signed
                        ? (distance + maxDistance) / (2f * maxDistance)
                        : distance / maxDistance;
                    normalized = math.clamp(normalized, 0f, 1f);
                    if (inverted)
                        normalized = 1f - normalized;

                    outputPixels[i] = isTwoColorGradient
                        ? (Color32)Color.Lerp(left, right, normalized)
                        : (Color32)evaluatedGradient.Evaluate(normalized);
                }

                resultTexture = new Texture2D(context.width, context.height, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                resultTexture.SetPixelData(outputPixels, 0);
                resultTexture.Apply(false, false);
                return ApplyTransformAndModifiers(resultTexture, context);
            }
            finally
            {
                if (signedDistances.IsCreated)
                    signedDistances.Dispose();
                if (outputPixels.IsCreated)
                    outputPixels.Dispose();
                if (inputTexture != null)
                    UnityEngine.Object.DestroyImmediate(inputTexture);
                if (resultTexture != null)
                    UnityEngine.Object.DestroyImmediate(resultTexture);
            }
        }

        public override string ToString()
        {
            return "SDF";
        }

        private float ConvertDistance(float signedDistance)
        {
            switch (distancePosition)
            {
                case DistancePosition.Outside:
                    return math.max(signedDistance, 0f);
                case DistancePosition.Inside:
                    return math.max(-signedDistance, 0f);
                case DistancePosition.Center:
                    return math.abs(signedDistance);
                default:
                    return signedDistance;
            }
        }

        private float GetNormalizationDistance(in LayerRenderContext context)
        {
            if (maxDistanceNormalization > 0f)
                return Mathf.Max(0.0001f, maxDistanceNormalization / context.scaleMultiplier);

            switch (metric)
            {
                case DistanceMetric.Manhattan:
                    return Mathf.Max(1f, context.width + context.height);
                case DistanceMetric.Chebyshev:
                    return Mathf.Max(context.width, context.height);
                default:
                    return Mathf.Max(1f, Mathf.Sqrt((float)context.width * context.width + (float)context.height * context.height));
            }
        }

        public enum SourceChannel
        {
            Alpha,
            Red,
            Green,
            Blue,
            Luminance
        }

        public enum DistancePosition
        {
            Outside,
            Inside,
            Center,
            Signed
        }
    }

    internal static class DistanceFieldUtility
    {
        public static void ComputeSignedDistance(
            NativeArray<Color32> input,
            NativeArray<float> output,
            int width,
            int height,
            byte threshold,
            int sourceChannel,
            DistanceMetric metric)
        {
            NativeArray<float> distanceToObject = new NativeArray<float>(input.Length, Allocator.TempJob);
            NativeArray<float> distanceToBackground = new NativeArray<float>(input.Length, Allocator.TempJob);
            try
            {
                if (metric == DistanceMetric.EuclideanExact)
                {
                    int maximumLineLength = math.max(width, height);
                    NativeArray<float> temporary = new NativeArray<float>(input.Length, Allocator.TempJob);
                    NativeArray<float> lineInput = new NativeArray<float>(maximumLineLength, Allocator.TempJob);
                    NativeArray<float> lineOutput = new NativeArray<float>(maximumLineLength, Allocator.TempJob);
                    NativeArray<int> vertices = new NativeArray<int>(maximumLineLength, Allocator.TempJob);
                    NativeArray<float> boundaries = new NativeArray<float>(maximumLineLength + 1, Allocator.TempJob);
                    try
                    {
                        ExactSignedDistanceJob job = new ExactSignedDistanceJob
                        {
                            input = input,
                            output = output,
                            distanceToObject = distanceToObject,
                            distanceToBackground = distanceToBackground,
                            temporary = temporary,
                            lineInput = lineInput,
                            lineOutput = lineOutput,
                            vertices = vertices,
                            boundaries = boundaries,
                            width = width,
                            height = height,
                            threshold = threshold,
                            sourceChannel = sourceChannel
                        };
                        job.Run();
                    }
                    finally
                    {
                        temporary.Dispose();
                        lineInput.Dispose();
                        lineOutput.Dispose();
                        vertices.Dispose();
                        boundaries.Dispose();
                    }
                }
                else
                {
                    float diagonalCost;
                    switch (metric)
                    {
                        case DistanceMetric.Manhattan:
                            diagonalCost = 2f;
                            break;
                        case DistanceMetric.Chebyshev:
                            diagonalCost = 1f;
                            break;
                        default:
                            diagonalCost = math.sqrt(2f);
                            break;
                    }

                    ApproximateSignedDistanceJob job = new ApproximateSignedDistanceJob
                    {
                        input = input,
                        output = output,
                        distanceToObject = distanceToObject,
                        distanceToBackground = distanceToBackground,
                        width = width,
                        height = height,
                        threshold = threshold,
                        sourceChannel = sourceChannel,
                        diagonalCost = diagonalCost
                    };
                    job.Run();
                }
            }
            finally
            {
                distanceToObject.Dispose();
                distanceToBackground.Dispose();
            }
        }
    }

    [BurstCompile]
    internal struct ApproximateSignedDistanceJob : IJob
    {
        [ReadOnly] public NativeArray<Color32> input;
        public NativeArray<float> output;
        public NativeArray<float> distanceToObject;
        public NativeArray<float> distanceToBackground;
        public int width;
        public int height;
        public byte threshold;
        public int sourceChannel;
        public float diagonalCost;

        public void Execute()
        {
            const float infinity = 1e10f;
            for (int i = 0; i < input.Length; i++)
            {
                bool isObject = DistanceFieldSource.IsObject(input[i], sourceChannel, threshold);
                distanceToObject[i] = isObject ? 0f : infinity;
                distanceToBackground[i] = isObject ? infinity : 0f;
            }

            Transform(distanceToObject);
            Transform(distanceToBackground);
            for (int i = 0; i < input.Length; i++)
            {
                bool isObject = DistanceFieldSource.IsObject(input[i], sourceChannel, threshold);
                output[i] = isObject ? -distanceToBackground[i] : distanceToObject[i];
            }
        }

        private void Transform(NativeArray<float> distances)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float best = distances[index];
                    if (x > 0)
                        best = math.min(best, distances[index - 1] + 1f);
                    if (y > 0)
                    {
                        best = math.min(best, distances[index - width] + 1f);
                        if (x > 0)
                            best = math.min(best, distances[index - width - 1] + diagonalCost);
                        if (x < width - 1)
                            best = math.min(best, distances[index - width + 1] + diagonalCost);
                    }
                    distances[index] = best;
                }
            }

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = width - 1; x >= 0; x--)
                {
                    int index = y * width + x;
                    float best = distances[index];
                    if (x < width - 1)
                        best = math.min(best, distances[index + 1] + 1f);
                    if (y < height - 1)
                    {
                        best = math.min(best, distances[index + width] + 1f);
                        if (x > 0)
                            best = math.min(best, distances[index + width - 1] + diagonalCost);
                        if (x < width - 1)
                            best = math.min(best, distances[index + width + 1] + diagonalCost);
                    }
                    distances[index] = best;
                }
            }
        }
    }

    [BurstCompile]
    internal struct ExactSignedDistanceJob : IJob
    {
        [ReadOnly] public NativeArray<Color32> input;
        public NativeArray<float> output;
        public NativeArray<float> distanceToObject;
        public NativeArray<float> distanceToBackground;
        public NativeArray<float> temporary;
        public NativeArray<float> lineInput;
        public NativeArray<float> lineOutput;
        public NativeArray<int> vertices;
        public NativeArray<float> boundaries;
        public int width;
        public int height;
        public byte threshold;
        public int sourceChannel;

        public void Execute()
        {
            bool hasObject = false;
            bool hasBackground = false;
            float maximumSquaredDistance = (float)width * width + (float)height * height;
            float largeValue = maximumSquaredDistance * 4f + 1f;

            for (int i = 0; i < input.Length; i++)
            {
                bool isObject = DistanceFieldSource.IsObject(input[i], sourceChannel, threshold);
                hasObject |= isObject;
                hasBackground |= !isObject;
                distanceToObject[i] = isObject ? 0f : largeValue;
                distanceToBackground[i] = isObject ? largeValue : 0f;
            }

            float maximumDistance = math.sqrt(maximumSquaredDistance);
            if (!hasObject || !hasBackground)
            {
                float value = hasObject ? -maximumDistance : maximumDistance;
                for (int i = 0; i < output.Length; i++)
                    output[i] = value;
                return;
            }

            Transform2D(distanceToObject);
            Transform2D(distanceToBackground);
            for (int i = 0; i < input.Length; i++)
            {
                bool isObject = DistanceFieldSource.IsObject(input[i], sourceChannel, threshold);
                output[i] = isObject
                    ? -math.sqrt(distanceToBackground[i])
                    : math.sqrt(distanceToObject[i]);
            }
        }

        private void Transform2D(NativeArray<float> distances)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                    lineInput[y] = distances[y * width + x];
                Transform1D(height);
                for (int y = 0; y < height; y++)
                    temporary[y * width + x] = lineOutput[y];
            }

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                    lineInput[x] = temporary[row + x];
                Transform1D(width);
                for (int x = 0; x < width; x++)
                    distances[row + x] = lineOutput[x];
            }
        }

        private void Transform1D(int length)
        {
            int envelopeSize = 0;
            vertices[0] = 0;
            boundaries[0] = -1e20f;
            boundaries[1] = 1e20f;

            for (int q = 1; q < length; q++)
            {
                float intersection;
                while (true)
                {
                    int p = vertices[envelopeSize];
                    intersection = ((lineInput[q] + q * q) - (lineInput[p] + p * p)) / (2f * (q - p));
                    if (intersection > boundaries[envelopeSize])
                        break;
                    envelopeSize--;
                }

                envelopeSize++;
                vertices[envelopeSize] = q;
                boundaries[envelopeSize] = intersection;
                boundaries[envelopeSize + 1] = 1e20f;
            }

            envelopeSize = 0;
            for (int q = 0; q < length; q++)
            {
                while (boundaries[envelopeSize + 1] < q)
                    envelopeSize++;
                int p = vertices[envelopeSize];
                float delta = q - p;
                lineOutput[q] = delta * delta + lineInput[p];
            }
        }
    }

    [BurstCompile]
    internal static class DistanceFieldSource
    {
        public static bool IsObject(Color32 color, int sourceChannel, byte threshold)
        {
            float value;
            switch (sourceChannel)
            {
                case 1:
                    value = color.r;
                    break;
                case 2:
                    value = color.g;
                    break;
                case 3:
                    value = color.b;
                    break;
                case 4:
                    value = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
                    break;
                default:
                    value = color.a;
                    break;
            }
            return value > threshold;
        }
    }
}
