using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [System.Serializable]
    public sealed class FileLayer : Layer
    {
        public Texture2D sourceTexture;

        internal void AssignSourceTexture(Texture2D texture, TextureCompositor owner)
        {
            bool wasEmpty = sourceTexture == null;
            sourceTexture = texture;
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
