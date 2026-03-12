using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public class ColorFillLayer : Layer
    {
        public override RenderTexture GetRenderTexture(TextureCompositor compositor, int layerIndex, int width, int height, float scaleMultiplier = 1)
        {
            return null;
        }
    }
}
