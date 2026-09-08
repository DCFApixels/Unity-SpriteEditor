using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    internal readonly struct PaintStrokeParameters
    {
        internal readonly Color Color;
        internal readonly float Size;
        internal readonly float Hardness;
        internal readonly float SpacingPixels;
        internal readonly bool Erase;
        internal readonly bool WrapCanvas;

        internal PaintStrokeParameters WithCanvasWrap() => new PaintStrokeParameters(this);

        private PaintStrokeParameters(PaintStrokeParameters source)
        {
            Color = source.Color;
            Size = source.Size;
            Hardness = source.Hardness;
            SpacingPixels = source.SpacingPixels;
            Erase = source.Erase;
            WrapCanvas = true;
        }

        internal PaintStrokeParameters(Color color, float size, float hardness, float spacing, bool erase)
        {
            Color = color;
            Size = Mathf.Max(1f, size);
            Hardness = Mathf.Clamp01(hardness);
            SpacingPixels = Mathf.Max(1f, Size * Mathf.Clamp(spacing,
                DrawingLayer.MinimumBrushSpacing, DrawingLayer.MaximumBrushSpacing));
            Erase = erase;
            WrapCanvas = false;
        }
    }
}
