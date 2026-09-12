using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    using UnityEngine.Experimental.Rendering;

    [System.Serializable]
    public sealed class FileLayerBehaviour : LayerBehaviour
    {
        public Texture2D sourceTexture;
        [SerializeField] private bool sourceAssigned;

        internal void AssignSourceTexture(Texture2D texture, TextureCompositor owner, bool initializeCanvas = false)
        {
            bool wasEmpty = sourceTexture == null;
            bool changed = sourceTexture != texture;
            if (initializeCanvas && wasEmpty && !sourceAssigned && texture != null && CanInitializeCanvas(owner))
            {
                owner.width = Mathf.Max(1, texture.width);
                owner.height = Mathf.Max(1, texture.height);
            }
            sourceAssigned |= sourceTexture != null || texture != null;
            sourceTexture = texture;
            if (changed && texture != null && GraphicsFormatUtility.IsHDRFormat(texture.graphicsFormat))
            {
                colorRange = LayerColorRange.HDR;
                blendRange = LayerBlendRange.HDR;
            }
            if (wasEmpty && texture != null && TryGetOriginalAspectTransform(owner, out TextureTransform fitted))
                transform = fitted;
        }

        private bool CanInitializeCanvas(TextureCompositor owner)
        {
            if (owner == null) return false;
            if (owner.layers != null)
                foreach (Layer layer in owner.layers)
                    if (layer != null && !ReferenceEquals(layer, Owner))
                        return false;
            return true;
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
