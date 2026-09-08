using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        private const int MaximumFillPixels = 16777216;

        private void AddPaintColorFields(VisualElement row, DrawingLayer layer)
        {
            ColorField primary = CompactField(new ColorField(), 54f);
            primary.tooltip = PrimaryBrushColorContent.tooltip;
            toolkitHeaderBindings.Track(primary, () => layer.brushColor);
            primary.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Foreground Color", () => layer.brushColor = evt.newValue));
            row.Add(primary);
            ColorField secondary = CompactField(new ColorField(), 54f);
            secondary.tooltip = SecondaryBrushColorContent.tooltip;
            toolkitHeaderBindings.Track(secondary, () => layer.secondaryBrushColor);
            secondary.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Background Color", () => layer.secondaryBrushColor = evt.newValue));
            row.Add(secondary);
        }

        private void AddFillSettings(DrawingLayer layer)
        {
            VisualElement row = SpriteEditorUI.CreateToolbar();
            row.AddToClassList("sprite-editor-fill-settings");
            toolkitHeaderBindings.Add(() => row.EnableInClassList("sprite-editor-tool-options--hidden", !IsPreviewFillEnabled));
            AddPaintColorFields(row, layer);
            Toggle allLayers = new Toggle("All Layers");
            allLayers.AddToClassList("sprite-editor-fill-all-layers");
            allLayers.tooltip = "On: sample the full-resolution visible composition. Off: sample this layer's stored pixels. Both paint only this Drawing layer.";
            toolkitHeaderBindings.Track(allLayers, () => layer.fillSampleMode == FillSampleMode.AllLayers);
            allLayers.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Fill Sample", () => layer.fillSampleMode = evt.newValue ? FillSampleMode.AllLayers : FillSampleMode.CurrentLayer));
            row.Add(allLayers);
            Toggle contiguous = new Toggle("Contiguous");
            contiguous.AddToClassList("sprite-editor-fill-contiguous");
            contiguous.tooltip = "On: fill only the connected area at the clicked pixel. Off: fill all similar pixels across the layer, even in separate areas. Uses the All Layers setting and Tolerance.";
            toolkitHeaderBindings.Track(contiguous, () => layer.fillContiguous);
            contiguous.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Fill Contiguous", () => layer.fillContiguous = evt.newValue));
            row.Add(contiguous);
            Slider tolerance = new Slider("Tolerance", 0f, 255f) { showInputField = true };
            tolerance.AddToClassList("sprite-editor-fill-tolerance");
            tolerance.tooltip = "Color/alpha similarity to the clicked pixel (0–255). Low values stop at small differences; high values include more colors. Contiguous limits matching to the connected area.";
            toolkitHeaderBindings.Track(tolerance, () => (float)layer.fillTolerance);
            tolerance.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Fill Tolerance", () => layer.fillTolerance = Mathf.Clamp(Mathf.RoundToInt(evt.newValue), 0, 255)));
            row.Add(tolerance);
            Toggle antialias = new Toggle("Antialias");
            antialias.AddToClassList("sprite-editor-fill-antialias");
            antialias.tooltip = "Soften the fill edge with partial pixel coverage. Disable for hard pixel-art edges.";
            toolkitHeaderBindings.Track(antialias, () => layer.fillAntialias);
            antialias.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Fill Antialias", () => layer.fillAntialias = evt.newValue));
            row.Add(antialias);
            IntegerField expand = new IntegerField("Expand (px)");
            expand.AddToClassList("sprite-editor-fill-expand");
            expand.tooltip = "Grow the detected area by 0–32 source pixels to overlap outlines. Unlike Tolerance, this does not change which colors are connected.";
            toolkitHeaderBindings.Track(expand, () => layer.fillExpand);
            expand.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Fill Expansion", () => layer.fillExpand = Mathf.Clamp(evt.newValue, 0, 32)));
            row.Add(expand);
            toolkitPreviewHeader.Add(row);
        }

        private bool HandleFillPointerDown(PointerDownEvent evt)
        {
            if (!IsPreviewFillEnabled || evt.button != 0 || evt.altKey || compositor == null)
                return false;
            if (!toolkitPreviewCanvas.ImageRect.Contains(evt.localPosition)) return false;
            evt.PreventDefault();
            evt.StopImmediatePropagation();
            Focus();
            toolkitPreviewCanvas.Focus();
            DrawingLayer layer = (DrawingLayer)GetSelectedLayer();
            if (Mathf.Abs(layer.transform.scale.x) < 0.00001f || Mathf.Abs(layer.transform.scale.y) < 0.00001f ||
                !TryMapPreviewToLayerUv(evt.localPosition, toolkitPreviewCanvas.ImageRect, layer, out Vector2 uv))
            {
                ShowNotification(new GUIContent("Fill inside the layer's source frame, or apply its transform first."));
                return true;
            }
            Vector4 channels = PreviewChannelMask;
            Color foreground = layer.brushColor;
            Color32 color = new Color(foreground.r * channels.x, foreground.g * channels.y,
                foreground.b * channels.z, foreground.a * channels.w);
            if (color.a == 0) return true;
            FinishPaintingStroke();
            FinishPreviewTransform();
            lineAnchorLayer = null;
            Texture2D composite = null;
            int undoGroup = -1;
            try
            {
                Texture2D stored = layer.StoredTexture;
                int width = stored != null ? stored.width : compositor.width;
                int height = stored != null ? stored.height : compositor.height;
                int length = checked(width * height);
                if (length > MaximumFillPixels || (layer.fillSampleMode == FillSampleMode.AllLayers &&
                    (long)compositor.width * compositor.height > MaximumFillPixels))
                    throw new InvalidOperationException("Fill supports up to 16,777,216 pixels (for example, 4096 × 4096) per source/reference image.");
                using var source = new NativeArray<Color32>(length, Allocator.TempJob);
                using var reference = new NativeArray<Color32>(length, Allocator.TempJob);
                using var valid = new NativeArray<byte>(length, Allocator.TempJob);
                using var output = new NativeArray<Color32>(length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                if (stored != null) NativeArray<Color32>.Copy(stored.GetRawTextureData<Color32>(), source);
                if (layer.fillSampleMode == FillSampleMode.AllLayers)
                {
                    composite = compositor.Compose();
                    Vector2 origin = MapLayerToDocumentUv(Vector2.zero, layer);
                    new FloodFillUtility.ProjectReferenceJob
                    {
                        composite = composite.GetRawTextureData<Color32>(), reference = reference, valid = valid,
                        width = width, compositeWidth = composite.width, compositeHeight = composite.height,
                        origin = origin,
                        stepX = (MapLayerToDocumentUv(Vector2.right, layer) - origin) / width,
                        stepY = (MapLayerToDocumentUv(Vector2.up, layer) - origin) / height
                    }.Schedule(length, 256).Complete();
                }
                else
                {
                    new FloodFillUtility.CopyReferenceJob
                    {
                        source = source, reference = reference, valid = valid
                    }.Schedule(length, 256).Complete();
                }
                int seed = Mathf.Clamp(Mathf.FloorToInt(uv.y * height), 0, height - 1) * width +
                    Mathf.Clamp(Mathf.FloorToInt(uv.x * width), 0, width - 1);
                if (!FloodFillUtility.Fill(source, reference, valid, output, width, height, seed, color,
                    layer.fillTolerance, layer.fillExpand, layer.fillAntialias, layer.fillContiguous)) return true;
                Undo.IncrementCurrentGroup();
                undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Fill Drawing Layer");
                Undo.RecordObject(compositor, "Fill Drawing Layer");
                layer.ApplyFillPixels(output, width, height, "Fill Drawing Layer");
                temporaryDocumentDirty |= !AssetDatabase.Contains(compositor);
                compositor.MarkChanged();
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(undoGroup);
                RequestPreview(true);
            }
            catch (Exception exception)
            {
                if (undoGroup >= 0)
                {
                    Undo.FlushUndoRecordObjects();
                    Undo.RevertAllDownToGroup(undoGroup);
                    compositor.InvalidateDrawingLayerSurfaces();
                    RequestPreview(true);
                }
                ShowNotification(new GUIContent("Fill failed: " + exception.Message));
                Debug.LogException(exception);
            }
            finally
            {
                if (composite != null) DestroyImmediate(composite);
                if (undoGroup >= 0) Undo.IncrementCurrentGroup();
            }
            return true;
        }
    }
}
