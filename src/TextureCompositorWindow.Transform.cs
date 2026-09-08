using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        [NonSerialized] private bool previewTransformActive;
        [NonSerialized] private double nextTransformPreviewAt;
        private VisualElement previewTransformOverlay;
        private PreviewTransformManipulator previewTransformManipulator;

        private bool IsPreviewTransformEnabled => previewTransformActive &&
            GetSelectedLayer() is Layer layer && !layer.IsGroup;

        private void BuildPreviewTransformTool()
        {
            previewTransformOverlay = new VisualElement { pickingMode = PickingMode.Ignore };
            previewTransformOverlay.StretchToParentSize();
            toolkitPreviewCanvas.Add(previewTransformOverlay);
            previewTransformManipulator = new PreviewTransformManipulator(this);
            previewTransformOverlay.generateVisualContent += previewTransformManipulator.Draw;
            toolkitPreviewCanvas.AddManipulator(previewTransformManipulator);
            toolkitPreviewCanvas.RegisterCallback<GeometryChangedEvent>(_ => previewTransformOverlay.MarkDirtyRepaint());
        }

        private void AddPreviewTransformSettings()
        {
            VisualElement row = SpriteEditorUI.CreateToolbar();
            row.AddToClassList("sprite-editor-transform-settings");
            VisualElement tilingGroup = SpriteEditorUI.CreateRow();
            tilingGroup.AddToClassList("sprite-editor-transform-option");
            tilingGroup.Add(CreateCompactLabel("Tiling", 38f));
            EnumField tiling = CompactField(new EnumField(TransformTilingMode.Clip), 100f);
            tiling.tooltip = "Clip: transparent outside the frame. Repeat: tile. Mirror: reflected tiles. " +
                "Source: inherit the texture's wrap modes; Clamp extends edge pixels instead of clipping.";
            toolkitHeaderBindings.Track(tiling, () => (Enum)(GetSelectedLayer()?.transform.tiling ?? TransformTilingMode.Clip));
            toolkitHeaderBindings.Add(() => row.style.display = IsPreviewTransformEnabled ? DisplayStyle.Flex : DisplayStyle.None);
            tiling.RegisterValueChangedCallback(evt =>
            {
                Layer selected = GetSelectedLayer();
                if (selected == null || selected.IsGroup)
                    return;
                FinishPreviewTransform();
                ApplyToolkitChange("Change Transform Tiling", () => selected.transform.tiling = (TransformTilingMode)evt.newValue);
            });
            tilingGroup.Add(tiling);
            row.Add(tilingGroup);
            VisualElement filterGroup = SpriteEditorUI.CreateRow();
            filterGroup.AddToClassList("sprite-editor-transform-option");
            filterGroup.Add(CreateCompactLabel("Filter", 36f));
            EnumField filter = CompactField(new EnumField(LayerFilterMode.Source), 100f);
            filter.tooltip = "Source: inherit the texture's Filter Mode. Point: sharp pixels. Bilinear: smooth. " +
                "Trilinear: smooth mip transitions (requires source mipmaps). Independent of Tiling.";
            toolkitHeaderBindings.Track(filter, () => (Enum)(GetSelectedLayer()?.filterMode ?? LayerFilterMode.Source));
            filter.RegisterValueChangedCallback(evt =>
            {
                Layer selected = GetSelectedLayer();
                if (selected == null || selected.IsGroup)
                    return;
                FinishPreviewTransform();
                FinishPaintingStroke();
                ApplyToolkitChange("Change Layer Filter", () => selected.filterMode = (LayerFilterMode)evt.newValue);
            });
            filterGroup.Add(filter);
            row.Add(filterGroup);
            row.Add(SpriteEditorUI.CreateOriginalAspectButton(
                GetSelectedLayer, () => compositor,
                (undoName, change) =>
                {
                    FinishPreviewTransform();
                    FinishPaintingStroke();
                    ApplyToolkitChange(undoName, change);
                }, toolkitHeaderBindings));
            toolkitPreviewHeader.Add(row);
        }

        private void TogglePreviewTransform()
        {
            SetPreviewTool(!previewTransformActive);
        }

        private void SetPreviewTool(bool transform)
        {
            if (transform && !(GetSelectedLayer() is Layer layer && !layer.IsGroup))
                return;
            FinishPreviewTransform();
            FinishPaintingStroke();
            previewTransformActive = transform;
            lineAnchorLayer = null;
            RefreshToolkitInterface();
            toolkitPreviewCanvas?.Focus();
        }

        private bool HandlePreviewTransformKey(KeyDownEvent evt)
        {
            if (evt.ctrlKey || evt.commandKey || evt.altKey)
                return false;
            if (evt.keyCode == KeyCode.T)
                TogglePreviewTransform();
            else if (evt.keyCode == KeyCode.B && GetSelectedLayer() is DrawingLayer)
                SetPreviewTool(false);
            else if (IsPreviewTransformEnabled && evt.keyCode == KeyCode.Escape)
            {
                if (previewTransformManipulator != null && previewTransformManipulator.IsDragging)
                    FinishPreviewTransform(true);
                else
                    TogglePreviewTransform();
            }
            else if (IsPreviewTransformEnabled && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
                TogglePreviewTransform();
            else
                return false;
            evt.PreventDefault();
            evt.StopImmediatePropagation();
            return true;
        }

        private void FinishPreviewTransform(bool cancel = false)
        {
            previewTransformManipulator?.End(cancel, true);
        }

        private void RefreshPreviewTransformTool()
        {
            previewTransformManipulator?.ValidateSelection();
            previewTransformOverlay?.MarkDirtyRepaint();
        }

        private void RequestTransformPreview()
        {
            double requestedAt = Math.Max(EditorApplication.timeSinceStartup, nextTransformPreviewAt);
            if (!previewRequested || previewAt > requestedAt)
                previewAt = requestedAt;
            previewRequested = true;
            previewTransformOverlay.MarkDirtyRepaint();
        }

        private sealed class PreviewTransformManipulator : PointerManipulator
        {
            private static readonly Vector2[] Handles =
            {
                new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 0.5f)
            };
            private const int MoveHandle = 8;
            private const int RotateHandle = 9;
            private const int PivotHandle = 10;
            private const float PivotSnapDistance = 10f;
            private readonly TextureCompositorWindow owner;
            private Layer layer;
            private TextureTransform original;
            private Vector2 size;
            private Vector2 pointerStart;
            private Vector2 lastPointerPosition;
            private int pointerId = -1;
            private int handle;
            private int undoGroup = -1;
            private Rect gestureImageRect;

            public bool IsDragging => pointerId >= 0;

            public PreviewTransformManipulator(TextureCompositorWindow owner) => this.owner = owner;

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<PointerDownEvent>(OnDown);
                target.RegisterCallback<PointerMoveEvent>(OnMove);
                target.RegisterCallback<PointerUpEvent>(OnUp);
                target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
                target.RegisterCallback<KeyDownEvent>(OnModifierDown);
                target.RegisterCallback<KeyUpEvent>(OnModifierUp);
                target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                End(false, true);
                target.UnregisterCallback<PointerDownEvent>(OnDown);
                target.UnregisterCallback<PointerMoveEvent>(OnMove);
                target.UnregisterCallback<PointerUpEvent>(OnUp);
                target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
                target.UnregisterCallback<KeyDownEvent>(OnModifierDown);
                target.UnregisterCallback<KeyUpEvent>(OnModifierUp);
                target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            }

            public void ValidateSelection()
            {
                if (IsDragging && (!owner.IsPreviewTransformEnabled ||
                    !ReferenceEquals(layer, owner.GetSelectedLayer()) ||
                    size != new Vector2(owner.compositor.width, owner.compositor.height)))
                    End(false, true);
            }

            private static Vector2 Rotate(Vector2 point, float degrees)
            {
                float angle = degrees * Mathf.Deg2Rad;
                float cosine = Mathf.Cos(angle);
                float sine = Mathf.Sin(angle);
                return new Vector2(cosine * point.x - sine * point.y, sine * point.x + cosine * point.y);
            }

            private static Vector2 TransformPoint(Vector2 uv, TextureTransform transform, Vector2 dimensions)
            {
                Vector2 pivot = Vector2.Scale(transform.pivot, dimensions);
                return pivot + transform.position + Rotate(
                    Vector2.Scale(Vector2.Scale(uv, dimensions) - pivot, transform.scale), transform.rotation);
            }

            private static Vector2 ToPreview(Vector2 pixels, Rect imageRect, Vector2 dimensions) =>
                new Vector2(imageRect.x + pixels.x / dimensions.x * imageRect.width,
                    imageRect.yMax - pixels.y / dimensions.y * imageRect.height);

            private static Vector2 ToDocument(Vector2 point, Rect imageRect, Vector2 dimensions) =>
                new Vector2((point.x - imageRect.x) / imageRect.width * dimensions.x,
                    (imageRect.yMax - point.y) / imageRect.height * dimensions.y);

            private static Vector2 RotationHandle(TextureTransform transform, Rect imageRect, Vector2 dimensions)
            {
                Vector2 center = ToPreview(TransformPoint(new Vector2(0.5f, 0.5f), transform, dimensions), imageRect, dimensions);
                Vector2 top = ToPreview(TransformPoint(Handles[5], transform, dimensions), imageRect, dimensions);
                Vector2 direction = top - center;
                if (direction.sqrMagnitude < 0.01f)
                    direction = Vector2.up * -1f;
                return top + direction.normalized * 24f;
            }

            private static int HitTest(Vector2 point, TextureTransform transform, Rect imageRect, Vector2 dimensions)
            {
                Vector2 pivot = ToPreview(Vector2.Scale(transform.pivot, dimensions) + transform.position, imageRect, dimensions);
                if ((point - pivot).sqrMagnitude <= 81f)
                    return CanMovePivot(transform) ? PivotHandle : -1;
                if ((point - RotationHandle(transform, imageRect, dimensions)).sqrMagnitude <= 81f)
                    return RotateHandle;
                int nearest = -1;
                float distance = 81f;
                for (int i = 0; i < Handles.Length; i++)
                {
                    float candidate = (point - ToPreview(TransformPoint(Handles[i], transform, dimensions), imageRect, dimensions)).sqrMagnitude;
                    if (candidate < distance)
                    {
                        nearest = i;
                        distance = candidate;
                    }
                }
                if (nearest >= 0)
                    return nearest;
                Vector2 local = Rotate(ToDocument(point, imageRect, dimensions) -
                    Vector2.Scale(transform.pivot, dimensions) - transform.position, -transform.rotation);
                Vector2 source = new Vector2(local.x / SafeScale(transform.scale.x), local.y / SafeScale(transform.scale.y)) +
                    Vector2.Scale(transform.pivot, dimensions);
                return source.x >= 0f && source.y >= 0f && source.x <= dimensions.x && source.y <= dimensions.y ? MoveHandle : -1;
            }

            private void OnDown(PointerDownEvent evt)
            {
                if (!owner.IsPreviewTransformEnabled || evt.button != 0 || evt.altKey || IsDragging)
                    return;
                Rect rect = owner.toolkitPreviewCanvas.ImageRect;
                if (rect.width <= 0f || rect.height <= 0f)
                    return;
                Layer selected = owner.GetSelectedLayer();
                Vector2 dimensions = new Vector2(owner.compositor.width, owner.compositor.height);
                int hit = HitTest(evt.localPosition, selected.transform, rect, dimensions);
                if (hit < 0)
                    return;
                owner.Focus();
                target.Focus();
                layer = selected;
                original = selected.transform;
                size = dimensions;
                handle = hit;
                gestureImageRect = rect;
                pointerStart = ToDocument(evt.localPosition, rect, size);
                lastPointerPosition = evt.localPosition;
                pointerId = evt.pointerId;
                undoGroup = -1;
                target.CapturePointer(pointerId);
                evt.StopImmediatePropagation();
            }

            private void OnMove(PointerMoveEvent evt)
            {
                if (!IsDragging || evt.pointerId != pointerId)
                    return;
                UpdateTransform(evt.localPosition, evt.shiftKey, evt.ctrlKey);
                evt.StopImmediatePropagation();
            }

            private static float SafeScale(float value) => value < 0f ? Mathf.Min(value, -0.00001f) : Mathf.Max(value, 0.00001f);

            private static bool CanMovePivot(TextureTransform transform) =>
                Mathf.Abs(transform.scale.x) >= 0.00001f && Mathf.Abs(transform.scale.y) >= 0.00001f;

            private void OnModifierDown(KeyDownEvent evt)
            {
                if (RefreshPivotModifiers(evt.keyCode, evt.shiftKey, evt.ctrlKey))
                    evt.StopPropagation();
            }

            private void OnModifierUp(KeyUpEvent evt)
            {
                if (RefreshPivotModifiers(evt.keyCode, evt.shiftKey, evt.ctrlKey))
                    evt.StopPropagation();
            }

            private bool RefreshPivotModifiers(KeyCode key, bool shift, bool control)
            {
                if (!IsDragging || handle != PivotHandle ||
                    (key != KeyCode.LeftControl && key != KeyCode.RightControl &&
                     key != KeyCode.LeftShift && key != KeyCode.RightShift))
                    return false;
                UpdateTransform(lastPointerPosition, shift, control);
                return true;
            }

            private Vector2 SnapPivot(Vector2 pivot, Vector2 documentPosition)
            {
                Vector2 previewPosition = ToPreview(documentPosition, gestureImageRect, size);
                float nearestDistance = PivotSnapDistance * PivotSnapDistance;
                Vector2 result = pivot;
                for (int i = 0; i <= Handles.Length; i++)
                {
                    Vector2 anchor = i < Handles.Length ? Handles[i] : new Vector2(0.5f, 0.5f);
                    Vector2 anchorPosition = ToPreview(TransformPoint(anchor, original, size), gestureImageRect, size);
                    float distance = (anchorPosition - previewPosition).sqrMagnitude;
                    if (distance <= nearestDistance)
                    {
                        nearestDistance = distance;
                        result = anchor;
                    }
                }
                return result;
            }

            private void UpdateTransform(Vector2 point, bool constrain, bool disablePivotSnap)
            {
                ValidateSelection();
                if (!IsDragging)
                    return;
                lastPointerPosition = point;
                Vector2 current = ToDocument(point, gestureImageRect, size);
                Vector2 delta = current - pointerStart;
                if (undoGroup < 0 && delta.sqrMagnitude < 0.000001f)
                    return;
                TextureTransform next = original;
                if (handle == MoveHandle || handle == PivotHandle)
                {
                    if (constrain)
                    {
                        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) delta.y = 0f;
                        else delta.x = 0f;
                    }
                    if (handle == PivotHandle)
                    {
                        Vector2 localDelta = Rotate(delta, -original.rotation);
                        Vector2 pivotDelta = new Vector2(localDelta.x / original.scale.x, localDelta.y / original.scale.y);
                        next.pivot += new Vector2(pivotDelta.x / size.x, pivotDelta.y / size.y);
                        if (!disablePivotSnap)
                            next.pivot = SnapPivot(next.pivot, Vector2.Scale(original.pivot, size) + original.position + delta);
                        Vector2 actualPivotDelta = Vector2.Scale(next.pivot - original.pivot, size);
                        next.position += Rotate(Vector2.Scale(actualPivotDelta, original.scale), original.rotation) - actualPivotDelta;
                    }
                    else
                        next.position += delta;
                }
                else if (handle == RotateHandle)
                {
                    Vector2 pivot = Vector2.Scale(original.pivot, size) + original.position;
                    Vector2 from = pointerStart - pivot;
                    Vector2 to = current - pivot;
                    if (from.sqrMagnitude < 0.0001f || to.sqrMagnitude < 0.0001f)
                        return;
                    next.rotation += Vector2.SignedAngle(from, to);
                    if (constrain)
                        next.rotation = Mathf.Round(next.rotation / 15f) * 15f;
                }
                else
                {
                    Vector2 grip = Handles[handle];
                    Vector2 anchor = Vector2.one - grip;
                    Vector2 fixedPoint = TransformPoint(anchor, original, size);
                    Vector2 handlePoint = TransformPoint(grip, original, size) + delta;
                    Vector2 local = Rotate(handlePoint - fixedPoint, -original.rotation);
                    bool x = grip.x != 0.5f;
                    bool y = grip.y != 0.5f;
                    if (x) next.scale.x = SafeScale(local.x / ((grip.x - anchor.x) * size.x));
                    if (y) next.scale.y = SafeScale(local.y / ((grip.y - anchor.y) * size.y));
                    if (constrain)
                    {
                        float ratioX = next.scale.x / SafeScale(original.scale.x);
                        float ratioY = next.scale.y / SafeScale(original.scale.y);
                        float ratio = x && (!y || Mathf.Abs(ratioX - 1f) >= Mathf.Abs(ratioY - 1f)) ? ratioX : ratioY;
                        next.scale = new Vector2(SafeScale(original.scale.x * ratio), SafeScale(original.scale.y * ratio));
                    }
                    Vector2 pivot = Vector2.Scale(original.pivot, size);
                    next.position = fixedPoint - pivot - Rotate(
                        Vector2.Scale(Vector2.Scale(anchor, size) - pivot, next.scale), original.rotation);
                }
                if (next.pivot == layer.transform.pivot && next.position == layer.transform.position && next.scale == layer.transform.scale &&
                    Mathf.Approximately(next.rotation, layer.transform.rotation))
                    return;
                if (undoGroup < 0)
                {
                    Undo.IncrementCurrentGroup();
                    undoGroup = Undo.GetCurrentGroup();
                    string undoName = handle == PivotHandle ? "Move Layer Pivot" : "Transform Layer";
                    Undo.SetCurrentGroupName(undoName);
                    Undo.RegisterCompleteObjectUndo(owner.compositor, undoName);
                }
                layer.transform = next;
                if (handle == PivotHandle)
                    owner.previewTransformOverlay.MarkDirtyRepaint();
                else
                    owner.RequestTransformPreview();
            }

            private void OnUp(PointerUpEvent evt)
            {
                if (!IsDragging || evt.pointerId != pointerId || evt.button != 0)
                    return;
                UpdateTransform(evt.localPosition, evt.shiftKey, evt.ctrlKey);
                End(false, true);
                evt.StopImmediatePropagation();
            }

            private void OnCaptureOut(PointerCaptureOutEvent evt)
            {
                if (evt.pointerId == pointerId)
                    End(false, true);
            }

            private void OnDetach(DetachFromPanelEvent evt) => End(false, true);

            public void End(bool cancel, bool commit)
            {
                if (!IsDragging)
                    return;
                int captured = pointerId;
                pointerId = -1;
                if (target.HasPointerCapture(captured))
                    target.ReleasePointer(captured);
                if (commit && undoGroup >= 0 && owner.compositor != null)
                {
                    if (cancel)
                        layer.transform = original;
                    Undo.FlushUndoRecordObjects();
                    Undo.CollapseUndoOperations(undoGroup);
                    Undo.IncrementCurrentGroup();
                    owner.applyingToolkitChange = true;
                    try { owner.CommitModelChange(); }
                    finally { owner.applyingToolkitChange = false; }
                    owner.lineAnchorLayer = null;
                    owner.RequestPreview(true);
                    owner.toolkitRefreshRequested = true;
                }
                layer = null;
                undoGroup = -1;
                owner.previewTransformOverlay?.MarkDirtyRepaint();
            }

            public void Draw(MeshGenerationContext context)
            {
                if (!owner.IsPreviewTransformEnabled)
                    return;
                Rect rect = owner.toolkitPreviewCanvas.ImageRect;
                if (rect.width <= 0f || rect.height <= 0f)
                    return;
                TextureTransform transform = owner.GetSelectedLayer().transform;
                Vector2 dimensions = new Vector2(owner.compositor.width, owner.compositor.height);
                Painter2D painter = context.painter2D;
                for (int pass = 0; pass < 2; pass++)
                {
                    painter.lineWidth = pass == 0 ? 3f : 1f;
                    painter.strokeColor = pass == 0 ? new Color(0f, 0f, 0f, 0.85f) : new Color(0.35f, 0.75f, 1f);
                    painter.BeginPath();
                    painter.MoveTo(ToPreview(TransformPoint(Handles[0], transform, dimensions), rect, dimensions));
                    for (int i = 2; i < Handles.Length; i += 2)
                        painter.LineTo(ToPreview(TransformPoint(Handles[i], transform, dimensions), rect, dimensions));
                    painter.ClosePath();
                    painter.Stroke();
                    painter.BeginPath();
                    painter.MoveTo(ToPreview(TransformPoint(Handles[5], transform, dimensions), rect, dimensions));
                    painter.LineTo(RotationHandle(transform, rect, dimensions));
                    painter.Stroke();
                }
                painter.lineWidth = 1f;
                painter.strokeColor = Color.black;
                painter.fillColor = Color.white;
                for (int i = 0; i < Handles.Length; i++)
                {
                    Vector2 p = ToPreview(TransformPoint(Handles[i], transform, dimensions), rect, dimensions);
                    painter.BeginPath();
                    painter.MoveTo(p + new Vector2(-3f, -3f));
                    painter.LineTo(p + new Vector2(3f, -3f));
                    painter.LineTo(p + new Vector2(3f, 3f));
                    painter.LineTo(p + new Vector2(-3f, 3f));
                    painter.ClosePath();
                    painter.Fill();
                    painter.Stroke();
                }
                painter.BeginPath();
                painter.Arc(RotationHandle(transform, rect, dimensions), 4f, Angle.Degrees(0f), Angle.Degrees(360f), ArcDirection.Clockwise);
                painter.Fill();
                painter.Stroke();
                Vector2 center = ToPreview(Vector2.Scale(transform.pivot, dimensions) + transform.position, rect, dimensions);
                for (int pass = 0; pass < 2; pass++)
                {
                    painter.lineWidth = pass == 0 ? 3f : 1f;
                    painter.strokeColor = pass == 0 ? Color.black :
                        CanMovePivot(transform) ? new Color(1f, 0.78f, 0.2f) : Color.gray;
                    painter.BeginPath();
                    painter.Arc(center, 6f, Angle.Degrees(0f), Angle.Degrees(360f), ArcDirection.Clockwise);
                    painter.Stroke();
                    painter.BeginPath();
                    painter.MoveTo(center - new Vector2(8f, 0f));
                    painter.LineTo(center + new Vector2(8f, 0f));
                    painter.MoveTo(center - new Vector2(0f, 8f));
                    painter.LineTo(center + new Vector2(0f, 8f));
                    painter.Stroke();
                }
            }
        }
    }
}
