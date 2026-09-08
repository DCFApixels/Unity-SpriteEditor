using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        private enum PreviewTool { None, Brush, Transform, Fill, Zoom }

        [NonSerialized] private PreviewTool previewTool = PreviewTool.Brush;
        [NonSerialized] private Button previewNoneButton;
        [NonSerialized] private Button previewBrushButton;
        [NonSerialized] private Button previewTransformButton;
        [NonSerialized] private Button previewFillButton;
        [NonSerialized] private Button previewZoomButton;

        private bool IsPreviewBrushEnabled => previewTool == PreviewTool.Brush && GetSelectedLayer() is DrawingLayer;
        private bool IsPreviewFillEnabled => previewTool == PreviewTool.Fill && GetSelectedLayer() is DrawingLayer;

        private VisualElement BuildPreviewToolToolbar()
        {
            VisualElement toolbar = new VisualElement { name = "previewTools" };
            toolbar.AddToClassList("sprite-editor-tools");
            toolbar.EnableInClassList("sprite-editor-tools--light", !EditorGUIUtility.isProSkin);
            previewNoneButton = CreatePreviewToolButton("noTool", PreviewTool.None,
                "No Tool (V). View the composition without painting, pattern guides or transform handles.");
            previewBrushButton = CreatePreviewToolButton("brushTool", PreviewTool.Brush,
                "Brush (B). Paint on the selected Drawing layer. Choose Brush/Eraser in the header; RMB temporarily erases.");
            previewFillButton = CreatePreviewToolButton("fillTool", PreviewTool.Fill,
                "Fill (G). Fill similar pixels on the selected Drawing layer, sampling this layer or all visible layers. Contiguous limits the fill to the clicked region.");
            previewTransformButton = CreatePreviewToolButton("transformTool", PreviewTool.Transform,
                "Transform (T). Drag inside to move, handles to scale, circle to rotate. " +
                "Drag the gold cross to move the pivot without moving the image (requires nonzero scale). " +
                "The pivot snaps to frame anchors; hold Ctrl to disable snapping. " +
                "Shift: constrain movement / preserve proportions / snap rotation to 15°. Groups are not supported yet.");
            toolbar.Add(previewNoneButton);
            toolbar.Add(previewTransformButton);
            toolbar.Add(previewBrushButton);
            toolbar.Add(previewFillButton);
            previewZoomButton = CreatePreviewToolButton("zoomTool", PreviewTool.Zoom,
                "Zoom (Z). Click to zoom in, drag a rectangle to frame an area, or Alt-click to zoom out. MMB-drag pans the preview.");
            toolbar.Add(previewZoomButton);
            return toolbar;
        }

        private Button CreatePreviewToolButton(string name, PreviewTool tool, string tooltip)
        {
            Button button = new Button(() => SetPreviewTool(tool)) { name = name, tooltip = tooltip };
            button.AddToClassList("sprite-editor-tool-button");
            button.Add(new PreviewToolIcon(tool));
            return button;
        }

        private void RefreshPreviewToolToolbar()
        {
            Layer selected = GetSelectedLayer();
            previewZoomButton?.SetEnabled(compositor != null);
            previewZoomButton?.EnableInClassList("sprite-editor-tool-button--selected", previewTool == PreviewTool.Zoom);
            previewNoneButton?.EnableInClassList("sprite-editor-tool-button--selected", previewTool == PreviewTool.None);
            if (previewBrushButton != null)
            {
                previewBrushButton.SetEnabled(selected is DrawingLayer);
                previewBrushButton.EnableInClassList("sprite-editor-tool-button--selected", previewTool == PreviewTool.Brush);
            }
            if (previewTransformButton != null)
            {
                previewTransformButton.SetEnabled(selected != null && !selected.IsGroup);
                previewTransformButton.EnableInClassList("sprite-editor-tool-button--selected", previewTool == PreviewTool.Transform);
            }
            if (previewFillButton != null)
            {
                previewFillButton.SetEnabled(selected is DrawingLayer);
                previewFillButton.EnableInClassList("sprite-editor-tool-button--selected", previewTool == PreviewTool.Fill);
            }
        }

        private sealed class PreviewToolIcon : VisualElement
        {
            private readonly PreviewTool tool;

            internal PreviewToolIcon(PreviewTool tool)
            {
                this.tool = tool;
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
                if (tool == PreviewTool.None)
                    DrawPointer(painter);
                else if (tool == PreviewTool.Transform)
                    DrawHand(painter);
                else if (tool == PreviewTool.Fill)
                    DrawBucket(painter);
                else if (tool == PreviewTool.Zoom)
                    DrawMagnifier(painter);
                else
                    DrawBrush(painter);
            }

            private Vector2 P(float x, float y) => new Vector2(
                contentRect.x + x * contentRect.width / 24f,
                contentRect.y + y * contentRect.height / 24f);

            private void DrawMagnifier(Painter2D painter)
            {
                painter.BeginPath();
                painter.Arc(P(9.5f, 9.5f), contentRect.width * 6.5f / 24f, 0f, 360f);
                painter.ClosePath();
                painter.Stroke();
                painter.lineWidth = 3f;
                painter.BeginPath();
                painter.MoveTo(P(14.5f, 14.5f));
                painter.LineTo(P(21f, 21f));
                painter.Stroke();
            }

            private void DrawBucket(Painter2D painter)
            {
                Color ink = resolvedStyle.color;
                painter.fillColor = new Color(ink.r, ink.g, ink.b, ink.a * 0.2f);
                painter.BeginPath();
                painter.MoveTo(P(4f, 11f));
                painter.LineTo(P(11f, 4f));
                painter.LineTo(P(18f, 11f));
                painter.LineTo(P(11f, 18f));
                painter.ClosePath();
                painter.Fill();
                painter.Stroke();
                painter.BeginPath();
                painter.MoveTo(P(4f, 11f));
                painter.LineTo(P(18f, 11f));
                painter.MoveTo(P(11f, 8f));
                painter.LineTo(P(7.5f, 3f));
                painter.BezierCurveTo(P(5f, 0f), P(2f, 3f), P(4f, 6f));
                painter.Stroke();
                painter.fillColor = ink;
                painter.BeginPath();
                painter.MoveTo(P(19f, 13f));
                painter.BezierCurveTo(P(18f, 15f), P(16.5f, 17f), P(17f, 18.5f));
                painter.BezierCurveTo(P(18f, 21f), P(22f, 19.5f), P(21f, 17.5f));
                painter.ClosePath();
                painter.Fill();
            }

            private void DrawPointer(Painter2D painter)
            {
                Color ink = resolvedStyle.color;
                painter.fillColor = new Color(ink.r, ink.g, ink.b, ink.a * 0.2f);
                painter.BeginPath();
                painter.MoveTo(P(5f, 2.5f));
                painter.LineTo(P(19f, 12.5f));
                painter.LineTo(P(12.8f, 13.5f));
                painter.LineTo(P(16.3f, 20.1f));
                painter.LineTo(P(12.8f, 21.8f));
                painter.LineTo(P(9.5f, 15.2f));
                painter.LineTo(P(5f, 19f));
                painter.ClosePath();
                painter.Fill();
                painter.Stroke();
            }

            private void DrawBrush(Painter2D painter)
            {
                Color ink = resolvedStyle.color;
                painter.fillColor = new Color(ink.r, ink.g, ink.b, ink.a * 0.16f);
                painter.BeginPath();
                painter.MoveTo(P(9.8f, 10.6f));
                painter.BezierCurveTo(P(13.5f, 8.5f), P(17.6f, 4.1f), P(19.9f, 2.6f));
                painter.BezierCurveTo(P(21.4f, 1.6f), P(22.3f, 2.8f), P(21.3f, 4.2f));
                painter.BezierCurveTo(P(19.5f, 6.7f), P(15.8f, 11.1f), P(13.8f, 14.6f));
                painter.ClosePath();
                painter.Fill();
                painter.Stroke();
                painter.fillColor = new Color(ink.r, ink.g, ink.b, ink.a * 0.45f);
                painter.BeginPath();
                painter.MoveTo(P(9.8f, 10.6f));
                painter.LineTo(P(7.8f, 12.6f));
                painter.LineTo(P(11.8f, 16.6f));
                painter.LineTo(P(13.8f, 14.6f));
                painter.ClosePath();
                painter.Fill();
                painter.Stroke();

                painter.fillColor = ink;
                painter.BeginPath();
                painter.MoveTo(P(7.8f, 12.6f));
                painter.BezierCurveTo(P(4.4f, 12f), P(3.9f, 15.4f), P(4.2f, 17.1f));
                painter.BezierCurveTo(P(4.5f, 19.2f), P(2.8f, 20.7f), P(1.8f, 21.4f));
                painter.BezierCurveTo(P(6.4f, 22.6f), P(11.7f, 20.9f), P(12.3f, 18.4f));
                painter.BezierCurveTo(P(12.5f, 17.6f), P(12.2f, 17f), P(11.8f, 16.6f));
                painter.ClosePath();
                painter.MoveTo(P(7.1f, 14.6f));
                painter.BezierCurveTo(P(5.9f, 16f), P(7f, 17.3f), P(5.1f, 19.6f));
                painter.BezierCurveTo(P(8f, 18.3f), P(7.2f, 16.6f), P(7.1f, 14.6f));
                painter.ClosePath();
                painter.Fill(FillRule.OddEven);
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
