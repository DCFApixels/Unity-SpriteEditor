using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        [NonSerialized] private Button previewBrushButton;
        [NonSerialized] private Button previewTransformButton;

        private VisualElement BuildPreviewToolToolbar()
        {
            VisualElement toolbar = new VisualElement { name = "previewTools" };
            toolbar.AddToClassList("sprite-editor-tools");
            toolbar.EnableInClassList("sprite-editor-tools--light", !EditorGUIUtility.isProSkin);
            previewBrushButton = CreatePreviewToolButton("brushTool", false,
                "Brush (B). Paint on the selected Drawing layer. Choose Brush/Eraser in the header; RMB temporarily erases.");
            previewTransformButton = CreatePreviewToolButton("transformTool", true,
                "Transform (T). Drag inside to move, handles to scale, circle to rotate. " +
                "Drag the gold cross to move the pivot without moving the image (requires nonzero scale). " +
                "The pivot snaps to frame anchors; hold Ctrl to disable snapping. " +
                "Shift: constrain movement / preserve proportions / snap rotation to 15°. Groups are not supported yet.");
            toolbar.Add(previewBrushButton);
            toolbar.Add(previewTransformButton);
            return toolbar;
        }

        private Button CreatePreviewToolButton(string name, bool transform, string tooltip)
        {
            Button button = new Button(() => SetPreviewTool(transform)) { name = name, tooltip = tooltip };
            button.AddToClassList("sprite-editor-tool-button");
            button.Add(new PreviewToolIcon(transform));
            return button;
        }

        private void RefreshPreviewToolToolbar()
        {
            Layer selected = GetSelectedLayer();
            if (previewBrushButton != null)
            {
                previewBrushButton.SetEnabled(selected is DrawingLayer);
                previewBrushButton.EnableInClassList("sprite-editor-tool-button--selected", !previewTransformActive);
            }
            if (previewTransformButton != null)
            {
                previewTransformButton.SetEnabled(selected != null && !selected.IsGroup);
                previewTransformButton.EnableInClassList("sprite-editor-tool-button--selected", previewTransformActive);
            }
        }

        private sealed class PreviewToolIcon : VisualElement
        {
            private readonly bool transformTool;

            internal PreviewToolIcon(bool transformTool)
            {
                this.transformTool = transformTool;
                pickingMode = PickingMode.Ignore;
                AddToClassList("sprite-editor-tool-icon");
                generateVisualContent += Draw;
            }

            private void Draw(MeshGenerationContext context)
            {
                if (contentRect.width < 1f || contentRect.height < 1f)
                    return;
                Painter2D painter = context.painter2D;
                painter.strokeColor = resolvedStyle.color;
                painter.fillColor = resolvedStyle.color;
                painter.lineWidth = 1.35f;
                painter.lineCap = LineCap.Round;
                painter.lineJoin = LineJoin.Round;
                if (transformTool)
                    DrawHand(painter);
                else
                    DrawBrush(painter);
            }

            private Vector2 P(float x, float y) => new Vector2(
                contentRect.x + x * contentRect.width / 24f,
                contentRect.y + y * contentRect.height / 24f);

            private void DrawBrush(Painter2D painter)
            {
                Color ink = resolvedStyle.color;
                painter.fillColor = new Color(ink.r, ink.g, ink.b, ink.a * 0.22f);
                painter.BeginPath();
                painter.MoveTo(P(8.3f, 10.5f));
                painter.LineTo(P(19.3f, 2.9f));
                painter.BezierCurveTo(P(20.9f, 1.8f), P(22.2f, 3.1f), P(21.1f, 4.7f));
                painter.LineTo(P(13.5f, 15.7f));
                painter.ClosePath();
                painter.Fill();
                painter.Stroke();
                painter.BeginPath();
                painter.MoveTo(P(8.3f, 10.5f));
                painter.LineTo(P(6.3f, 12.5f));
                painter.LineTo(P(11.5f, 17.7f));
                painter.LineTo(P(13.5f, 15.7f));
                painter.ClosePath();
                painter.Stroke();

                painter.fillColor = ink;
                painter.BeginPath();
                painter.MoveTo(P(6.3f, 12.5f));
                painter.BezierCurveTo(P(2.5f, 12.8f), P(4.9f, 18.3f), P(1.8f, 21.8f));
                painter.BezierCurveTo(P(6.5f, 22.2f), P(13.4f, 21.6f), P(11.5f, 17.7f));
                painter.ClosePath();
                painter.Fill();
            }

            private void DrawHand(Painter2D painter)
            {
                painter.BeginPath();
                painter.MoveTo(P(7f, 12.2f));
                painter.LineTo(P(7f, 6f));
                painter.BezierCurveTo(P(7f, 3.8f), P(10f, 3.8f), P(10f, 6f));
                painter.LineTo(P(10f, 11f));
                painter.LineTo(P(10f, 3.8f));
                painter.BezierCurveTo(P(10f, 1.6f), P(13f, 1.6f), P(13f, 3.8f));
                painter.LineTo(P(13f, 11f));
                painter.LineTo(P(13f, 5f));
                painter.BezierCurveTo(P(13f, 2.8f), P(16f, 2.8f), P(16f, 5f));
                painter.LineTo(P(16f, 11.8f));
                painter.LineTo(P(16f, 8f));
                painter.BezierCurveTo(P(16f, 5.8f), P(19f, 5.8f), P(19f, 8f));
                painter.LineTo(P(19f, 14f));
                painter.BezierCurveTo(P(19f, 18f), P(17f, 18.5f), P(17f, 21f));
                painter.LineTo(P(9f, 21f));
                painter.BezierCurveTo(P(9f, 18.7f), P(7f, 18f), P(5.8f, 16f));
                painter.LineTo(P(3.5f, 12.5f));
                painter.BezierCurveTo(P(2f, 10f), P(4.3f, 9.2f), P(5.8f, 11f));
                painter.LineTo(P(7f, 12.2f));
                painter.ClosePath();
                painter.Stroke();
            }
        }
    }
}
