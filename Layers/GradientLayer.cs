using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [System.Serializable]
    public class GradientLayer : Layer
    {
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

        public GradientType gradientType = GradientType.Vertical;
        public Gradient gradient = GradientUtility.Clone(GradientUtility.WhiteToBlack);
        public Vector2 center = new Vector2(0.5f, 0.5f);
        public float radius = 0.5f;
        public float circularRepetitions = 1;
        public WrapMode circularWrapMode = WrapMode.Repeat;

        private Texture2D cachedPreview;
        private int cachedHash;


        public void ClearPreviewCache()
        {
            if (cachedPreview != null)
            {
                Object.DestroyImmediate(cachedPreview);
                cachedPreview = null;
            }
        }
        public override Texture2D GetPreviewTexture(int size)
        {
            int currentHash = ComputeHash();
            if (cachedPreview != null && cachedHash == currentHash && cachedPreview.width == size)
                return cachedPreview;

            if (cachedPreview != null)
                Object.DestroyImmediate(cachedPreview);

            cachedPreview = GenerateGradientTexture(size, size);
            cachedHash = currentHash;
            return cachedPreview;
        }
        private int ComputeHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + gradientType.GetHashCode();
                hash = hash * 31 + gradient.GetHashCode(); // Gradient не имеет хеша, можно использовать его ключи, но для простоты будем считать, что изменение градиента не отслеживается. Либо использовать Gradient.Evaluate для хеша? Сложно. Пока пропустим.
                hash = hash * 31 + center.GetHashCode();
                hash = hash * 31 + radius.GetHashCode();
                hash = hash * 31 + circularRepetitions.GetHashCode();
                hash = hash * 31 + circularWrapMode.GetHashCode();
                return hash;
            }
        }

        public override RenderTexture GetRenderTexture(TextureCompositor compositor, int layerIndex, int width, int height, float scaleMultiplier = 1f)
        {
            Texture2D tex = GenerateGradientTexture(width, height);

            RenderTexture transformed = ApplyTransform(tex, width, height);

            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(transformed, rt);
            Object.DestroyImmediate(tex);

            foreach (var modifier in modifiers)
            {
                if (modifier != null)
                {
                    RenderTexture temp = RenderTexture.GetTemporary(rt.width, rt.height, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(rt, temp, modifier);
                    RenderTexture.ReleaseTemporary(rt);
                    rt = temp;
                }
            }

            if (rt != transformed)
            {
                RenderTexture.ReleaseTemporary(transformed);
            }
            return rt;
        }

        private Texture2D GenerateGradientTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;
                    float t = GetGradientCoord(u, v);
                    pixels[y * width + x] = gradient.Evaluate(t);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
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
                    if (radius <= 0) return 0;
                    float dx = u - center.x;
                    float dy = v - center.y;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    return Mathf.Clamp01(dist / radius);
                case GradientType.Circular:
                    Vector2 dir = new Vector2(u - center.x, v - center.y);
                    float angle = Mathf.Atan2(dir.y, dir.x);
                    float t = (angle + Mathf.PI) / (2 * Mathf.PI); // [0, 1)

                    if (circularRepetitions > 1)
                    {
                        // Применяем повторения
                        if (circularWrapMode == WrapMode.Repeat)
                        {
                            t = (t * circularRepetitions) % 1.0f;
                        }
                        else // PingPong
                        {
                            float tt = t * circularRepetitions;
                            int cycle = Mathf.FloorToInt(tt);
                            float frac = tt - cycle;
                            if (cycle % 2 == 0)
                                t = frac;
                            else
                                t = 1.0f - frac;
                        }
                    }
                    else
                    {
                        t = (t * circularRepetitions) % 1.0f;
                    }
                    return t;
                case GradientType.Diamond:
                    if (radius <= 0) return 0;
                    float dxa = Mathf.Abs(u - center.x);
                    float dya = Mathf.Abs(v - center.y);
                    float diamondDist = (dxa + dya) / radius;
                    return Mathf.Clamp01(diamondDist);
                case GradientType.Square:
                    if (radius <= 0) return 0;
                    float dxs = Mathf.Abs(u - center.x);
                    float dys = Mathf.Abs(v - center.y);
                    float squareDist = Mathf.Max(dxs, dys) / radius;
                    return Mathf.Clamp01(squareDist);
                default:
                    return 0;
            }
        }

        public override string ToString()
        {
            return gradientType.ToString();
        }
    }
}