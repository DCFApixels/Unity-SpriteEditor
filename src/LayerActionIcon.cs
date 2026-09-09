using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    internal sealed class LayerActionIcon : VisualElement
    {
        internal enum Kind { Add, Group, Delete, Bug, Eye, EyeOff, Alpha }

        private readonly Kind kind;

        internal LayerActionIcon(Kind kind)
        {
            this.kind = kind;
            pickingMode = PickingMode.Ignore;
            AddToClassList("sprite-editor-layer-action-icon");
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            if (contentRect.width < 1f || contentRect.height < 1f)
                return;

            Painter2D painter = context.painter2D;
            painter.strokeColor = resolvedStyle.color;
            painter.lineWidth = 1.5f;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            switch (kind)
            {
                case Kind.Alpha:
                    DrawAlpha(painter);
                    return;
                case Kind.Eye:
                case Kind.EyeOff:
                    DrawEye(painter, kind == Kind.EyeOff);
                    return;
                case Kind.Bug:
                    DrawBug(painter);
                    return;
                case Kind.Add:
                    painter.MoveTo(new Vector2(8f, 3f));
                    painter.LineTo(new Vector2(8f, 13f));
                    painter.MoveTo(new Vector2(3f, 8f));
                    painter.LineTo(new Vector2(13f, 8f));
                    break;
                case Kind.Group:
                    painter.MoveTo(new Vector2(2f, 13f));
                    painter.LineTo(new Vector2(2f, 3f));
                    painter.LineTo(new Vector2(6f, 3f));
                    painter.LineTo(new Vector2(8f, 5f));
                    painter.LineTo(new Vector2(14f, 5f));
                    painter.LineTo(new Vector2(14f, 13f));
                    painter.ClosePath();
                    break;
                case Kind.Delete:
                    painter.MoveTo(new Vector2(3f, 4f));
                    painter.LineTo(new Vector2(13f, 4f));
                    painter.MoveTo(new Vector2(6f, 4f));
                    painter.LineTo(new Vector2(6f, 2f));
                    painter.LineTo(new Vector2(10f, 2f));
                    painter.LineTo(new Vector2(10f, 4f));
                    painter.MoveTo(new Vector2(4f, 6f));
                    painter.LineTo(new Vector2(5f, 14f));
                    painter.LineTo(new Vector2(11f, 14f));
                    painter.LineTo(new Vector2(12f, 6f));
                    painter.MoveTo(new Vector2(7f, 7f));
                    painter.LineTo(new Vector2(7f, 11f));
                    painter.MoveTo(new Vector2(9f, 7f));
                    painter.LineTo(new Vector2(9f, 11f));
                    break;
            }
            painter.Stroke();
        }

        private static void DrawAlpha(Painter2D painter)
        {
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(2.5f, 2.5f));
            painter.LineTo(new Vector2(13.5f, 2.5f));
            painter.LineTo(new Vector2(13.5f, 13.5f));
            painter.LineTo(new Vector2(2.5f, 13.5f));
            painter.ClosePath();
            painter.Stroke();
            painter.fillColor = painter.strokeColor;
            for (int i = 0; i < 2; i++)
            {
                float start = 3.5f + i * 4.5f;
                float end = start + 4.5f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(start, start));
                painter.LineTo(new Vector2(end, start));
                painter.LineTo(new Vector2(end, end));
                painter.LineTo(new Vector2(start, end));
                painter.ClosePath();
                painter.Fill();
            }
        }

        private static void DrawEye(Painter2D painter, bool hidden)
        {
            painter.lineWidth = 1.25f;
            painter.BeginPath();
            if (hidden)
            {
                painter.MoveTo(new Vector2(6.2f, 4.1f));
                painter.BezierCurveTo(new Vector2(9.8f, 3.1f), new Vector2(12.7f, 5.2f), new Vector2(14.5f, 8f));
                painter.BezierCurveTo(new Vector2(13.6f, 9.2f), new Vector2(12.6f, 10.2f), new Vector2(11.4f, 10.9f));
                painter.MoveTo(new Vector2(9.6f, 11.8f));
                painter.BezierCurveTo(new Vector2(6.1f, 12.7f), new Vector2(3.2f, 10.4f), new Vector2(1.5f, 8f));
                painter.BezierCurveTo(new Vector2(2.3f, 6.8f), new Vector2(3.2f, 5.8f), new Vector2(4.4f, 5.1f));
                painter.MoveTo(new Vector2(2.2f, 2.2f));
                painter.LineTo(new Vector2(13.8f, 13.8f));
                painter.Stroke();
            }
            else
            {
                painter.MoveTo(new Vector2(1.5f, 8f));
                painter.BezierCurveTo(new Vector2(5f, 2.7f), new Vector2(11f, 2.7f), new Vector2(14.5f, 8f));
                painter.BezierCurveTo(new Vector2(11f, 13.3f), new Vector2(5f, 13.3f), new Vector2(1.5f, 8f));
                painter.ClosePath();
                painter.Stroke();
            }
            if (hidden)
            {
                painter.lineCap = LineCap.Butt;
                painter.BeginPath();
                painter.Arc(new Vector2(8f, 8f), 2f, 225f, 405f);
                painter.Stroke();
                painter.BeginPath();
                painter.Arc(new Vector2(8f, 8f), 2f, 100f, 170f);
                painter.Stroke();
                return;
            }
            painter.BeginPath();
            painter.Arc(new Vector2(8f, 8f), 2f, 0f, 360f);
            painter.ClosePath();
            painter.Stroke();
        }

        private void DrawBug(Painter2D painter)
        {
            painter.lineWidth = 1.25f;
            painter.fillColor = resolvedStyle.color;
            for (int side = 0; side < 2; side++)
            {
                Vector2 P(float x, float y) => new Vector2(side == 0 ? x : 16f - x, y);
                painter.BeginPath();
                painter.MoveTo(P(6.7f, 3.3f));
                painter.LineTo(P(5.4f, 1.4f));
                painter.MoveTo(P(5.2f, 7.5f));
                painter.LineTo(P(3.2f, 6f));
                painter.LineTo(P(2.9f, 4.5f));
                painter.MoveTo(P(4.8f, 9.5f));
                painter.LineTo(P(1.6f, 9.5f));
                painter.MoveTo(P(5.2f, 11.5f));
                painter.LineTo(P(3.2f, 12.8f));
                painter.LineTo(P(2.9f, 14.1f));
                painter.Stroke();

                painter.BeginPath();
                painter.MoveTo(P(7.4f, 6.3f));
                painter.BezierCurveTo(P(5.5f, 6.1f), P(4.4f, 7.7f), P(4.4f, 9.6f));
                painter.BezierCurveTo(P(4.4f, 12.1f), P(5.7f, 14f), P(7.4f, 14.3f));
                painter.ClosePath();
                painter.Fill();
            }

            painter.BeginPath();
            painter.MoveTo(new Vector2(5.8f, 5.2f));
            painter.BezierCurveTo(new Vector2(5.8f, 1.9f), new Vector2(10.2f, 1.9f), new Vector2(10.2f, 5.2f));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
