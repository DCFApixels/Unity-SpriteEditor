using System;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [Serializable]
    public sealed class ShaderProcessorLayer : Layer
    {
        public ShaderProcessorLayer()
        {
            colorRange = LayerColorRange.HDR;
            blendRange = LayerBlendRange.HDR;
        }

        public override string ToString() => "Shader Processor";
        internal override RenderTexture Render(in LayerRenderContext context) =>
            ApplyTransformAndModifiers(context.input, context);
    }
}
