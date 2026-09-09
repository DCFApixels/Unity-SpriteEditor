using System;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [Serializable]
    public sealed class GradientLayer : Layer
    {
        public GradientType gradientType = GradientType.Vertical;
        public Gradient gradient = GradientUtility.Create(GradientUtility.WhiteToBlack);
        public Vector2 center = new Vector2(0.5f, 0.5f);
        public float radius = 0.5f;
        public float circularRepetitions = 1f;
        public WrapMode circularWrapMode = WrapMode.Repeat;

        [NonSerialized] private Texture2D cachedPreview;
        [NonSerialized] private int cachedHash;

        public override Texture2D GetPreviewTexture(int size)
        {
            size = Mathf.Max(1, size);
            int currentHash = ComputeHash();
            if (cachedPreview != null && cachedHash == currentHash && cachedPreview.width == size)
                return cachedPreview;

            ReleaseTransientResources();
            cachedPreview = GenerateGradientTexture(size, size);
            cachedHash = currentHash;
            return cachedPreview;
        }

        internal override RenderTexture Render(in LayerRenderContext context)
        {
            Texture2D texture = GenerateGradientTexture(context.width, context.height);
            try
            {
                return ApplyTransformAndModifiers(texture, context);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        public float GetGradientCoord(float u, float v)
        {
            switch (gradientType)
            {
                case GradientType.Vertical:
                    return v;
                case GradientType.Horizontal:
                    return u;
                case GradientType.Radial:
                    return NormalizeDistance(Vector2.Distance(new Vector2(u, v), center));
                case GradientType.Circular:
                {
                    Vector2 direction = new Vector2(u - center.x, v - center.y);
                    float angle = Mathf.Atan2(direction.y, direction.x);
                    float turn = (angle + Mathf.PI) / (2f * Mathf.PI);
                    float repeated = turn * Mathf.Max(float.Epsilon, circularRepetitions);
                    return circularWrapMode == WrapMode.PingPong
                        ? Mathf.PingPong(repeated, 1f)
                        : repeated - Mathf.Floor(repeated);
                }
                case GradientType.Diamond:
                    return NormalizeDistance(Mathf.Abs(u - center.x) + Mathf.Abs(v - center.y));
                case GradientType.Square:
                    return NormalizeDistance(Mathf.Max(Mathf.Abs(u - center.x), Mathf.Abs(v - center.y)));
                default:
                    return 0f;
            }
        }

        internal override void ReleaseTransientResources()
        {
            if (cachedPreview == null)
                return;
            UnityEngine.Object.DestroyImmediate(cachedPreview);
            cachedPreview = null;
        }

        public override string ToString()
        {
            return gradientType.ToString();
        }

        private float NormalizeDistance(float distance)
        {
            return radius <= 0f ? 0f : Mathf.Clamp01(distance / radius);
        }

        private Texture2D GenerateGradientTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = texture.GetRawTextureData<Color>();
            Gradient evaluatedGradient = gradient ?? GradientUtility.WhiteToBlack;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;
                    pixels[y * width + x] = HdrUtility.Decode(evaluatedGradient.Evaluate(GetGradientCoord(u, v)));
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private int ComputeHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + gradientType.GetHashCode();
                hash = hash * 31 + GradientUtility.ComputeHash(gradient);
                hash = hash * 31 + center.GetHashCode();
                hash = hash * 31 + radius.GetHashCode();
                hash = hash * 31 + circularRepetitions.GetHashCode();
                hash = hash * 31 + circularWrapMode.GetHashCode();
                return hash;
            }
        }

        public enum GradientType
        {
            Vertical,
            Horizontal,
            Radial,
            Circular,
            Diamond,
            Square
        }

        public enum WrapMode
        {
            Repeat,
            PingPong
        }
    }
}
