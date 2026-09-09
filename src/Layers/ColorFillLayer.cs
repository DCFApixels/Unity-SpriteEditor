using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace DCFApixels.SpriteEditor
{
    [Serializable]
    public sealed class ColorFillLayer : Layer
    {
        [SerializeField, FormerlySerializedAs("color")] private Color storedColor = Color.white;
        [SerializeField, HideInInspector] private bool colorIsEncoded;

        public Color color
        {
            get => !colorIsEncoded && QualitySettings.activeColorSpace == ColorSpace.Linear
                ? HdrUtility.Encode(storedColor) : storedColor;
            set { storedColor = value; colorIsEncoded = true; }
        }

        private Color LinearColor => !colorIsEncoded && QualitySettings.activeColorSpace == ColorSpace.Linear
            ? storedColor : HdrUtility.Decode(storedColor);

        [NonSerialized] private Texture2D cachedPreview;
        [NonSerialized] private Color cachedColor;

        public override Texture2D GetPreviewTexture(int size)
        {
            if (cachedPreview != null && cachedColor == color)
                return cachedPreview;

            ReleaseTransientResources();
            cachedPreview = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            cachedPreview.SetPixel(0, 0, color);
            cachedPreview.Apply(false, false);
            cachedColor = color;
            return cachedPreview;
        }

        internal override RenderTexture Render(in LayerRenderContext context)
        {
            RenderTexture source = RenderTexture.GetTemporary(
                context.width,
                context.height,
                0,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            source.filterMode = FilterMode.Bilinear;
            source.wrapMode = TextureWrapMode.Clamp;
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                GL.Clear(true, true, LinearColor);
                return ApplyTransformAndModifiers(source, context);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(source);
            }
        }

        internal override void ReleaseTransientResources()
        {
            if (cachedPreview == null)
                return;
            UnityEngine.Object.DestroyImmediate(cachedPreview);
            cachedPreview = null;
        }
    }
}
