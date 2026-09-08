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

        private void AddPaintColorFields(VisualElement row)
        {
            ColorField primary = CompactField(new ColorField(), 54f);
            primary.tooltip = PrimaryBrushColorContent.tooltip;
            toolkitHeaderBindings.Track(primary, () => paintSettings.brushColor);
            primary.RegisterValueChangedCallback(evt => ApplyPaintToolChange(
                "Change Foreground Color", () => paintSettings.brushColor = evt.newValue));
            row.Add(primary);
            ColorField secondary = CompactField(new ColorField(), 54f);
            secondary.tooltip = SecondaryBrushColorContent.tooltip;
            toolkitHeaderBindings.Track(secondary, () => paintSettings.secondaryBrushColor);
            secondary.RegisterValueChangedCallback(evt => ApplyPaintToolChange(
                "Change Background Color", () => paintSettings.secondaryBrushColor = evt.newValue));
            row.Add(secondary);
        }

        private void AddFillSettings()
        {
            VisualElement row = SpriteEditorUI.CreateToolbar();
            row.AddToClassList("sprite-editor-fill-settings");
            BindPreviewSettingsRow(row, PreviewTool.Fill);
            AddPaintColorFields(row);
            Toggle allLayers = new Toggle("All Layers");
            allLayers.AddToClassList("sprite-editor-fill-all-layers");
            allLayers.tooltip = "On: sample the full-resolution visible composition. Off: sample this layer's stored pixels. Both paint only this Drawing layer.";
            toolkitHeaderBindings.Track(allLayers, () => paintSettings.fillSampleMode == FillSampleMode.AllLayers);
            allLayers.RegisterValueChangedCallback(evt => ApplyPaintToolChange(
                "Change Fill Sample", () => paintSettings.fillSampleMode = evt.newValue ? FillSampleMode.AllLayers : FillSampleMode.CurrentLayer));
            row.Add(allLayers);
            Toggle contiguous = new Toggle("Contiguous");
            contiguous.AddToClassList("sprite-editor-fill-contiguous");
            contiguous.tooltip = "On: fill only the connected area at the clicked pixel. Off: fill all similar pixels across the layer, even in separate areas. Uses the All Layers setting and Tolerance.";
            toolkitHeaderBindings.Track(contiguous, () => paintSettings.fillContiguous);
            contiguous.RegisterValueChangedCallback(evt => ApplyPaintToolChange(
                "Change Fill Contiguous", () => paintSettings.fillContiguous = evt.newValue));
            row.Add(contiguous);
            Slider tolerance = new Slider("Tolerance", 0f, 255f) { showInputField = true };
            tolerance.AddToClassList("sprite-editor-fill-tolerance");
            tolerance.tooltip = "Color/alpha similarity to the clicked pixel (0–255). Low values stop at small differences; high values include more colors. Contiguous limits matching to the connected area.";
            toolkitHeaderBindings.Track(tolerance, () => (float)paintSettings.fillTolerance);
            tolerance.RegisterValueChangedCallback(evt => ApplyPaintToolChange(
                "Change Fill Tolerance", () => paintSettings.fillTolerance = Mathf.Clamp(Mathf.RoundToInt(evt.newValue), 0, 255)));
            row.Add(tolerance);
            Toggle antialias = new Toggle("Antialias");
            antialias.AddToClassList("sprite-editor-fill-antialias");
            antialias.tooltip = "Soften the fill edge with partial pixel coverage. Disable for hard pixel-art edges.";
            toolkitHeaderBindings.Track(antialias, () => paintSettings.fillAntialias);
            antialias.RegisterValueChangedCallback(evt => ApplyPaintToolChange(
                "Change Fill Antialias", () => paintSettings.fillAntialias = evt.newValue));
            row.Add(antialias);
            IntegerField expand = new IntegerField("Expand (px)");
            expand.AddToClassList("sprite-editor-fill-expand");
            expand.tooltip = "Grow the detected area by 0–32 source pixels to overlap outlines. Unlike Tolerance, this does not change which colors are connected.";
            toolkitHeaderBindings.Track(expand, () => paintSettings.fillExpand);
            expand.RegisterValueChangedCallback(evt => ApplyPaintToolChange(
                "Change Fill Expansion", () => paintSettings.fillExpand = Mathf.Clamp(evt.newValue, 0, 32)));
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
            Color foreground = paintSettings.brushColor;
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
                if (length > MaximumFillPixels || (paintSettings.fillSampleMode == FillSampleMode.AllLayers &&
                    (long)compositor.width * compositor.height > MaximumFillPixels))
                    throw new InvalidOperationException("Fill supports up to 16,777,216 pixels (for example, 4096 × 4096) per source/reference image.");
                using var source = new NativeArray<Color32>(length, Allocator.TempJob);
                using var reference = new NativeArray<Color32>(length, Allocator.TempJob);
                using var valid = new NativeArray<byte>(length, Allocator.TempJob);
                using var output = new NativeArray<Color32>(length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                if (stored != null) NativeArray<Color32>.Copy(stored.GetRawTextureData<Color32>(), source);
                if (paintSettings.fillSampleMode == FillSampleMode.AllLayers)
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
                    paintSettings.fillTolerance, paintSettings.fillExpand, paintSettings.fillAntialias, paintSettings.fillContiguous)) return true;
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
