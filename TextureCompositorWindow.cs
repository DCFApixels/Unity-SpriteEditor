using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public sealed class TextureCompositorWindow : EditorWindow
    {
        private const int PreviewMaxSize = 512;
        private const int PaintingPreviewMaxSize = 192;
        private const double PreviewDelay = 0.12d;
        private const double PaintingPreviewInterval = 1d / 30d;
        private const float PreviewPaneMinWidth = 200f;
        private const float SettingsPaneMinWidth = 320f;
        private const float SplitterWidth = 6f;
        private const float PanePadding = 8f;
        private const float GroupDropCenterFraction = 0.5f;
        private const float StandardPreviewHeaderHeight = 28f;
        private const float PaintingPreviewHeaderHeight = 110f;
        private const string DraggedLayerIdKey = "DCFApixels.SpriteEditor.DraggedLayerId";
        private const string DraggedCompositorIdKey = "DCFApixels.SpriteEditor.DraggedCompositorId";
        private static readonly int SplitterControlHash = "DCFApixels.SpriteEditor.Splitter".GetHashCode();
        private static readonly int LayerDragHandleHash = "DCFApixels.SpriteEditor.LayerDragHandle".GetHashCode();
        private static readonly int PaintCanvasControlHash = "DCFApixels.SpriteEditor.PaintCanvas".GetHashCode();
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
            "Continue lets the brush cross each repeated shape boundary. Clip cuts every copy to its own cell or sector.");
        private static readonly GUIContent BrushSpacingContent = new GUIContent(
            "Step",
            "Distance between brush stamps as a percentage of brush size. Larger values are faster and produce a dotted stroke.");

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
        [NonSerialized] private string dragCandidateLayerId;
        [NonSerialized] private DrawingLayer paintingLayer;
        [NonSerialized] private Vector2 lastPaintingUv;
        [NonSerialized] private bool hasLastPaintingUv;
        [NonSerialized] private double nextPaintingPreviewAt;

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
            }

            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            minSize = new Vector2(640f, 420f);
            wantsMouseMove = true;
            groupExpansion = new Dictionary<string, bool>();
            TextureCompositor.Changed += OnCompositorChanged;
            Undo.undoRedoPerformed += OnUndoRedo;

            if (compositor == null)
                SetCompositor(CreateTemporaryCompositor());
            else
                compositor.NormalizeModel();
            RequestPreview(true);
        }

        private void OnDisable()
        {
            FinishPaintingStroke();
            TextureCompositor.Changed -= OnCompositorChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
            ClearLayerDragData();
            ReleasePreview();
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

        private void OnGUI()
        {
            if (compositor == null)
                SetCompositor(CreateTemporaryCompositor());

            if (Event.current.type == EventType.DragExited)
            {
                ClearLayerDragData();
                Repaint();
            }

            HandleBrushSizeHotkeys();

            Rect contentRect = new Rect(0f, 0f, position.width, position.height);
            float maxPreviewWidth = Mathf.Max(
                PreviewPaneMinWidth,
                contentRect.width - SettingsPaneMinWidth - SplitterWidth);
            previewPaneWidth = Mathf.Clamp(previewPaneWidth, PreviewPaneMinWidth, maxPreviewWidth);

            Rect previewRect = new Rect(contentRect.x, contentRect.y, previewPaneWidth, contentRect.height);
            Rect splitterRect = new Rect(previewRect.xMax, contentRect.y, SplitterWidth, contentRect.height);
            Rect settingsRect = new Rect(
                splitterRect.xMax,
                contentRect.y,
                Mathf.Max(0f, contentRect.xMax - splitterRect.xMax),
                contentRect.height);

            DrawPreviewPane(previewRect);
            DrawSettingsPane(settingsRect);
            DrawSplitter(splitterRect, contentRect);
        }

        private void DrawSettingsPane(Rect rect)
        {
            GUILayout.BeginArea(rect);
            DrawDocumentToolbar();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawOutputSettings();
            EditorGUILayout.Space();
            DrawLayerHierarchy();
            EditorGUILayout.Space();
            DrawExportSection();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawDocumentToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                TextureCompositor selected = (TextureCompositor)EditorGUILayout.ObjectField(
                    compositor,
                    typeof(TextureCompositor),
                    false,
                    GUILayout.MinWidth(180f));
                if (selected != null && selected != compositor)
                {
                    if (ResolveUnsavedTemporaryDocument())
                        SetCompositor(selected);
                }

                if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(46f)) &&
                    ResolveUnsavedTemporaryDocument())
                {
                    SetCompositor(CreateTemporaryCompositor());
                }

                if (GUILayout.Button("Save As", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                    SaveAsAsset();
            }

            if (!AssetDatabase.Contains(compositor))
            {
                EditorGUILayout.HelpBox(
                    temporaryDocumentDirty
                        ? "Unsaved compositor. Use Save As to keep this layer tree."
                        : "Temporary compositor. It can be exported directly or saved as an asset.",
                    temporaryDocumentDirty ? MessageType.Warning : MessageType.Info);
            }
        }

        private void DrawOutputSettings()
        {
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            Undo.RecordObject(compositor, "Change Sprite Output Size");
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                compositor.width = EditorGUILayout.IntField("Width", compositor.width);
                compositor.height = EditorGUILayout.IntField("Height", compositor.height);
            }
            if (EditorGUI.EndChangeCheck())
                CommitModelChange();
        }

        private void DrawLayerHierarchy()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Layers (top to bottom)", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(LayerDragHintContent, EditorStyles.miniLabel, GUILayout.Width(42f));
                if (GUILayout.Button("Add", GUILayout.Width(54f)))
                    ShowAddMenuForSelection();

                using (new EditorGUI.DisabledScope(GetSelectedLayer() == null))
                {
                    if (GUILayout.Button("Group", GUILayout.Width(54f)))
                        GroupSelectedLayer();
                }
            }

            if (compositor.layers.Count == 0)
            {
                EditorGUILayout.HelpBox("Add a layer or group to start composing.", MessageType.Info);
                return;
            }

            Undo.RecordObject(compositor, "Edit Sprite Layers");
            EditorGUI.BeginChangeCheck();
            DrawLayerList(compositor.layers, 0);
            if (EditorGUI.EndChangeCheck())
                CommitModelChange();
        }

        private void DrawLayerList(List<Layer> sourceLayers, int depth)
        {
            if (sourceLayers == null)
                return;

            for (int i = 0; i < sourceLayers.Count; i++)
            {
                Layer layer = sourceLayers[i];
                if (layer == null)
                {
                    DrawMissingLayerRow(sourceLayers, i, depth);
                    continue;
                }

                if (layer is GroupLayer group)
                    DrawGroupRow(group, sourceLayers, i, depth);
                else
                    DrawLeafRow(layer, sourceLayers, i, depth);
            }

            DrawContainerEndDropZone(sourceLayers, depth);
        }

        private void DrawGroupRow(GroupLayer group, List<Layer> container, int index, int depth)
        {
            bool expanded = GetGroupExpanded(group);
            bool nextExpanded = expanded;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(depth * 14f);
                DrawLayerDragHandle(group);
                DrawSelectionToggle(group);
                group.enabled = EditorGUILayout.Toggle(group.enabled, GUILayout.Width(18f));
                Rect foldoutRect = GUILayoutUtility.GetRect(
                    14f,
                    EditorGUIUtility.singleLineHeight,
                    GUILayout.Width(14f));
                nextExpanded = EditorGUI.Foldout(foldoutRect, expanded, GUIContent.none, false);
                if (nextExpanded != expanded)
                    groupExpansion[group.Id] = nextExpanded;
                group.layerName = EditorGUILayout.TextField(group.layerName, GUILayout.MinWidth(100f));
                GUILayout.Label($"{group.layers.Count} items", EditorStyles.miniLabel, GUILayout.Width(50f));
                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(24f)))
                    ShowAddMenu(group.layers, 0);
                if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(30f)))
                    ShowLayerContextMenu(group, container, index);
            }

            Rect rowRect = GUILayoutUtility.GetLastRect();
            HandleLayerRowDrop(rowRect, group, container, index, depth);

            if (nextExpanded)
                DrawLayerList(group.layers, depth + 1);
        }

        private void DrawLeafRow(Layer layer, List<Layer> container, int index, int depth)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(depth * 14f);
                DrawLayerDragHandle(layer);
                DrawSelectionToggle(layer);
                layer.enabled = EditorGUILayout.Toggle(layer.enabled, GUILayout.Width(18f));
                DrawLayerThumbnail(layer);
                layer.layerName = EditorGUILayout.TextField(layer.layerName, GUILayout.MinWidth(90f));
                layer.opacity = Mathf.Clamp01(EditorGUILayout.FloatField(layer.opacity, GUILayout.Width(38f)));
                layer.blendMode = (BlendMode)EditorGUILayout.EnumPopup(layer.blendMode, GUILayout.Width(82f));

                if (layer is TargetedLayerEffect effect &&
                    !compositor.HasUsableEffectInput(effect, container, index))
                {
                    string tooltip = effect.inputMode == EffectInputMode.Specific
                        ? "Select an existing non-cyclic target layer or group in the effect settings."
                        : "This effect needs a layer or group directly below it.";
                    GUILayout.Label(new GUIContent("!", tooltip), GUILayout.Width(10f));
                }

                string editLabel = layer is DrawingLayer ? "Paint" : "Edit";
                if (GUILayout.Button(editLabel, EditorStyles.miniButton, GUILayout.Width(38f)))
                {
                    if (layer is DrawingLayer)
                    {
                        selectedLayerId = layer.Id;
                        Repaint();
                    }
                    else
                    {
                        OpenLayerEditor(layer);
                    }
                }
                if (GUILayout.Button("FX", EditorStyles.miniButton, GUILayout.Width(28f)))
                    ModifierEditorWindow.Open(layer, compositor);
                if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(30f)))
                    ShowLayerContextMenu(layer, container, index);
            }
            Rect rowRect = GUILayoutUtility.GetLastRect();
            HandleLayerRowDrop(rowRect, layer, container, index, depth);
        }

        private void DrawSelectionToggle(Layer layer)
        {
            bool selected = layer.Id == selectedLayerId;
            bool next = GUILayout.Toggle(selected, GUIContent.none, EditorStyles.radioButton, GUILayout.Width(14f));
            if (next && !selected)
            {
                selectedLayerId = layer.Id;
                Repaint();
            }
        }

        private void DrawLayerDragHandle(Layer layer)
        {
            Rect handleRect = GUILayoutUtility.GetRect(
                14f,
                EditorGUIUtility.singleLineHeight,
                GUILayout.Width(14f));
            GUI.Label(handleRect, LayerDragHandleContent, EditorStyles.centeredGreyMiniLabel);
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.Pan);

            int controlId = GUIUtility.GetControlID(LayerDragHandleHash, FocusType.Passive, handleRect);
            Event current = Event.current;
            switch (current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (current.button == 0 && handleRect.Contains(current.mousePosition))
                    {
                        GUIUtility.hotControl = controlId;
                        dragCandidateLayerId = layer.Id;
                        selectedLayerId = layer.Id;
                        Repaint();
                        current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId && dragCandidateLayerId == layer.Id)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
                        DragAndDrop.SetGenericData(DraggedLayerIdKey, layer.Id);
                        DragAndDrop.SetGenericData(DraggedCompositorIdKey, compositor);
                        DragAndDrop.StartDrag(string.IsNullOrEmpty(layer.layerName) ? "Layer" : layer.layerName);
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId && current.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        dragCandidateLayerId = null;
                        current.Use();
                    }
                    break;
            }
        }

        private void HandleLayerRowDrop(
            Rect rowRect,
            Layer rowLayer,
            List<Layer> rowContainer,
            int rowIndex,
            int depth)
        {
            Layer draggedLayer = GetDraggedLayer();
            Event current = Event.current;
            if (draggedLayer == null || !rowRect.Contains(current.mousePosition))
                return;

            bool dropIntoGroup = false;
            if (rowLayer is GroupLayer)
            {
                float centerHalfHeight = rowRect.height * GroupDropCenterFraction * 0.5f;
                dropIntoGroup = current.mousePosition.y >= rowRect.center.y - centerHalfHeight &&
                                current.mousePosition.y <= rowRect.center.y + centerHalfHeight;
            }

            List<Layer> destinationContainer;
            int destinationIndex;
            GroupLayer groupToExpand = null;
            bool insertBefore = current.mousePosition.y < rowRect.center.y;
            if (dropIntoGroup)
            {
                groupToExpand = (GroupLayer)rowLayer;
                destinationContainer = groupToExpand.layers;
                destinationIndex = 0;
            }
            else
            {
                destinationContainer = rowContainer;
                destinationIndex = insertBefore ? rowIndex : rowIndex + 1;
            }

            bool canDrop = CanDropLayer(draggedLayer, destinationContainer, destinationIndex);
            if (current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = canDrop ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
                current.Use();
                return;
            }

            if (current.type == EventType.Repaint && canDrop)
            {
                if (dropIntoGroup)
                {
                    EditorGUI.DrawRect(rowRect, GroupDropHighlightColor);
                    DrawBorder(rowRect, DropIndicatorColor);
                }
                else
                {
                    float indicatorY = insertBefore ? rowRect.y : rowRect.yMax - 2f;
                    float indicatorX = rowRect.x + depth * 14f + 4f;
                    EditorGUI.DrawRect(
                        new Rect(indicatorX, indicatorY, Mathf.Max(0f, rowRect.xMax - indicatorX), 2f),
                        DropIndicatorColor);
                }
                return;
            }

            if (current.type != EventType.DragPerform || !canDrop)
                return;

            DragAndDrop.AcceptDrag();
            PerformLayerDrop(draggedLayer, destinationContainer, destinationIndex, groupToExpand);
            ClearLayerDragData();
            current.Use();
            GUIUtility.ExitGUI();
        }

        private void DrawContainerEndDropZone(List<Layer> destinationContainer, int depth)
        {
            Rect dropRect = GUILayoutUtility.GetRect(1f, 8f, GUILayout.ExpandWidth(true));
            Layer draggedLayer = GetDraggedLayer();
            Event current = Event.current;
            if (draggedLayer == null || !dropRect.Contains(current.mousePosition))
                return;

            int destinationIndex = destinationContainer.Count;
            bool canDrop = CanDropLayer(draggedLayer, destinationContainer, destinationIndex);
            if (current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = canDrop ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
                current.Use();
                return;
            }

            if (current.type == EventType.Repaint && canDrop)
            {
                float indicatorX = dropRect.x + depth * 14f + 4f;
                EditorGUI.DrawRect(
                    new Rect(indicatorX, dropRect.center.y - 1f, Mathf.Max(0f, dropRect.xMax - indicatorX), 2f),
                    DropIndicatorColor);
                return;
            }

            if (current.type != EventType.DragPerform || !canDrop)
                return;

            DragAndDrop.AcceptDrag();
            PerformLayerDrop(draggedLayer, destinationContainer, destinationIndex, null);
            ClearLayerDragData();
            current.Use();
            GUIUtility.ExitGUI();
        }

        private Layer GetDraggedLayer()
        {
            TextureCompositor draggedCompositor =
                DragAndDrop.GetGenericData(DraggedCompositorIdKey) as TextureCompositor;
            if (draggedCompositor == null || compositor == null || draggedCompositor != compositor)
            {
                return null;
            }

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
            dragCandidateLayerId = null;
            DragAndDrop.SetGenericData(DraggedLayerIdKey, null);
            DragAndDrop.SetGenericData(DraggedCompositorIdKey, null);
        }

        private static void DrawLayerThumbnail(Layer layer)
        {
            Rect rect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f));
            Texture2D preview = layer.GetPreviewTexture(18);
            if (preview != null)
                GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
            else
                EditorGUI.DrawRect(rect, new Color(0.28f, 0.28f, 0.28f, 1f));
        }

        private void DrawMissingLayerRow(List<Layer> container, int index, int depth)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(depth * 14f + 14f);
                EditorGUILayout.LabelField("Missing layer data", EditorStyles.miniLabel);
                if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(54f)))
                    ExecuteModelChange("Remove Missing Layer", () => container.RemoveAt(index));
            }
        }

        private void DrawPreviewPane(Rect rect)
        {
            DrawingLayer drawingLayer = GetSelectedLayer() as DrawingLayer;
            Color background = EditorGUIUtility.isProSkin
                ? new Color(0.105f, 0.105f, 0.105f, 1f)
                : new Color(0.65f, 0.65f, 0.65f, 1f);
            Color headerBackground = EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.16f, 0.16f, 1f)
                : new Color(0.78f, 0.78f, 0.78f, 1f);
            EditorGUI.DrawRect(rect, background);

            float headerHeight = drawingLayer != null
                ? PaintingPreviewHeaderHeight
                : StandardPreviewHeaderHeight;
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, headerHeight);
            EditorGUI.DrawRect(headerRect, headerBackground);
            if (drawingLayer != null)
                DrawPaintingPreviewHeader(headerRect, drawingLayer);
            else
                DrawStandardPreviewHeader(headerRect);

            Rect footerRect = new Rect(rect.x, rect.yMax - 22f, rect.width, 22f);
            string footerText;
            if (drawingLayer != null)
            {
                footerText = previewTexture != null
                    ? $"Paint on canvas • [ / ] brush size • {drawingLayer.brushSize:0.#} px"
                    : "Rendering painting preview…";
            }
            else
            {
                footerText = previewTexture != null ? "Transparent canvas • auto refresh" : "Rendering preview…";
            }
            GUI.Label(
                footerRect,
                footerText,
                EditorStyles.centeredGreyMiniLabel);

            Rect canvasRect = new Rect(
                rect.x + PanePadding,
                headerRect.yMax + PanePadding,
                Mathf.Max(0f, rect.width - PanePadding * 2f),
                Mathf.Max(0f, footerRect.y - headerRect.yMax - PanePadding * 2f));

            if (!string.IsNullOrEmpty(previewError))
            {
                float errorHeight = Mathf.Min(52f, canvasRect.height);
                EditorGUI.HelpBox(
                    new Rect(canvasRect.x, canvasRect.y, canvasRect.width, errorHeight),
                    previewError,
                    MessageType.Error);
                canvasRect.yMin += errorHeight + PanePadding;
            }

            if (canvasRect.width <= 1f || canvasRect.height <= 1f)
                return;

            float sourceWidth = previewTexture != null
                ? Mathf.Max(1, previewTexture.width)
                : Mathf.Max(1, compositor.width);
            float sourceHeight = previewTexture != null
                ? Mathf.Max(1, previewTexture.height)
                : Mathf.Max(1, compositor.height);
            Rect imageRect = FitRect(canvasRect, sourceWidth / sourceHeight);
            Rect shadowRect = new Rect(imageRect.x + 3f, imageRect.y + 3f, imageRect.width, imageRect.height);
            EditorGUI.DrawRect(shadowRect, new Color(0f, 0f, 0f, 0.32f));
            DrawCheckerboard(imageRect);

            if (previewTexture != null)
                GUI.DrawTexture(imageRect, previewTexture, ScaleMode.StretchToFill, true);
            else
                GUI.Label(imageRect, "Preparing preview…", EditorStyles.centeredGreyMiniLabel);

            DrawBorder(imageRect, EditorGUIUtility.isProSkin
                ? new Color(0.32f, 0.32f, 0.32f, 1f)
                : new Color(0.38f, 0.38f, 0.38f, 1f));

            if (drawingLayer != null)
                HandlePreviewPainting(imageRect, drawingLayer);
        }

        private void DrawStandardPreviewHeader(Rect headerRect)
        {
            GUI.Label(
                new Rect(headerRect.x + 10f, headerRect.y + 4f, 80f, 20f),
                "Preview",
                EditorStyles.boldLabel);

            Rect refreshRect = new Rect(headerRect.xMax - 72f, headerRect.y + 4f, 64f, 20f);
            if (GUI.Button(refreshRect, "Refresh", EditorStyles.miniButton))
                RequestPreview(true);

            string dimensions = $"{Mathf.Max(1, compositor.width)} × {Mathf.Max(1, compositor.height)}";
            Rect dimensionsRect = new Rect(
                headerRect.x + 92f,
                headerRect.y + 4f,
                Mathf.Max(0f, refreshRect.x - headerRect.x - 100f),
                20f);
            GUI.Label(dimensionsRect, dimensions, EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawPaintingPreviewHeader(Rect headerRect, DrawingLayer layer)
        {
            PaintToolMode nextTool = layer.tool;
            Color nextColor = layer.brushColor;
            float nextSize = layer.brushSize;
            float nextHardness = layer.brushHardness;
            float nextSpacingPercent = layer.brushSpacing * 100f;
            bool nextMirrorVertical = layer.mirrorAcrossVerticalAxis;
            bool nextMirrorHorizontal = layer.mirrorAcrossHorizontalAxis;
            Vector2 nextCenter = layer.patternCenter;
            PaintRepeatMode nextRepeat = layer.repeatMode;
            PaintRepeatElementMode nextElementMode = layer.repeatElementMode;
            PaintRepeatBoundaryMode nextBoundary = layer.repeatBoundaryMode;
            int nextCount = layer.repeatCount;
            int nextSecondaryCount = layer.repeatSecondaryCount;
            bool clearRequested = false;

            GUILayout.BeginArea(headerRect);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(22f)))
            {
                GUILayout.Label("Preview • Drawing", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(42f)))
                    clearRequested = true;
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(56f)))
                    RequestPreview(true);
            }

            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(22f)))
            {
                nextTool = (PaintToolMode)EditorGUILayout.EnumPopup(nextTool, GUILayout.Width(62f));
                using (new EditorGUI.DisabledScope(nextTool == PaintToolMode.Eraser))
                {
                    nextColor = EditorGUILayout.ColorField(
                        GUIContent.none,
                        nextColor,
                        true,
                        true,
                        true,
                        GUILayout.Width(42f));
                }
                GUILayout.Label("Size", GUILayout.Width(27f));
                nextSize = EditorGUILayout.FloatField(nextSize, GUILayout.Width(42f));
                GUILayout.Label("Hard", GUILayout.Width(30f));
                nextHardness = GUILayout.HorizontalSlider(nextHardness, 0f, 1f, GUILayout.MinWidth(40f));
                GUILayout.Label($"{nextHardness * 100f:0}%", EditorStyles.miniLabel, GUILayout.Width(34f));
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(22f)))
            {
                nextMirrorVertical = GUILayout.Toggle(
                    nextMirrorVertical,
                    MirrorVerticalContent,
                    EditorStyles.toolbarButton,
                    GUILayout.Width(68f));
                nextMirrorHorizontal = GUILayout.Toggle(
                    nextMirrorHorizontal,
                    MirrorHorizontalContent,
                    EditorStyles.toolbarButton,
                    GUILayout.Width(68f));
                GUILayout.Label("Center", GUILayout.Width(38f));
                nextCenter = EditorGUILayout.Vector2Field(GUIContent.none, nextCenter, GUILayout.MinWidth(78f));
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(22f)))
            {
                GUILayout.Label("Repeat", GUILayout.Width(42f));
                nextRepeat = (PaintRepeatMode)EditorGUILayout.EnumPopup(nextRepeat, GUILayout.Width(78f));
                if (nextRepeat == PaintRepeatMode.Grid)
                {
                    GUILayout.Label("X", GUILayout.Width(12f));
                    nextCount = EditorGUILayout.IntField(nextCount, GUILayout.Width(30f));
                    GUILayout.Label("Y", GUILayout.Width(12f));
                    nextSecondaryCount = EditorGUILayout.IntField(nextSecondaryCount, GUILayout.Width(30f));
                }
                else if (nextRepeat != PaintRepeatMode.None)
                {
                    GUILayout.Label("Count", GUILayout.Width(36f));
                    nextCount = EditorGUILayout.IntField(nextCount, GUILayout.Width(34f));
                }
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(nextRepeat == PaintRepeatMode.None))
                {
                    nextElementMode = (PaintRepeatElementMode)EditorGUILayout.EnumPopup(
                        nextElementMode,
                        GUILayout.Width(108f));
                }
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(22f)))
            {
                GUILayout.Label(RepeatBoundaryContent, GUILayout.Width(36f));
                using (new EditorGUI.DisabledScope(nextRepeat == PaintRepeatMode.None))
                {
                    nextBoundary = (PaintRepeatBoundaryMode)EditorGUILayout.EnumPopup(
                        nextBoundary,
                        GUILayout.Width(76f));
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label(BrushSpacingContent, GUILayout.Width(28f));
                nextSpacingPercent = EditorGUILayout.FloatField(nextSpacingPercent, GUILayout.Width(38f));
                GUILayout.Label("%", EditorStyles.miniLabel, GUILayout.Width(12f));
            }
            bool changed = EditorGUI.EndChangeCheck();
            GUILayout.EndArea();

            if (changed)
            {
                ExecuteModelChange("Change Drawing Tool", () =>
                {
                    layer.tool = nextTool;
                    layer.brushColor = nextColor;
                    layer.brushSize = Mathf.Max(1f, nextSize);
                    layer.brushHardness = Mathf.Clamp01(nextHardness);
                    layer.brushSpacing = Mathf.Clamp(
                        nextSpacingPercent * 0.01f,
                        DrawingLayer.MinimumBrushSpacing,
                        DrawingLayer.MaximumBrushSpacing);
                    layer.mirrorAcrossVerticalAxis = nextMirrorVertical;
                    layer.mirrorAcrossHorizontalAxis = nextMirrorHorizontal;
                    layer.patternCenter = new Vector2(
                        Mathf.Clamp01(nextCenter.x),
                        Mathf.Clamp01(nextCenter.y));
                    layer.repeatMode = nextRepeat;
                    layer.repeatElementMode = nextElementMode;
                    layer.repeatBoundaryMode = nextBoundary;
                    layer.repeatCount = Mathf.Clamp(nextCount, 2, 64);
                    layer.repeatSecondaryCount = Mathf.Clamp(nextSecondaryCount, 2, 64);
                });
            }

            if (clearRequested)
                ClearDrawingLayer(layer);
        }

        private void HandlePreviewPainting(Rect imageRect, DrawingLayer layer)
        {
            Event current = Event.current;
            int controlId = GUIUtility.GetControlID(PaintCanvasControlHash, FocusType.Passive, imageRect);
            bool pointerInside = imageRect.Contains(current.mousePosition);

            if (current.type == EventType.Repaint)
            {
                DrawPaintingGuides(imageRect, layer);
                if (pointerInside && !current.alt)
                    DrawBrushCursor(imageRect, layer, current.mousePosition);
            }

            switch (current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (current.button != 0 || current.alt || !pointerInside)
                        break;
                    if (!TryMapPreviewToLayerUv(current.mousePosition, imageRect, layer, out Vector2 startUv))
                        break;

                    Focus();
                    GUI.FocusControl(null);
                    GUIUtility.hotControl = controlId;
                    paintingLayer = layer;
                    lastPaintingUv = startUv;
                    hasLastPaintingUv = true;
                    Undo.RecordObject(compositor, "Paint Stroke");
                    layer.PrepareStroke(compositor.width, compositor.height, "Paint Stroke");
                    layer.PaintPoint(startUv, compositor.width, compositor.height);
                    RefreshPreviewDuringPainting();
                    current.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlId || paintingLayer == null)
                        break;
                    if (TryMapPreviewToLayerUv(current.mousePosition, imageRect, paintingLayer, out Vector2 dragUv))
                    {
                        if (hasLastPaintingUv)
                        {
                            paintingLayer.PaintSegment(
                                lastPaintingUv,
                                dragUv,
                                compositor.width,
                                compositor.height,
                                false);
                        }
                        else
                        {
                            paintingLayer.PaintPoint(dragUv, compositor.width, compositor.height);
                        }
                        lastPaintingUv = dragUv;
                        hasLastPaintingUv = true;
                        RefreshPreviewDuringPainting();
                    }
                    else
                    {
                        hasLastPaintingUv = false;
                    }
                    current.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != controlId || current.button != 0)
                        break;
                    GUIUtility.hotControl = 0;
                    FinishPaintingStroke();
                    current.Use();
                    break;

                case EventType.MouseMove:
                    if (pointerInside)
                        Repaint();
                    break;
            }
        }

        private void ClearDrawingLayer(DrawingLayer layer)
        {
            if (layer == null)
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
            Repaint();
        }

        private void FinishPaintingStroke()
        {
            if (paintingLayer == null)
                return;

            paintingLayer.SyncSurfaceToTexture();
            paintingLayer = null;
            hasLastPaintingUv = false;
            nextPaintingPreviewAt = 0d;
            temporaryDocumentDirty |= compositor != null && !AssetDatabase.Contains(compositor);
            if (compositor != null)
                compositor.MarkChanged();
            Undo.FlushUndoRecordObjects();
            RequestPreview(true);
        }

        private void HandleBrushSizeHotkeys()
        {
            Event current = Event.current;
            if (current.type != EventType.KeyDown || EditorGUIUtility.editingTextField)
                return;

            bool decrease = current.keyCode == KeyCode.LeftBracket || current.character == '[';
            bool increase = current.keyCode == KeyCode.RightBracket || current.character == ']';
            if (!decrease && !increase)
                return;
            if (!(GetSelectedLayer() is DrawingLayer layer))
                return;

            float nextSize = decrease
                ? Mathf.Max(1f, Mathf.Round(layer.brushSize / 1.2f))
                : Mathf.Max(1f, Mathf.Round(layer.brushSize * 1.2f));
            if (Mathf.Approximately(nextSize, layer.brushSize))
                nextSize = Mathf.Max(1f, layer.brushSize + (increase ? 1f : -1f));

            ExecuteModelChange("Change Brush Size", () => layer.brushSize = nextSize);
            current.Use();
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

        private void DrawBrushCursor(Rect imageRect, DrawingLayer layer, Vector2 mousePosition)
        {
            float pixelScale = imageRect.width / Mathf.Max(1f, compositor.width);
            float transformScale = (Mathf.Abs(layer.transform.scale.x) + Mathf.Abs(layer.transform.scale.y)) * 0.5f;
            float radius = Mathf.Max(2f, layer.brushSize * pixelScale * Mathf.Max(0.0001f, transformScale) * 0.5f);
            Handles.BeginGUI();
            Color previous = Handles.color;
            Handles.color = new Color(0f, 0f, 0f, 0.9f);
            Handles.DrawWireDisc(mousePosition, Vector3.forward, radius + 1f);
            Handles.color = layer.tool == PaintToolMode.Eraser
                ? new Color(1f, 0.35f, 0.25f, 1f)
                : new Color(1f, 1f, 1f, 0.95f);
            Handles.DrawWireDisc(mousePosition, Vector3.forward, radius);
            Handles.color = previous;
            Handles.EndGUI();
        }

        private void DrawPaintingGuides(Rect imageRect, DrawingLayer layer)
        {
            if (!layer.mirrorAcrossVerticalAxis &&
                !layer.mirrorAcrossHorizontalAxis &&
                layer.repeatMode == PaintRepeatMode.None)
            {
                return;
            }

            Handles.BeginGUI();
            Color previous = Handles.color;
            Handles.color = new Color(0.20f, 0.70f, 1f, 0.45f);
            if (layer.mirrorAcrossVerticalAxis)
            {
                float x = imageRect.x + imageRect.width * layer.patternCenter.x;
                Handles.DrawLine(new Vector3(x, imageRect.y), new Vector3(x, imageRect.yMax));
            }
            if (layer.mirrorAcrossHorizontalAxis)
            {
                float y = imageRect.y + imageRect.height * (1f - layer.patternCenter.y);
                Handles.DrawLine(new Vector3(imageRect.x, y), new Vector3(imageRect.xMax, y));
            }

            int count = Mathf.Clamp(layer.repeatCount, 2, 64);
            if (layer.repeatMode == PaintRepeatMode.Horizontal || layer.repeatMode == PaintRepeatMode.Grid)
            {
                for (int xIndex = 1; xIndex < count; xIndex++)
                {
                    float x = Mathf.Lerp(imageRect.x, imageRect.xMax, (float)xIndex / count);
                    Handles.DrawLine(new Vector3(x, imageRect.y), new Vector3(x, imageRect.yMax));
                }
            }
            if (layer.repeatMode == PaintRepeatMode.Vertical || layer.repeatMode == PaintRepeatMode.Grid)
            {
                int verticalCount = layer.repeatMode == PaintRepeatMode.Grid
                    ? Mathf.Clamp(layer.repeatSecondaryCount, 2, 64)
                    : count;
                for (int yIndex = 1; yIndex < verticalCount; yIndex++)
                {
                    float y = Mathf.Lerp(imageRect.y, imageRect.yMax, (float)yIndex / verticalCount);
                    Handles.DrawLine(new Vector3(imageRect.x, y), new Vector3(imageRect.xMax, y));
                }
            }
            if (layer.repeatMode == PaintRepeatMode.Radial)
            {
                Vector2 center = new Vector2(
                    imageRect.x + imageRect.width * layer.patternCenter.x,
                    imageRect.y + imageRect.height * (1f - layer.patternCenter.y));
                float length = Mathf.Sqrt(imageRect.width * imageRect.width + imageRect.height * imageRect.height);
                for (int sector = 0; sector < count; sector++)
                {
                    float angle = -Mathf.PI + sector * Mathf.PI * 2f / count;
                    Vector2 direction = new Vector2(
                        Mathf.Cos(angle) * imageRect.width,
                        -Mathf.Sin(angle) * imageRect.height).normalized;
                    Handles.DrawLine(center, center + direction * length);
                }
            }
            Handles.color = previous;
            Handles.EndGUI();
        }

        private void DrawSplitter(Rect splitterRect, Rect contentRect)
        {
            int controlId = GUIUtility.GetControlID(SplitterControlHash, FocusType.Passive, splitterRect);
            Event current = Event.current;
            switch (current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (current.button == 0 && splitterRect.Contains(current.mousePosition))
                    {
                        GUIUtility.hotControl = controlId;
                        current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        float maxWidth = Mathf.Max(
                            PreviewPaneMinWidth,
                            contentRect.width - SettingsPaneMinWidth - SplitterWidth);
                        previewPaneWidth = Mathf.Clamp(
                            current.mousePosition.x - contentRect.x,
                            PreviewPaneMinWidth,
                            maxWidth);
                        GUI.changed = true;
                        Repaint();
                        current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId && current.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }
                    break;
            }

            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            bool highlighted = GUIUtility.hotControl == controlId || splitterRect.Contains(current.mousePosition);
            EditorGUI.DrawRect(
                splitterRect,
                highlighted
                    ? new Color(0.20f, 0.52f, 0.82f, 1f)
                    : new Color(0.10f, 0.10f, 0.10f, 1f));

            float gripY = splitterRect.center.y - 14f;
            Color gripColor = new Color(1f, 1f, 1f, highlighted ? 0.8f : 0.35f);
            EditorGUI.DrawRect(new Rect(splitterRect.center.x - 1f, gripY, 1f, 28f), gripColor);
            EditorGUI.DrawRect(new Rect(splitterRect.center.x + 1f, gripY, 1f, 28f), gripColor);
        }

        private static Rect FitRect(Rect container, float aspect)
        {
            aspect = Mathf.Max(0.0001f, aspect);
            float width = container.width;
            float height = width / aspect;
            if (height > container.height)
            {
                height = container.height;
                width = height * aspect;
            }

            return new Rect(
                container.x + (container.width - width) * 0.5f,
                container.y + (container.height - height) * 0.5f,
                width,
                height);
        }

        private static void DrawCheckerboard(Rect rect)
        {
            const float tileSize = 12f;
            Color light = EditorGUIUtility.isProSkin
                ? new Color(0.30f, 0.30f, 0.30f, 1f)
                : new Color(0.82f, 0.82f, 0.82f, 1f);
            Color dark = EditorGUIUtility.isProSkin
                ? new Color(0.23f, 0.23f, 0.23f, 1f)
                : new Color(0.70f, 0.70f, 0.70f, 1f);

            int rows = Mathf.CeilToInt(rect.height / tileSize);
            int columns = Mathf.CeilToInt(rect.width / tileSize);
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    Rect tile = new Rect(
                        rect.x + column * tileSize,
                        rect.y + row * tileSize,
                        Mathf.Min(tileSize, rect.xMax - (rect.x + column * tileSize)),
                        Mathf.Min(tileSize, rect.yMax - (rect.y + row * tileSize)));
                    EditorGUI.DrawRect(tile, ((row + column) & 1) == 0 ? light : dark);
                }
            }
        }

        private static void DrawBorder(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private void DrawExportSection()
        {
            if (GUILayout.Button("Export PNG"))
                ExportTexture();
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
            if (!compositor.TryFindParentGroup(container, out GroupLayer parent, out List<Layer> parentContainer, out int parentIndex))
                return;
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
            Undo.RecordObject(compositor, undoName);
            action();
            CommitModelChange();
        }

        private void CommitModelChange()
        {
            temporaryDocumentDirty |= !AssetDatabase.Contains(compositor);
            compositor.MarkChanged();
            RequestPreview();
            Repaint();
        }

        private void RequestPreview(bool immediate = false)
        {
            previewRequested = true;
            previewAt = EditorApplication.timeSinceStartup + (immediate ? 0d : PreviewDelay);
            Repaint();
        }

        private void UpdatePreview()
        {
            ReleasePreview();
            previewError = null;
            if (compositor == null)
                return;

            try
            {
                int maxSize = paintingLayer != null ? PaintingPreviewMaxSize : PreviewMaxSize;
                previewTexture = compositor.ComposePreview(maxSize);
            }
            catch (Exception exception)
            {
                previewError = exception.Message;
                Debug.LogException(exception);
            }
            Repaint();
        }

        private void ReleasePreview()
        {
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
            if (changedCompositor == compositor)
                RequestPreview();
        }

        private void OnUndoRedo()
        {
            if (compositor == null)
                return;
            paintingLayer = null;
            hasLastPaintingUv = false;
            GUIUtility.hotControl = 0;
            compositor.NormalizeModel();
            compositor.InvalidateDrawingLayerSurfaces();
            selectedLayerId = compositor.FindLayer(selectedLayerId)?.Id;
            temporaryDocumentDirty |= !AssetDatabase.Contains(compositor);
            RequestPreview(true);
            Repaint();
        }
    }
}
