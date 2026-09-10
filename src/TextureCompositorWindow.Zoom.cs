using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        [NonSerialized] private PreviewViewport previewViewport = new PreviewViewport();
        private PreviewZoomManipulator previewZoomManipulator;
        private Label previewZoomPercent;
        private float displayedPreviewScale = float.NaN;
        private bool IsPreviewZoomEnabled => previewTool == PreviewTool.Zoom && compositor != null;

        private void BuildPreviewZoomTool()
        {
            previewZoomManipulator = new PreviewZoomManipulator(this);
            toolkitPreviewCanvas.AddManipulator(previewZoomManipulator);
            toolkitPreviewCanvas.ViewChanged += () =>
            {
                RefreshPreviewZoomReadout();
                RefreshPreviewTransformTool();
                RefreshPreviewPointerCursor();
            };
        }

        private void AddPreviewZoomSettings()
        {
            VisualElement row = SpriteEditorUI.CreateToolbar();
            row.AddToClassList("sprite-editor-zoom-settings");
            BindPreviewSettingsRow(row, PreviewTool.Zoom);
            previewZoomPercent = new Label();
            displayedPreviewScale = float.NaN;
            previewZoomPercent.AddToClassList("sprite-editor-zoom-percent");
            toolkitHeaderBindings.Add(RefreshPreviewZoomReadout);
            row.Add(previewZoomPercent);
            row.Add(SpriteEditorUI.CreateButton("Fit", () => ChangePreviewZoom(true)));
            row.Add(SpriteEditorUI.CreateButton("100%", () => ChangePreviewZoom(false)));
            toolkitPreviewHeader.Add(row);
        }

        private void RefreshPreviewZoomReadout()
        {
            if (previewZoomPercent == null || toolkitPreviewCanvas == null)
                return;
            float scale = toolkitPreviewCanvas.PixelScale;
            if (scale == displayedPreviewScale)
                return;
            displayedPreviewScale = scale;
            previewZoomPercent.text = $"{scale * 100f:0.##}%";
        }

        private void ChangePreviewZoom(bool fit)
        {
            CancelPreviewZoomGesture();
            FinishPreviewTransform();
            FinishPaintingStroke();
            if (fit) previewViewport.Reset();
            else toolkitPreviewCanvas.ZoomAt(toolkitPreviewCanvas.contentRect.center, 1f);
            toolkitPreviewCanvas.UpdateImageLayout();
            toolkitPreviewCanvas.Focus();
        }

        private void CancelPreviewZoomGesture() => previewZoomManipulator?.Cancel();

        private sealed class PreviewZoomManipulator : PointerManipulator
        {
            private readonly TextureCompositorWindow owner;
            private int pointerId = -1;
            private bool panning, zoomOut;
            private Vector2 start, current;
            private readonly VisualElement selection;
            internal bool IsDragging => pointerId >= 0;
            internal bool IsPanning => IsDragging && panning;

            internal PreviewZoomManipulator(TextureCompositorWindow owner)
            {
                this.owner = owner;
                selection = new VisualElement { pickingMode = PickingMode.Ignore };
                selection.AddToClassList("sprite-editor-zoom-selection");
                selection.generateVisualContent += DrawSelection;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.Add(selection);
                target.RegisterCallback<PointerDownEvent>(OnDown);
                target.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
                target.RegisterCallback<PointerMoveEvent>(OnMove);
                target.RegisterCallback<PointerUpEvent>(OnUp);
                target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
                target.RegisterCallback<PointerCancelEvent>(OnCancel);
                target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
                target.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                Cancel();
                target.UnregisterCallback<PointerDownEvent>(OnDown);
                target.UnregisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
                target.UnregisterCallback<PointerMoveEvent>(OnMove);
                target.UnregisterCallback<PointerUpEvent>(OnUp);
                target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
                target.UnregisterCallback<PointerCancelEvent>(OnCancel);
                target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
                target.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
                selection.RemoveFromHierarchy();
            }

            internal void Cancel()
            {
                int captured = pointerId;
                pointerId = -1;
                if (captured >= 0 && target.HasPointerCapture(captured)) target.ReleasePointer(captured);
                selection.MarkDirtyRepaint();
                if (captured >= 0) owner.RefreshPreviewPointerCursor();
            }

            private void OnCaptureOut(PointerCaptureOutEvent evt) { if (evt.pointerId == pointerId) Cancel(); }
            private void OnCancel(PointerCancelEvent evt) { if (evt.pointerId == pointerId) Cancel(); }
            private void OnDetach(DetachFromPanelEvent evt)
            {
                owner.ClearPreviewPointerCursor();
                Cancel();
            }
            private void OnGeometry(GeometryChangedEvent evt) => Cancel();

            private void OnDown(PointerDownEvent evt)
            {
                if (IsDragging)
                {
                    SpriteEditorUI.ConsumeEvent(evt);
                    return;
                }
                if (owner.compositor == null || (evt.button != 2 && !(evt.button == 0 && owner.IsPreviewZoomEnabled)) ||
                    !target.contentRect.Contains(evt.localPosition)) return;
                owner.CancelPreviewEyedropper();
                owner.FinishPreviewTransform();
                owner.FinishPaintingStroke();
                owner.Focus();
                target.Focus();
                start = current = evt.localPosition;
                panning = evt.button == 2;
                zoomOut = evt.altKey;
                pointerId = evt.pointerId;
                target.CapturePointer(pointerId);
                owner.UpdatePreviewCursor(evt.localPosition, evt.altKey);
                SpriteEditorUI.ConsumeEvent(evt);
            }

            private void OnWheel(WheelEvent evt)
            {
                Vector2 point = target.WorldToLocal(evt.mousePosition);
                if (owner.compositor == null || !target.contentRect.Contains(point) || evt.delta.y == 0f ||
                    float.IsNaN(evt.delta.y) || float.IsInfinity(evt.delta.y)) return;
                SpriteEditorUI.ConsumeEvent(evt);
                if (IsDragging && !panning) Cancel();
                owner.FinishPreviewTransform();
                owner.FinishPaintingStroke();
                SpritePreviewElement canvas = owner.toolkitPreviewCanvas;
                canvas.ZoomAt(point, PreviewViewport.WheelScale(canvas.PixelScale, evt.delta.y));
                if (IsPanning) current = point;
                owner.UpdatePreviewCursor(point, evt.altKey);
            }

            private void OnMove(PointerMoveEvent evt)
            {
                if (!IsDragging || evt.pointerId != pointerId) return;
                if ((evt.pressedButtons & (panning ? 4 : 1)) == 0)
                {
                    Cancel();
                    owner.UpdatePreviewCursor(evt.localPosition, evt.altKey);
                    evt.StopImmediatePropagation();
                    return;
                }
                Vector2 point = evt.localPosition;
                if (panning) owner.toolkitPreviewCanvas.Pan(point - current);
                current = point;
                owner.UpdatePreviewCursor(point, evt.altKey);
                selection.MarkDirtyRepaint();
                evt.StopImmediatePropagation();
            }

            private void OnUp(PointerUpEvent evt)
            {
                if (!IsDragging || evt.pointerId != pointerId || evt.button != (panning ? 2 : 0)) return;
                if (panning) owner.toolkitPreviewCanvas.Pan((Vector2)evt.localPosition - current);
                current = evt.localPosition;
                if (!panning)
                {
                    SpritePreviewElement canvas = owner.toolkitPreviewCanvas;
                    Rect region = SelectionRect();
                    if (zoomOut || evt.altKey)
                    {
                        if ((current - start).sqrMagnitude < 16f)
                            canvas.ZoomAt(start, canvas.PixelScale * 0.5f);
                    }
                    else if (region.width >= 4f && region.height >= 4f)
                        canvas.Frame(region);
                    else if ((current - start).sqrMagnitude < 16f)
                        canvas.ZoomAt(start, canvas.PixelScale * 2f);
                }
                Cancel();
                owner.UpdatePreviewCursor(evt.localPosition, evt.altKey);
                SpriteEditorUI.ConsumeEvent(evt);
            }

            private Rect SelectionRect()
            {
                Rect bounds = target.contentRect;
                float xMin = Mathf.Max(Mathf.Min(start.x, current.x), bounds.xMin);
                float yMin = Mathf.Max(Mathf.Min(start.y, current.y), bounds.yMin);
                float xMax = Mathf.Min(Mathf.Max(start.x, current.x), bounds.xMax);
                float yMax = Mathf.Min(Mathf.Max(start.y, current.y), bounds.yMax);
                return new Rect(xMin, yMin, Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
            }

            private void DrawSelection(MeshGenerationContext context)
            {
                if (!IsDragging || panning || zoomOut) return;
                Rect rect = SelectionRect();
                if (rect.width < 4f || rect.height < 4f) return;
                Painter2D painter = context.painter2D;
                painter.fillColor = new Color(0.2f, 0.65f, 1f, 0.12f);
                painter.strokeColor = new Color(0.3f, 0.75f, 1f, 1f);
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(rect.min);
                painter.LineTo(new Vector2(rect.xMax, rect.yMin));
                painter.LineTo(rect.max);
                painter.LineTo(new Vector2(rect.xMin, rect.yMax));
                painter.ClosePath();
                painter.Fill();
                painter.Stroke();
            }
        }
    }
}
