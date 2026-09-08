using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    internal sealed class LayerActionIcon : VisualElement
    {
        internal enum Kind { Add, Group, Delete }

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
    }
}
