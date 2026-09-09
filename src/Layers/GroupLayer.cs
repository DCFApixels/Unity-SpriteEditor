using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [Serializable]
    public sealed class GroupLayer : Layer
    {
        [SerializeReference] public List<Layer> layers = new List<Layer>();
        // Zero preserves the behavior of documents saved before group blending existed.
        public GroupCompositing compositing;

        internal override bool IsGroup => true;
        internal bool IsPassThrough => compositing == GroupCompositing.PassThrough && swizzle.IsIdentity;
        internal BlendMode EffectiveBlendMode => compositing == GroupCompositing.PassThrough ? BlendMode.Normal : blendMode;

        internal override RenderTexture Render(in LayerRenderContext context)
        {
            // The compositor owns both pass-through and isolated group evaluation.
            return null;
        }
    }
    public enum GroupCompositing { PassThrough, Isolated }
}
