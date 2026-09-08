using System;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [Serializable]
    public sealed class ColorFillLayer : Layer
    {
        public Color color = Color.white;

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
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            source.filterMode = FilterMode.Bilinear;
            source.wrapMode = TextureWrapMode.Clamp;
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                GL.Clear(true, true, color);
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
