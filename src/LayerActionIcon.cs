using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    internal sealed class LayerActionIcon : VisualElement
    {
        internal enum Kind { Add, Group, Delete, Bug }

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
