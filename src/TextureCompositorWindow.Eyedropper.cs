using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        private PreviewEyedropperManipulator previewEyedropper;
        private bool CanUsePreviewEyedropper => IsPreviewPaintTool || previewTool == PreviewTool.Fill;

        private void CancelPreviewEyedropper() => previewEyedropper?.Cancel();

        private static Vector2Int PreviewSamplePixel(Vector2 point, Rect image, int width, int height, bool tiled)
        {
            float u = (point.x - image.x) / image.width;
            float v = 1f - (point.y - image.y) / image.height;
            if (tiled) { u = Mathf.Repeat(u, 1f); v = Mathf.Repeat(v, 1f); }
            return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(u * width), 0, width - 1),
                Mathf.Clamp(Mathf.FloorToInt(v * height), 0, height - 1));
        }

        private static Color ReadPreviewSampleColor(RenderTexture source, Texture2D pixel, Vector2Int coordinate)
        {
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                pixel.ReadPixels(new Rect(coordinate.x, coordinate.y, 1, 1), 0, 0, false);
                return HdrUtility.Encode(pixel.GetPixel(0, 0));
            }
            finally { RenderTexture.active = previous; }
        }

        private sealed class PreviewEyedropperManipulator : PointerManipulator
        {
            private readonly TextureCompositorWindow owner;
            private readonly VisualElement icon;
            private RenderTexture source;
            private Texture2D pixel;
            private int pointerId = -1;
            private Vector2 position;
            private Vector2Int lastPixel = new Vector2Int(-1, -1);
            private bool inside;

            internal PreviewEyedropperManipulator(TextureCompositorWindow owner)
            {
                this.owner = owner;
                icon = new VisualElement { pickingMode = PickingMode.Ignore, usageHints = UsageHints.DynamicTransform };
                icon.AddToClassList("sprite-editor-eyedropper-cursor");
                icon.AddToClassList("sprite-editor-eyedropper-cursor--hidden");
                icon.generateVisualContent += DrawIcon;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.Add(icon);
                target.RegisterCallback<PointerDownEvent>(OnDown);
                target.RegisterCallback<PointerMoveEvent>(OnMove);
                target.RegisterCallback<PointerUpEvent>(OnUp);
                target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
                target.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
                target.RegisterCallback<PointerLeaveEvent>(OnLeave);
                target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                Cancel();
                target.UnregisterCallback<PointerDownEvent>(OnDown);
                target.UnregisterCallback<PointerMoveEvent>(OnMove);
                target.UnregisterCallback<PointerUpEvent>(OnUp);
                target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
                target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
                target.UnregisterCallback<PointerLeaveEvent>(OnLeave);
                target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
                icon.RemoveFromHierarchy();
            }

            internal void UpdateCursor(Vector2 point, bool alt)
            {
                position = point;
                inside = target.contentRect.Contains(point);
                bool visible = inside && alt && owner.CanUsePreviewEyedropper && owner.paintingLayer == null &&
                    !(owner.previewZoomManipulator?.IsPanning ?? false) && owner.PreviewContainsPaintPoint(point);
                icon.EnableInClassList("sprite-editor-eyedropper-cursor--hidden", !visible);
                if (visible) icon.style.translate = new Translate(point.x + 12f, point.y + 12f);
            }

            internal void UpdateModifier(bool alt)
            {
                if (inside) owner.UpdatePreviewCursor(position, alt);
            }

            internal void Cancel()
            {
                int captured = pointerId;
                pointerId = -1;
                if (captured >= 0 && target.HasPointerCapture(captured)) target.ReleasePointer(captured);
                if (source != null) RenderTexture.ReleaseTemporary(source);
                source = null;
                if (pixel != null) Object.DestroyImmediate(pixel);
                pixel = null;
                lastPixel = new Vector2Int(-1, -1);
                inside = false;
                icon.AddToClassList("sprite-editor-eyedropper-cursor--hidden");
            }

            private void OnDown(PointerDownEvent evt)
            {
                if (pointerId >= 0)
                {
                    evt.PreventDefault();
                    evt.StopImmediatePropagation();
                    return;
                }
                if (!evt.altKey || evt.button != 0 || !owner.CanUsePreviewEyedropper ||
                    owner.compositor == null || owner.paintingLayer != null ||
                    !owner.PreviewContainsPaintPoint(evt.localPosition)) return;
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                owner.Focus();
                target.Focus();
                try
                {
                    source = owner.compositor.RenderPreview(Mathf.Max(owner.compositor.width, owner.compositor.height));
                    TextureFormat format = source.format == RenderTextureFormat.ARGBFloat
                        ? TextureFormat.RGBAFloat : TextureFormat.RGBAHalf;
                    pixel = new Texture2D(1, 1, format, false, true)
                    { hideFlags = HideFlags.HideAndDontSave };
                    pointerId = evt.pointerId;
                    target.CapturePointer(pointerId);
                    Sample(evt.localPosition);
                    owner.UpdatePreviewCursor(evt.localPosition, true);
                }
                catch (System.Exception exception)
                {
                    Cancel();
                    Debug.LogException(exception);
                    owner.ShowNotification(new GUIContent("Unable to sample the composition."));
                }
            }

            private void OnMove(PointerMoveEvent evt)
            {
                if (pointerId < 0 || evt.pointerId != pointerId) return;
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                if ((evt.pressedButtons & 1) == 0) Cancel();
                else if (evt.altKey)
                {
                    try { Sample(evt.localPosition); }
                    catch (System.Exception exception) { Cancel(); Debug.LogException(exception); }
                }
                owner.UpdatePreviewCursor(evt.localPosition, evt.altKey);
            }

            private void OnUp(PointerUpEvent evt)
            {
                if (evt.pointerId != pointerId || evt.button != 0) return;
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                try { if (evt.altKey) Sample(evt.localPosition); }
                finally { Cancel(); }
                owner.UpdatePreviewCursor(evt.localPosition, evt.altKey);
            }

            private void Sample(Vector2 point)
            {
                Rect image = owner.toolkitPreviewCanvas.ImageRect;
                if (source == null || image.width <= 0f || image.height <= 0f || !owner.PreviewContainsPaintPoint(point)) return;
                Vector2Int next = PreviewSamplePixel(point, image, source.width, source.height, owner.tiledPreview);
                if (next == lastPixel) return;
                Color color = ReadPreviewSampleColor(source, pixel, next);
                owner.paintSettings.brushColor = color;
                owner.SavePaintToolSettings();
                owner.toolkitHeaderBindings.Refresh();
                lastPixel = next;
            }

            private void OnCaptureOut(PointerCaptureOutEvent evt) { if (evt.pointerId == pointerId) Cancel(); }
            private void OnPointerCancel(PointerCancelEvent evt) { if (evt.pointerId == pointerId) Cancel(); }
            private void OnLeave(PointerLeaveEvent evt)
            {
                inside = false;
                icon.AddToClassList("sprite-editor-eyedropper-cursor--hidden");
            }
            private void OnDetach(DetachFromPanelEvent evt) => Cancel();

            private static void DrawIcon(MeshGenerationContext context)
            {
                Painter2D p = context.painter2D;
                p.lineJoin = LineJoin.Round;
                for (int pass = 0; pass < 2; pass++)
                {
                    p.lineWidth = pass == 0 ? 4f : 1.5f;
                    p.strokeColor = pass == 0 ? new Color(0f, 0f, 0f, .85f) : Color.white;
                    p.BeginPath();
                    p.MoveTo(new Vector2(3, 21));
                    p.LineTo(new Vector2(4, 16));
                    p.LineTo(new Vector2(14, 6));
                    p.LineTo(new Vector2(18, 10));
                    p.LineTo(new Vector2(8, 20));
                    p.ClosePath();
                    p.Stroke();
                    p.BeginPath();
                    p.MoveTo(new Vector2(12, 4));
                    p.LineTo(new Vector2(20, 12));
                    p.MoveTo(new Vector2(16, 7));
                    p.LineTo(new Vector2(20, 3));
                    p.LineTo(new Vector2(23, 6));
                    p.LineTo(new Vector2(19, 10));
                    p.Stroke();
                }
            }
        }
    }
}
