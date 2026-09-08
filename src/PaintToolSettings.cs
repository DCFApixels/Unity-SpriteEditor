using System;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [Serializable]
    internal sealed class PaintToolSettings
    {
        public PaintToolMode tool = PaintToolMode.Brush;
        public Color brushColor = Color.white;
        public Color secondaryBrushColor = Color.black;
        public float brushSize = 32f;
        public float brushHardness = 0.8f;
        public float brushSpacing = 0.16f;
        public FillSampleMode fillSampleMode = FillSampleMode.CurrentLayer;
        public bool fillContiguous = true;
        public int fillTolerance = 32;
        public bool fillAntialias = true;
        public int fillExpand;

        internal PaintStrokeParameters GetStrokeParameters(bool erase, Color? colorOverride = null)
        {
            return new PaintStrokeParameters(colorOverride ?? brushColor, brushSize, brushHardness, brushSpacing, erase);
        }

        internal void SwapBrushColors()
        {
            Color previous = brushColor;
            brushColor = secondaryBrushColor;
            secondaryBrushColor = previous;
        }
    }
}
