using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    using UnityEngine.Experimental.Rendering;

    [System.Serializable]
    public sealed class FileLayer : Layer
    {
        public Texture2D sourceTexture;

        internal void AssignSourceTexture(Texture2D texture, TextureCompositor owner)
        {
            bool wasEmpty = sourceTexture == null;
            bool changed = sourceTexture != texture;
            sourceTexture = texture;
            if (changed && texture != null && GraphicsFormatUtility.IsHDRFormat(texture.graphicsFormat))
            {
                colorRange = LayerColorRange.HDR;
                blendRange = LayerBlendRange.HDR;
            }
            if (wasEmpty && texture != null && TryGetOriginalAspectTransform(owner, out TextureTransform fitted))
                transform = fitted;
        }

        public override Texture2D GetPreviewTexture(int size)
        {
            return sourceTexture;
        }

        internal override RenderTexture Render(in LayerRenderContext context)
        {
            return sourceTexture == null ? null : ApplyTransformAndModifiers(sourceTexture, context);
        }
    }
}
