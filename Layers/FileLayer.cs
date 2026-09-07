using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [System.Serializable]
    public sealed class FileLayer : Layer
    {
        public Texture2D sourceTexture;

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
