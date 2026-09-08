using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow : EditorWindow
    {
        private const int PreviewMaxSize = 512;
        private const double PreviewDelay = 0.12d;
        private const double PaintingPreviewInterval = 1d / 30d;
        private const float DefaultPaintingPreviewScale = 0.375f;
        private const float MinimumPaintingPreviewScale = 0.125f;
        private const float MaximumPaintingPreviewScale = 1f;
        private const float PreviewPaneMinWidth = 200f;
        private const float SettingsPaneMinWidth = 320f;
        private const float PanePadding = 8f;
        private const string DraggedLayerIdKey = "DCFApixels.SpriteEditor.DraggedLayerId";
        private const string DraggedCompositorIdKey = "DCFApixels.SpriteEditor.DraggedCompositorId";
        private const string PaintingPreviewScalePrefKey = "DCFApixels.SpriteEditor.PaintingPreviewScale";

        private static readonly GUIContent LayerDragHandleContent = new GUIContent(
            "≡",
            "Drag to reorder this layer or move it into a group.");
        private static readonly GUIContent LayerDragHintContent = new GUIContent(
            "≡ drag",
            "Drag a row by its handle. Drop on a line to reorder, or on a highlighted group to move inside.");
        private static readonly Color DropIndicatorColor = new Color(0.20f, 0.58f, 0.95f, 1f);
        private static readonly Color GroupDropHighlightColor = new Color(0.20f, 0.58f, 0.95f, 0.22f);
        private static readonly GUIContent MirrorVerticalContent = new GUIContent(
            "Mirror X",
            "Reflect each brush stroke across the vertical axis through Center.");
        private static readonly GUIContent MirrorHorizontalContent = new GUIContent(
            "Mirror Y",
            "Reflect each brush stroke across the horizontal axis through Center.");
        private static readonly GUIContent RepeatBoundaryContent = new GUIContent(
            "Edges",
            "Continue lets a stroke cross repeated shape boundaries. Clip keeps the whole stroke inside the cell or sector where it started.");
        private static readonly GUIContent BrushSpacingContent = new GUIContent(
            "Step",
            "Distance between brush stamps as a percentage of brush size. Larger values are faster and produce a dotted stroke.");
        private static readonly GUIContent LivePreviewQualityContent = new GUIContent(
            "Live Quality",
            "Resolution used while painting. 100% disables downscaling; lower values make effect-heavy previews faster.");
        private static readonly GUIContent PrimaryBrushColorContent = new GUIContent(
            string.Empty,
            "Foreground brush color. Press X to swap it with the background color.");
        private static readonly GUIContent SecondaryBrushColorContent = new GUIContent(
            string.Empty,
            "Background brush color. Press X to swap it with the foreground color.");
        private static readonly System.Reflection.PropertyInfo UnityShortcutsEnabledProperty =
            typeof(EditorWindow).Assembly
                .GetType("UnityEditor.ShortcutManagement.ShortcutIntegration")
                ?.GetProperty(
                    "enabled",
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

        private static int unityShortcutSuppressionOwners;
        private static bool restoreUnityShortcutsEnabled;
        private static bool shortcutSuppressionWarningLogged;

        [SerializeField] private TextureCompositor compositor;
        [SerializeField] private string selectedLayerId;
        [SerializeField] private Vector2 scrollPosition;
        [SerializeField] private float previewPaneWidth = 340f;

        [NonSerialized] private Texture2D previewTexture;
        [NonSerialized] private bool previewRequested;
        [NonSerialized] private double previewAt;
        [NonSerialized] private bool temporaryDocumentDirty;
        [NonSerialized] private string previewError;
        [NonSerialized] private Dictionary<string, bool> groupExpansion;
        [NonSerialized] private DrawingLayer paintingLayer;
        [NonSerialized] private Vector2 lastPaintingUv;
        [NonSerialized] private bool hasLastPaintingUv;
        [NonSerialized] private bool paintingErase;
        [NonSerialized] private int paintingMouseButton = -1;
        [NonSerialized] private double nextPaintingPreviewAt;
        [NonSerialized] private float paintingPreviewScale;
        [NonSerialized] private bool ownsUnityShortcutSuppression;

        [MenuItem("Window/Sprite Editor")]
        public static void ShowWindow()
        {
            GetWindow<TextureCompositorWindow>("Sprite Editor");
        }

        public static void Open(TextureCompositor target)
        {
            if (target == null)
                return;

            TextureCompositorWindow window = GetWindow<TextureCompositorWindow>("Sprite Editor");
            if (window.compositor != target)
            {
                if (!window.ResolveUnsavedTemporaryDocument())
                    return;
                window.SetCompositor(target);
            }
            else
            {
                target.NormalizeModel();
                window.RequestPreview(true);
                window.RebuildToolkitInterface();
            }

            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            minSize = new Vector2(640f, 420f);
            groupExpansion = new Dictionary<string, bool>();
            paintingPreviewScale = ClampPaintingPreviewScale(
                EditorPrefs.GetFloat(PaintingPreviewScalePrefKey, DefaultPaintingPreviewScale));
            TextureCompositor.Changed += OnCompositorChanged;
            Undo.undoRedoPerformed += OnUndoRedo;

            if (compositor == null)
                SetCompositor(CreateTemporaryCompositor());
            else
                compositor.NormalizeModel();
            RequestPreview(true);

            if (focusedWindow == this)
                SuppressUnityShortcuts();
        }

        private void OnDisable()
        {
            RestoreUnityShortcuts();
            FinishPaintingStroke();
            TextureCompositor.Changed -= OnCompositorChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
            ClearLayerDragData();
            ReleasePreview();
        }

        private void OnFocus()
        {
            SuppressUnityShortcuts();
        }

        private void OnLostFocus()
        {
            RestoreUnityShortcuts();
        }

        private void SuppressUnityShortcuts()
        {
            if (ownsUnityShortcutSuppression)
                return;
            ownsUnityShortcutSuppression = AcquireUnityShortcutSuppression();
        }

        private void RestoreUnityShortcuts()
        {
            if (!ownsUnityShortcutSuppression)
                return;
            ownsUnityShortcutSuppression = false;
            ReleaseUnityShortcutSuppression();
        }

        private static bool AcquireUnityShortcutSuppression()
        {
            if (UnityShortcutsEnabledProperty == null)
            {
                LogShortcutSuppressionWarning("Unity shortcut integration API is unavailable.");
                return false;
            }

            try
            {
                if (unityShortcutSuppressionOwners == 0)
                {
                    restoreUnityShortcutsEnabled = (bool)UnityShortcutsEnabledProperty.GetValue(null);
                    if (restoreUnityShortcutsEnabled)
                        UnityShortcutsEnabledProperty.SetValue(null, false);
                }
                unityShortcutSuppressionOwners++;
                return true;
            }
            catch (Exception exception)
            {
                LogShortcutSuppressionWarning(exception.Message);
                return false;
            }
        }

        private static void ReleaseUnityShortcutSuppression()
        {
            if (unityShortcutSuppressionOwners <= 0)
                return;

            unityShortcutSuppressionOwners--;
            if (unityShortcutSuppressionOwners > 0)
                return;

            try
            {
                if (restoreUnityShortcutsEnabled && UnityShortcutsEnabledProperty != null)
                    UnityShortcutsEnabledProperty.SetValue(null, true);
            }
            catch (Exception exception)
            {
                LogShortcutSuppressionWarning(exception.Message);
            }
            finally
            {
                restoreUnityShortcutsEnabled = false;
            }
        }

        private static void LogShortcutSuppressionWarning(string details)
        {
            if (shortcutSuppressionWarningLogged)
                return;
            shortcutSuppressionWarningLogged = true;
            Debug.LogWarning($"Sprite Editor could not isolate Unity shortcuts: {details}");
        }

        private void Update()
        {
            if (!previewRequested || EditorApplication.timeSinceStartup < previewAt)
                return;

            previewRequested = false;
            bool paintingPreview = paintingLayer != null;
            UpdatePreview();
            if (paintingPreview)
                nextPaintingPreviewAt = EditorApplication.timeSinceStartup + PaintingPreviewInterval;
        }

        private Layer GetDraggedLayer()
        {
            TextureCompositor draggedCompositor =
                DragAndDrop.GetGenericData(DraggedCompositorIdKey) as TextureCompositor;
            if (draggedCompositor == null || compositor == null || draggedCompositor != compositor)
                return null;

            string draggedLayerId = DragAndDrop.GetGenericData(DraggedLayerIdKey) as string;
            return compositor.FindLayer(draggedLayerId);
        }

        private bool CanDropLayer(Layer layer, List<Layer> destinationContainer, int destinationIndex)
        {
            return TryResolveLayerDrop(
                layer,
                destinationContainer,
                destinationIndex,
                out _,
                out _,
                out _);
        }

        private bool TryResolveLayerDrop(
            Layer layer,
            List<Layer> destinationContainer,
            int destinationIndex,
            out List<Layer> sourceContainer,
            out int sourceIndex,
            out int normalizedDestinationIndex)
        {
            sourceContainer = null;
            sourceIndex = -1;
            normalizedDestinationIndex = -1;
            if (layer == null ||
                destinationContainer == null ||
                !compositor.TryFindLayer(layer, out sourceContainer, out sourceIndex))
            {
                return false;
            }

            if (layer is GroupLayer group && ContainsLayerContainer(group, destinationContainer))
                return false;

            normalizedDestinationIndex = Mathf.Clamp(destinationIndex, 0, destinationContainer.Count);
            if (ReferenceEquals(sourceContainer, destinationContainer) && sourceIndex < normalizedDestinationIndex)
                normalizedDestinationIndex--;

            return !ReferenceEquals(sourceContainer, destinationContainer) ||
                   normalizedDestinationIndex != sourceIndex;
        }

        private static bool ContainsLayerContainer(GroupLayer group, List<Layer> candidateContainer)
        {
            if (group == null || group.layers == null)
                return false;
            if (ReferenceEquals(group.layers, candidateContainer))
                return true;

            for (int i = 0; i < group.layers.Count; i++)
            {
                if (group.layers[i] is GroupLayer nestedGroup &&
                    ContainsLayerContainer(nestedGroup, candidateContainer))
                {
                    return true;
                }
            }
            return false;
        }

        private void PerformLayerDrop(
            Layer layer,
            List<Layer> destinationContainer,
            int destinationIndex,
            GroupLayer groupToExpand)
        {
            if (!TryResolveLayerDrop(
                    layer,
                    destinationContainer,
                    destinationIndex,
                    out List<Layer> sourceContainer,
                    out int sourceIndex,
                    out int normalizedDestinationIndex))
            {
                return;
            }

            ExecuteModelChange("Move Sprite Layer", () =>
            {
                sourceContainer.RemoveAt(sourceIndex);
                normalizedDestinationIndex = Mathf.Clamp(
                    normalizedDestinationIndex,
                    0,
                    destinationContainer.Count);
                destinationContainer.Insert(normalizedDestinationIndex, layer);
                selectedLayerId = layer.Id;
                if (groupToExpand != null)
                    groupExpansion[groupToExpand.Id] = true;
            });
        }

        private void ClearLayerDragData()
        {
            layerDragPointerCandidateId = null;
            layerDragPointerId = -1;
            DragAndDrop.SetGenericData(DraggedLayerIdKey, null);
            DragAndDrop.SetGenericData(DraggedCompositorIdKey, null);
        }

        private void ClearDrawingLayer(DrawingLayer layer)
        {
            if (layer == null || compositor == null)
                return;
            Undo.RecordObject(compositor, "Clear Drawing Layer");
            layer.PrepareStroke(compositor.width, compositor.height, "Clear Drawing Layer");
            layer.ClearSurface(compositor.width, compositor.height);
            temporaryDocumentDirty |= !AssetDatabase.Contains(compositor);
            compositor.MarkChanged();
            RequestPreview(true);
        }

        private void RefreshPreviewDuringPainting()
        {
            double now = EditorApplication.timeSinceStartup;
            double requestedAt = Math.Max(now, nextPaintingPreviewAt);
            if (!previewRequested || requestedAt < previewAt)
                previewAt = requestedAt;
            previewRequested = true;
            toolkitPreviewCanvas?.MarkDirtyRepaint();
        }

        private void FinishPaintingStroke()
        {
            DrawingLayer finishedLayer = paintingLayer;
            paintingLayer = null;
            paintingErase = false;
            paintingMouseButton = -1;
            paintingPointerId = -1;
            hasLastPaintingUv = false;

            if (finishedLayer == null)
                return;

            finishedLayer.EndStroke();
            finishedLayer.SyncSurfaceToTexture();
            nextPaintingPreviewAt = 0d;
            temporaryDocumentDirty |= compositor != null && !AssetDatabase.Contains(compositor);
            if (compositor != null)
                compositor.MarkChanged();
            Undo.FlushUndoRecordObjects();
            RequestPreview(true);
        }

        private bool TryMapPreviewToLayerUv(
            Vector2 mousePosition,
            Rect imageRect,
            DrawingLayer layer,
            out Vector2 sourceUv)
        {
            sourceUv = default;
            if (imageRect.width <= 0f || imageRect.height <= 0f || layer == null)
                return false;

            Vector2 documentUv = new Vector2(
                (mousePosition.x - imageRect.x) / imageRect.width,
                1f - (mousePosition.y - imageRect.y) / imageRect.height);
            Vector2 outputSize = new Vector2(Mathf.Max(1, compositor.width), Mathf.Max(1, compositor.height));
            Vector2 pivotPixels = Vector2.Scale(layer.transform.pivot, outputSize);
            Vector2 local = Vector2.Scale(documentUv, outputSize) - pivotPixels - layer.transform.position;
            float radians = -layer.transform.rotation * Mathf.Deg2Rad;
            float sine = Mathf.Sin(radians);
            float cosine = Mathf.Cos(radians);
            local = new Vector2(
                cosine * local.x - sine * local.y,
                sine * local.x + cosine * local.y);
            float scaleX = Mathf.Abs(layer.transform.scale.x) < 0.00001f
                ? (layer.transform.scale.x < 0f ? -0.00001f : 0.00001f)
                : layer.transform.scale.x;
            float scaleY = Mathf.Abs(layer.transform.scale.y) < 0.00001f
                ? (layer.transform.scale.y < 0f ? -0.00001f : 0.00001f)
                : layer.transform.scale.y;
            Vector2 sourcePixels = pivotPixels + new Vector2(local.x / scaleX, local.y / scaleY);
            sourceUv = new Vector2(sourcePixels.x / outputSize.x, sourcePixels.y / outputSize.y);
            return sourceUv.x >= 0f && sourceUv.x <= 1f && sourceUv.y >= 0f && sourceUv.y <= 1f;
        }

        private void ShowAddMenuForSelection()
        {
            Layer selected = GetSelectedLayer();
            if (selected != null && compositor.TryFindLayer(selected, out List<Layer> container, out int index))
                ShowAddMenu(container, index);
            else
                ShowAddMenu(compositor.layers, 0);
        }

        private void ShowAddMenu(List<Layer> container, int insertionIndex)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Drawing Layer"), false, () => AddLayer(container, insertionIndex, new DrawingLayer()));
            menu.AddItem(new GUIContent("File Layer"), false, () => AddLayer(container, insertionIndex, new FileLayer()));
            menu.AddItem(new GUIContent("Color Fill Layer"), false, () => AddLayer(container, insertionIndex, new ColorFillLayer()));
            menu.AddItem(new GUIContent("Gradient Layer"), false, () => AddLayer(container, insertionIndex, new GradientLayer()));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Outline Layer"), false, () => AddLayer(container, insertionIndex, new OutlineLayer()));
            menu.AddItem(new GUIContent("SDF Layer"), false, () => AddLayer(container, insertionIndex, new SDFLayer()));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Group"), false, () => AddLayer(container, insertionIndex, new GroupLayer()));
            menu.ShowAsContext();
        }

        private void AddLayer(List<Layer> container, int insertionIndex, Layer layer)
        {
            string automaticName = compositor.AllocateLayerName();
            ExecuteModelChange("Add Sprite Layer", () =>
            {
                layer.layerName = automaticName;
                if (layer is DrawingLayer drawing)
                    drawing.InitializeCanvas(compositor.width, compositor.height);
                insertionIndex = Mathf.Clamp(insertionIndex, 0, container.Count);
                container.Insert(insertionIndex, layer);
                compositor.NormalizeModel();
                selectedLayerId = layer.Id;
                if (layer is GroupLayer)
                    groupExpansion[layer.Id] = true;
            });
        }

        private void GroupSelectedLayer()
        {
            Layer selected = GetSelectedLayer();
            if (selected == null || !compositor.TryFindLayer(selected, out List<Layer> container, out int index))
                return;

            string automaticName = compositor.AllocateLayerName();
            ExecuteModelChange("Group Sprite Layer", () =>
            {
                GroupLayer group = new GroupLayer { layerName = automaticName };
                group.layers.Add(selected);
                container[index] = group;
                compositor.NormalizeModel();
                selectedLayerId = group.Id;
                groupExpansion[group.Id] = true;
            });
        }

        private void Ungroup(GroupLayer group, List<Layer> container)
        {
            int index = container.IndexOf(group);
            if (index < 0)
                return;
            ExecuteModelChange("Ungroup Sprite Layers", () =>
            {
                container.RemoveAt(index);
                container.InsertRange(index, group.layers);
                selectedLayerId = group.layers.Count > 0 ? group.layers[0]?.Id : null;
            });
        }

        private void ShowLayerContextMenu(Layer layer, List<Layer> container, int index)
        {
            GenericMenu menu = new GenericMenu();
            if (index > 0)
                menu.AddItem(new GUIContent("Move Up"), false, () => MoveLayer(container, layer, -1));
            else
                menu.AddDisabledItem(new GUIContent("Move Up"));
            if (index + 1 < container.Count)
                menu.AddItem(new GUIContent("Move Down"), false, () => MoveLayer(container, layer, 1));
            else
                menu.AddDisabledItem(new GUIContent("Move Down"));

            menu.AddSeparator(string.Empty);
            if (index > 0 && container[index - 1] is GroupLayer groupAbove)
                menu.AddItem(new GUIContent("Move Into Group Above"), false, () => MoveIntoGroup(container, layer, groupAbove));
            else
                menu.AddDisabledItem(new GUIContent("Move Into Group Above"));

            if (compositor.TryFindParentGroup(container, out _, out _, out _))
                menu.AddItem(new GUIContent("Move Out Of Group"), false, () => MoveOutOfGroup(container, layer));
            else
                menu.AddDisabledItem(new GUIContent("Move Out Of Group"));

            if (layer is GroupLayer group)
            {
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Add Inside/Drawing Layer"), false, () => AddLayer(group.layers, 0, new DrawingLayer()));
                menu.AddItem(new GUIContent("Add Inside/File Layer"), false, () => AddLayer(group.layers, 0, new FileLayer()));
                menu.AddItem(new GUIContent("Add Inside/Color Fill Layer"), false, () => AddLayer(group.layers, 0, new ColorFillLayer()));
                menu.AddItem(new GUIContent("Add Inside/Gradient Layer"), false, () => AddLayer(group.layers, 0, new GradientLayer()));
                menu.AddItem(new GUIContent("Add Inside/Outline Layer"), false, () => AddLayer(group.layers, 0, new OutlineLayer()));
                menu.AddItem(new GUIContent("Add Inside/SDF Layer"), false, () => AddLayer(group.layers, 0, new SDFLayer()));
                menu.AddItem(new GUIContent("Add Inside/Group"), false, () => AddLayer(group.layers, 0, new GroupLayer()));
                menu.AddItem(new GUIContent("Ungroup"), false, () => Ungroup(group, container));
            }
            else
            {
                menu.AddItem(new GUIContent("Group This Layer"), false, () =>
                {
                    selectedLayerId = layer.Id;
                    GroupSelectedLayer();
                });
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Delete"), false, () => DeleteLayer(container, layer));
            menu.ShowAsContext();
        }

        private void MoveLayer(List<Layer> container, Layer layer, int direction)
        {
            int index = container.IndexOf(layer);
            int destination = index + direction;
            if (index < 0 || destination < 0 || destination >= container.Count)
                return;
            ExecuteModelChange("Reorder Sprite Layer", () =>
            {
                container.RemoveAt(index);
                container.Insert(destination, layer);
            });
        }

        private void MoveIntoGroup(List<Layer> container, Layer layer, GroupLayer group)
        {
            ExecuteModelChange("Move Layer Into Group", () =>
            {
                container.Remove(layer);
                group.layers.Add(layer);
                groupExpansion[group.Id] = true;
            });
        }

        private void MoveOutOfGroup(List<Layer> container, Layer layer)
        {
            if (!compositor.TryFindParentGroup(
                    container,
                    out _,
                    out List<Layer> parentContainer,
                    out int parentIndex))
            {
                return;
            }
            ExecuteModelChange("Move Layer Out Of Group", () =>
            {
                container.Remove(layer);
                parentContainer.Insert(parentIndex + 1, layer);
                selectedLayerId = layer.Id;
            });
        }

        private void DeleteLayer(List<Layer> container, Layer layer)
        {
            ExecuteModelChange("Delete Sprite Layer", () =>
            {
                compositor.DestroyLayerAssets(layer);
                container.Remove(layer);
                layer.ReleaseTransientResources();
                if (selectedLayerId == layer.Id)
                    selectedLayerId = null;
            });
        }

        private void OpenLayerEditor(Layer layer)
        {
            switch (layer)
            {
                case FileLayer fileLayer:
                    FileLayerEditorWindow.Open(fileLayer, compositor);
                    break;
                case ColorFillLayer colorFillLayer:
                    ColorFillLayerEditorWindow.Open(colorFillLayer, compositor);
                    break;
                case GradientLayer gradientLayer:
                    GradientLayerEditorWindow.Open(gradientLayer, compositor);
                    break;
                case OutlineLayer outlineLayer:
                    OutlineLayerEditorWindow.Open(outlineLayer, compositor);
                    break;
                case SDFLayer sdfLayer:
                    SDFLayerEditorWindow.Open(sdfLayer, compositor);
                    break;
            }
        }

        private Layer GetSelectedLayer()
        {
            return compositor == null ? null : compositor.FindLayer(selectedLayerId);
        }

        private bool GetGroupExpanded(GroupLayer group)
        {
            groupExpansion ??= new Dictionary<string, bool>();
            if (!groupExpansion.TryGetValue(group.Id, out bool expanded))
            {
                expanded = true;
                groupExpansion.Add(group.Id, true);
            }
            return expanded;
        }

        private void ExecuteModelChange(string undoName, Action action)
        {
            if (compositor == null || action == null)
                return;

            Undo.RecordObject(compositor, undoName);
            applyingToolkitChange = true;
            try
            {
                action();
                CommitModelChange();
            }
            finally
            {
                applyingToolkitChange = false;
            }
            RebuildToolkitInterface();
        }

        private void CommitModelChange()
        {
            temporaryDocumentDirty |= !AssetDatabase.Contains(compositor);
            compositor.NormalizeModel();
            compositor.MarkChanged();
            RequestPreview();
        }

        private void RequestPreview(bool immediate = false)
        {
            previewRequested = true;
            previewAt = EditorApplication.timeSinceStartup + (immediate ? 0d : PreviewDelay);
        }

        private void UpdatePreview()
        {
            ReleasePreview();
            previewError = null;
            if (compositor == null)
                return;

            try
            {
                int maxSize = paintingLayer != null ? GetPaintingPreviewMaxSize() : PreviewMaxSize;
                previewTexture = compositor.ComposePreview(maxSize);
            }
            catch (Exception exception)
            {
                previewError = exception.Message;
                Debug.LogException(exception);
            }
            UpdateToolkitPreviewPresentation();
        }

        private int GetPaintingPreviewMaxSize()
        {
            return Mathf.Clamp(
                Mathf.RoundToInt(PreviewMaxSize * paintingPreviewScale),
                Mathf.RoundToInt(PreviewMaxSize * MinimumPaintingPreviewScale),
                PreviewMaxSize);
        }

        private static float ClampPaintingPreviewScale(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return DefaultPaintingPreviewScale;
            return Mathf.Clamp(value, MinimumPaintingPreviewScale, MaximumPaintingPreviewScale);
        }

        private void ReleasePreview()
        {
            if (toolkitPreviewCanvas != null && compositor != null)
            {
                toolkitPreviewCanvas.SetDocument(
                    null,
                    compositor.width,
                    compositor.height,
                    GetSelectedLayer() as DrawingLayer);
            }
            if (previewTexture == null)
                return;
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }

        private TextureCompositor CreateTemporaryCompositor()
        {
            TextureCompositor result = CreateInstance<TextureCompositor>();
            result.name = "Unsaved Texture Compositor";
            result.hideFlags = HideFlags.HideAndDontSave;
            result.NormalizeModel();
            temporaryDocumentDirty = false;
            return result;
        }

        private void SetCompositor(TextureCompositor next)
        {
            if (next == null || next == compositor)
                return;

            FinishPaintingStroke();
            ClearLayerDragData();
            TextureCompositor previous = compositor;
            compositor = next;
            compositor.NormalizeModel();
            selectedLayerId = null;
            temporaryDocumentDirty = false;
            groupExpansion?.Clear();
            RequestPreview(true);
            RebuildToolkitInterface();

            if (previous != null && !AssetDatabase.Contains(previous))
                DestroyImmediate(previous);
        }

        private bool ResolveUnsavedTemporaryDocument()
        {
            if (compositor == null || AssetDatabase.Contains(compositor) || !temporaryDocumentDirty)
                return true;

            int choice = EditorUtility.DisplayDialogComplex(
                "Unsaved Sprite Editor document",
                "Save the current compositor before replacing it?",
                "Save As",
                "Discard",
                "Cancel");
            if (choice == 0)
                return SaveAsAsset();
            return choice == 1;
        }

        private bool SaveAsAsset()
        {
            string defaultName = compositor != null && !string.IsNullOrWhiteSpace(compositor.name)
                ? compositor.name
                : "TextureCompositor";
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Texture Compositor",
                defaultName,
                "asset",
                "Choose a location for the compositor asset.");
            if (string.IsNullOrEmpty(path))
                return false;

            compositor.SyncDrawingLayerTextures();
            TextureCompositor copy = Instantiate(compositor);
            copy.CloneDrawingLayerTextures();
            copy.name = Path.GetFileNameWithoutExtension(path);
            copy.hideFlags = HideFlags.None;
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(copy, path);
            copy.PersistDrawingLayerTextures();
            AssetDatabase.SaveAssets();
            SetCompositor(copy);
            Selection.activeObject = copy;
            EditorGUIUtility.PingObject(copy);
            return true;
        }

        private void ExportTexture()
        {
            string path = EditorUtility.SaveFilePanel("Export Sprite PNG", Application.dataPath, "sprite", "png");
            if (string.IsNullOrEmpty(path))
                return;

            Texture2D texture = null;
            try
            {
                texture = compositor.Compose();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                ImportExportedSpriteIfNeeded(path);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Sprite export failed", exception.Message, "OK");
            }
            finally
            {
                if (texture != null)
                    DestroyImmediate(texture);
            }
        }

        private static void ImportExportedSpriteIfNeeded(string path)
        {
            string fullPath = Path.GetFullPath(path).Replace('\\', '/');
            string assetsPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            if (!fullPath.StartsWith(assetsPath + "/", StringComparison.OrdinalIgnoreCase))
                return;

            string assetPath = "Assets" + fullPath.Substring(assetsPath.Length);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            UnityEngine.Object imported = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (imported == null)
                imported = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Selection.activeObject = imported;
            EditorGUIUtility.PingObject(imported);
        }

        private void OnCompositorChanged(TextureCompositor changedCompositor)
        {
            if (changedCompositor != compositor)
                return;

            RequestPreview();
            if (!applyingToolkitChange &&
                rootVisualElement != null &&
                !IsToolkitValueInteractionActive())
            {
                rootVisualElement.schedule.Execute(RebuildToolkitInterface);
            }
        }

        private void OnUndoRedo()
        {
            if (compositor == null)
                return;
            paintingLayer?.EndStroke();
            paintingLayer = null;
            hasLastPaintingUv = false;
            paintingPointerId = -1;
            compositor.NormalizeModel();
            compositor.InvalidateDrawingLayerSurfaces();
            selectedLayerId = compositor.FindLayer(selectedLayerId)?.Id;
            temporaryDocumentDirty |= !AssetDatabase.Contains(compositor);
            RequestPreview(true);
            RebuildToolkitInterface();
        }
    }
}
