using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [Serializable]
    public sealed class GroupLayer : Layer
    {
        [SerializeReference] public List<Layer> layers = new List<Layer>();

        internal override bool IsGroup => true;

        internal override RenderTexture Render(in LayerRenderContext context)
        {
            // Groups are structural nodes. TextureCompositor flattens their children
            // into the parent stack so blend modes behave as if no isolated group exists.
            return null;
        }
    }
}
