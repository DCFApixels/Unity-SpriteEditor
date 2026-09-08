using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        private const float ToolkitPreviewHeaderRowHeight = 24f;
        private const float ToolkitLayerRowHeight = 26f;
        private const float ToolkitLayerIndent = 14f;

        [NonSerialized] private VisualElement toolkitPreviewPane;
        [NonSerialized] private VisualElement toolkitPreviewHeader;
        [NonSerialized] private VisualElement toolkitCanvasToolbar;
        [NonSerialized] private VisualElement toolkitPreviewActions;
        [NonSerialized] private VisualElement toolkitDocumentRoot;
        [NonSerialized] private ScrollView toolkitSettingsScroll;
        [NonSerialized] private VisualElement toolkitLayerFooter;
        [NonSerialized] private SpritePreviewElement toolkitPreviewCanvas;
        [NonSerialized] private Label toolkitPreviewFooter;
        [NonSerialized] private VisualElement toolkitPreviewErrorRoot;
        [NonSerialized] private ObjectField toolkitDocumentField;
        [NonSerialized] private VisualElement toolkitLayerHierarchyRoot;
        [NonSerialized] private VisualElement activeDropElement;
        [NonSerialized] private StyleLength activeDropMarginLeft;
        [NonSerialized] private LayerDragManipulator activeLayerDrag;
        [NonSerialized] private int paintingPointerId = -1;
        [NonSerialized] private bool applyingToolkitChange;
        [NonSerialized] private bool rebuildingToolkit;
        [NonSerialized] private bool toolkitRefreshRequested;
        [NonSerialized] private TextureCompositor toolkitBoundDocument;
        [NonSerialized] private DrawingLayer toolkitHeaderLayer;
        [NonSerialized] private bool toolkitHeaderBuilt;
        [NonSerialized] private HelpBox toolkitDocumentStatus;
        [NonSerialized] private HelpBox toolkitPreviewError;
        private readonly SpriteEditorUI.ValueBindings toolkitSettingsBindings = new SpriteEditorUI.ValueBindings();
        private readonly SpriteEditorUI.ValueBindings toolkitHeaderBindings = new SpriteEditorUI.ValueBindings();
        private readonly SpriteEditorUI.ValueBindings toolkitLayerBindings = new SpriteEditorUI.ValueBindings();
        private readonly List<LayerTreeEntry> toolkitLayerTree = new List<LayerTreeEntry>();
        private readonly List<LayerTreeEntry> toolkitNextLayerTree = new List<LayerTreeEntry>();

        private readonly struct LayerTreeEntry : IEquatable<LayerTreeEntry>
        {
            public readonly List<Layer> Container;
            public readonly Layer Layer;
            public readonly int Index;
            public readonly int Depth;

            public LayerTreeEntry(List<Layer> container, Layer layer, int index, int depth)
            {
                Container = container;
                Layer = layer;
                Index = index;
                Depth = depth;
            }

            public bool Equals(LayerTreeEntry other) =>
                ReferenceEquals(Container, other.Container) && ReferenceEquals(Layer, other.Layer) &&
                Index == other.Index && Depth == other.Depth;
        }

        public void CreateGUI()
        {
            FinishPreviewTransform();
            if (compositor == null)
                SetCompositor(CreateTemporaryCompositor());

            VisualElement root = rootVisualElement;
            root.UnregisterCallback<KeyDownEvent>(OnToolkitKeyDown, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyUpEvent>(OnToolkitKeyUp, TrickleDown.TrickleDown);
            root.UnregisterCallback<DragExitedEvent>(OnToolkitDragExited);
            root.Clear();
            SpriteEditorUI.ApplyWindowStyles(root);
            toolkitBoundDocument = null;
            toolkitHeaderBuilt = false;
            toolkitSettingsBindings.Clear();
            toolkitHeaderBindings.Clear();
            toolkitLayerBindings.Clear();
            ResetToolkitLayerInspector();
            toolkitLayerTree.Clear();
            root.focusable = true;
            root.style.flexGrow = 1f;
            root.style.backgroundColor = SpriteEditorUI.PanelColor;
            root.RegisterCallback<KeyDownEvent>(OnToolkitKeyDown, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyUpEvent>(OnToolkitKeyUp, TrickleDown.TrickleDown);
            root.RegisterCallback<DragExitedEvent>(OnToolkitDragExited);

            toolkitDocumentRoot = new VisualElement();
            toolkitDocumentRoot.style.flexShrink = 0f;
            root.Add(toolkitDocumentRoot);

            if (settingsPaneWidth <= 0f)
                settingsPaneWidth = Mathf.Max(SettingsPaneMinWidth, position.width - previewPaneWidth - 3f);
            TwoPaneSplitView split = new TwoPaneSplitView(
                1,
                Mathf.Max(SettingsPaneMinWidth, settingsPaneWidth),
                TwoPaneSplitViewOrientation.Horizontal);
            SpriteEditorUI.StyleSplitView(split);
            split.style.flexGrow = 1f;
            split.style.minHeight = 0f;
            root.Add(split);

            toolkitPreviewPane = BuildToolkitPreviewPane();
            toolkitPreviewPane.style.minWidth = PreviewPaneMinWidth;
            split.Add(toolkitPreviewPane);

            VisualElement settingsPane = new VisualElement();
            settingsPane.style.minWidth = SettingsPaneMinWidth;
            settingsPane.style.minHeight = 0f;
            settingsPane.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.newRect.width >= SettingsPaneMinWidth)
                    settingsPaneWidth = evt.newRect.width;
            });
            settingsPane.AddToClassList("sprite-editor-settings-pane");
            settingsPane.EnableInClassList("sprite-editor-settings-pane--light", !EditorGUIUtility.isProSkin);
            split.Add(settingsPane);

            TwoPaneSplitView settingsSplit = new TwoPaneSplitView(
                0, Mathf.Max(100f, layerSettingsPaneHeight), TwoPaneSplitViewOrientation.Vertical);
            SpriteEditorUI.StyleSplitView(settingsSplit);
            settingsSplit.name = "layer-settings-split";
            settingsSplit.style.flexGrow = 1f;
            settingsSplit.style.minHeight = 0f;
            settingsPane.Add(settingsSplit);

            toolkitLayerSettingsScroll = new ScrollView(ScrollViewMode.Vertical);
            toolkitLayerSettingsScroll.name = "selected-layer-settings";
            toolkitLayerSettingsScroll.style.minHeight = 100f;
            toolkitLayerSettingsScroll.style.paddingLeft = 8f;
            toolkitLayerSettingsScroll.style.paddingRight = 8f;
            toolkitLayerSettingsScroll.style.paddingBottom = 8f;
            toolkitLayerSettingsScroll.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.newRect.height >= 100f)
                    layerSettingsPaneHeight = evt.newRect.height;
            });
            settingsSplit.Add(toolkitLayerSettingsScroll);

            VisualElement layersPane = new VisualElement();
            layersPane.AddToClassList("sprite-editor-layers-pane");
            settingsSplit.Add(layersPane);

            toolkitSettingsScroll = new ScrollView(ScrollViewMode.Vertical);
            toolkitSettingsScroll.name = "layer-list";
            toolkitSettingsScroll.style.minHeight = 0f;
            toolkitSettingsScroll.style.flexGrow = 1f;
            toolkitSettingsScroll.style.paddingLeft = 8f;
            toolkitSettingsScroll.style.paddingRight = 8f;
            toolkitSettingsScroll.style.paddingBottom = 8f;
            layersPane.Add(toolkitSettingsScroll);

            toolkitLayerFooter = new VisualElement { name = "layersFooter" };
            toolkitLayerFooter.AddToClassList("sprite-editor-layers-footer");
            toolkitLayerFooter.EnableInClassList("sprite-editor-layers-footer--light", !EditorGUIUtility.isProSkin);
            layersPane.Add(toolkitLayerFooter);

            RefreshToolkitInterface();
        }

        private void OnToolkitDragExited(DragExitedEvent evt)
        {
            ClearToolkitDropIndicator();
            ClearLayerDragData();
        }

        private VisualElement BuildToolkitPreviewPane()
        {
            VisualElement pane = new VisualElement();
            pane.style.flexDirection = FlexDirection.Column;
            pane.style.backgroundColor = SpriteEditorUI.PanelColor;

            toolkitCanvasToolbar = SpriteEditorUI.CreateToolbar();
            toolkitCanvasToolbar.AddToClassList("sprite-editor-canvas-toolbar");
            pane.Add(toolkitCanvasToolbar);

            toolkitPreviewHeader = new VisualElement();
            toolkitPreviewHeader.style.flexShrink = 0f;
            pane.Add(toolkitPreviewHeader);

            toolkitPreviewErrorRoot = new VisualElement();
            toolkitPreviewErrorRoot.style.flexShrink = 0f;
            toolkitPreviewErrorRoot.style.paddingLeft = PanePadding;
            toolkitPreviewErrorRoot.style.paddingRight = PanePadding;
            pane.Add(toolkitPreviewErrorRoot);
            toolkitPreviewError = SpriteEditorUI.AddHelpBox(toolkitPreviewErrorRoot, string.Empty, HelpBoxMessageType.Error);
            toolkitPreviewError.style.display = DisplayStyle.None;

            toolkitPreviewCanvas = new SpritePreviewElement();
            toolkitPreviewCanvas.style.flexGrow = 1f;
            toolkitPreviewCanvas.style.marginLeft = PanePadding;
            toolkitPreviewCanvas.style.marginRight = PanePadding;
            toolkitPreviewCanvas.style.marginTop = PanePadding;
            toolkitPreviewCanvas.style.marginBottom = PanePadding;
            BuildPreviewTransformTool();
            toolkitPreviewCanvas.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
            toolkitPreviewCanvas.RegisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
            toolkitPreviewCanvas.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);
            toolkitPreviewCanvas.RegisterCallback<PointerEnterEvent>(OnPreviewPointerEnter);
            toolkitPreviewCanvas.RegisterCallback<PointerLeaveEvent>(OnPreviewPointerLeave);
            toolkitPreviewCanvas.RegisterCallback<PointerCaptureOutEvent>(OnPreviewPointerCaptureOut);
            pane.Add(toolkitPreviewCanvas);

            toolkitPreviewFooter = new Label();
            toolkitPreviewFooter.style.height = 22f;
            toolkitPreviewFooter.style.flexShrink = 0f;
            toolkitPreviewFooter.style.unityTextAlign = TextAnchor.MiddleCenter;
            toolkitPreviewFooter.style.fontSize = 10f;
            pane.Add(toolkitPreviewFooter);
            return pane;
        }

        private void RefreshToolkitInterface(bool forceValues = false)
        {
            if (rootVisualElement == null || toolkitDocumentRoot == null || rebuildingToolkit)
                return;

            rebuildingToolkit = true;
            try
            {
                toolkitRefreshRequested = false;
                NormalizeLayerSelection();
                if (toolkitBoundDocument != compositor)
                {
                    toolkitBoundDocument = compositor;
                    toolkitSettingsBindings.Clear();
                    toolkitLayerBindings.Clear();
                    toolkitLayerTree.Clear();
                    toolkitHeaderBuilt = false;
                    BuildToolkitDocumentArea();
                    BuildToolkitCanvasToolbar();
                    BuildToolkitSettings();
                }
                toolkitSettingsBindings.Refresh(forceValues);
                RefreshToolkitLayerHierarchy(forceValues);
                RefreshToolkitLayerInspector(forceValues);
                RefreshToolkitPreviewHeader(forceValues);
                UpdateToolkitPreviewPresentation();
            }
            finally
            {
                rebuildingToolkit = false;
            }
        }

        private void BuildToolkitDocumentArea()
        {
            toolkitDocumentRoot.Clear();
            VisualElement toolbar = SpriteEditorUI.CreateToolbar();
            toolbar.style.backgroundColor = StyleKeyword.Null;
            toolbar.style.borderBottomWidth = StyleKeyword.Null;
            toolbar.AddToClassList("sprite-editor-document-header");
            toolbar.EnableInClassList("sprite-editor-document-header--light", !EditorGUIUtility.isProSkin);

            toolbar.Add(SpriteEditorUI.CreateToolbarButton("New", () =>
            {
                if (!ResolveUnsavedTemporaryDocument())
                    return;
                SetCompositor(CreateTemporaryCompositor());
            }, 46f));

            toolkitDocumentField = new ObjectField
            {
                objectType = typeof(TextureCompositor),
                allowSceneObjects = false
            };
            toolkitDocumentField.style.flexGrow = 1f;
            toolkitDocumentField.style.minWidth = 140f;
            toolkitDocumentField.SetValueWithoutNotify(compositor);
            toolkitDocumentField.RegisterValueChangedCallback(evt =>
            {
                TextureCompositor selected = evt.newValue as TextureCompositor;
                if (selected == null || selected == compositor)
                {
                    toolkitDocumentField.SetValueWithoutNotify(compositor);
                    return;
                }

                if (ResolveUnsavedTemporaryDocument())
                {
                    SetCompositor(selected);
                }
                else
                {
                    toolkitDocumentField.SetValueWithoutNotify(compositor);
                }
            });
            toolbar.Add(toolkitDocumentField);
            Button save = SpriteEditorUI.CreateToolbarButton("Save", SaveAsset, 46f);
            save.tooltip = "Save this document to its existing asset (Ctrl+S).";
            save.SetEnabled(compositor != null && AssetDatabase.Contains(compositor));
            toolkitSettingsBindings.Add(() => save.SetEnabled(compositor != null && AssetDatabase.Contains(compositor)));
            toolbar.Add(save);
            toolbar.Add(SpriteEditorUI.CreateToolbarButton("Save As", () =>
            {
                SaveAsAsset();
            }, 64f));
            Button export = SpriteEditorUI.CreateToolbarButton("Export", ShowExportMenu, 64f);
            export.tooltip = "Export the flattened texture as PNG, JPEG, TGA, EXR, or a Unity Texture2D asset.";
            toolbar.Add(export);
            toolkitDocumentRoot.Add(toolbar);

            toolkitDocumentStatus = SpriteEditorUI.AddHelpBox(toolkitDocumentRoot, string.Empty, HelpBoxMessageType.Info);
            toolkitSettingsBindings.Add(() =>
            {
                toolkitDocumentStatus.style.display = AssetDatabase.Contains(compositor) ? DisplayStyle.None : DisplayStyle.Flex;
                toolkitDocumentStatus.text = temporaryDocumentDirty
                    ? "Unsaved compositor. Use Save As to keep this layer tree."
                    : "Temporary compositor. It can be exported directly or saved as an asset.";
                toolkitDocumentStatus.messageType = temporaryDocumentDirty ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info;
            });

            VisualElement separator = new VisualElement
            {
                name = "documentHeaderSeparator",
                pickingMode = PickingMode.Ignore
            };
            separator.AddToClassList("sprite-editor-document-separator");
            separator.EnableInClassList("sprite-editor-document-separator--light", !EditorGUIUtility.isProSkin);
            toolkitDocumentRoot.Add(separator);
        }

        private void BuildToolkitCanvasToolbar()
        {
            toolkitCanvasToolbar.Clear();
            Label title = new Label("Canvas");
            title.AddToClassList("sprite-editor-canvas-title");
            toolkitCanvasToolbar.Add(title);

            IntegerField width = new IntegerField("W") { isDelayed = true };
            width.AddToClassList("sprite-editor-canvas-size");
            width.tooltip = "Canvas width in pixels. Press Enter or leave the field to apply.";
            width.SetValueWithoutNotify(compositor.width);
            toolkitSettingsBindings.Track(width, () => compositor.width);
            width.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Sprite Canvas Width",
                () => compositor.width = Mathf.Max(1, evt.newValue)));
            toolkitCanvasToolbar.Add(width);
            toolkitCanvasToolbar.Add(new Label("×") { pickingMode = PickingMode.Ignore });

            IntegerField height = new IntegerField("H") { isDelayed = true };
            height.AddToClassList("sprite-editor-canvas-size");
            height.tooltip = "Canvas height in pixels. Press Enter or leave the field to apply.";
            height.SetValueWithoutNotify(compositor.height);
            toolkitSettingsBindings.Track(height, () => compositor.height);
            height.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Sprite Canvas Height",
                () => compositor.height = Mathf.Max(1, evt.newValue)));
            toolkitCanvasToolbar.Add(height);

            toolkitPreviewActions = new VisualElement();
            toolkitPreviewActions.AddToClassList("sprite-editor-preview-actions");
            toolkitCanvasToolbar.Add(toolkitPreviewActions);
        }

        private void BuildToolkitSettings()
        {
            toolkitSettingsScroll.Clear();
            VisualElement layerHeader = SpriteEditorUI.CreateRow();
            layerHeader.style.marginTop = 8f;
            Label title = SpriteEditorUI.CreateHeading("Layers");
            title.style.flexGrow = 1f;
            title.style.marginTop = 0f;
            layerHeader.Add(title);
            toolkitSettingsScroll.Add(layerHeader);

            toolkitLayerHierarchyRoot = new VisualElement();
            toolkitLayerHierarchyRoot.style.flexShrink = 0f;
            toolkitSettingsScroll.Add(toolkitLayerHierarchyRoot);

            toolkitSettingsScroll.scrollOffset = scrollPosition;
            BuildToolkitLayerFooter();
        }

        private void BuildToolkitLayerFooter()
        {
            toolkitLayerFooter.Clear();
            toolkitLayerFooter.Add(CreateLayerActionButton(
                LayerActionIcon.Kind.Add, "Add layer", ShowAddMenuForSelection));
            Button group = CreateLayerActionButton(
                LayerActionIcon.Kind.Group, "Group selected layers", GroupSelectedLayer);
            Button delete = CreateLayerActionButton(
                LayerActionIcon.Kind.Delete, "Delete selected layers", DeleteSelectedLayers);
            group.AddToClassList("sprite-editor-layer-action--separated");
            delete.AddToClassList("sprite-editor-layer-action--separated");
            group.tooltip = "Group selected layers. You can also drop layers here.";
            delete.tooltip = "Delete selected layers. You can also drop layers here.";
            group.AddManipulator(new LayerFooterDropManipulator(this, delete: false));
            delete.AddManipulator(new LayerFooterDropManipulator(this, delete: true));
            toolkitLayerFooter.Add(group);
            toolkitLayerFooter.Add(delete);
            toolkitSettingsBindings.Add(() =>
            {
                bool hasSelection = GetSelectedLayer() != null;
                group.SetEnabled(hasSelection);
                delete.SetEnabled(hasSelection);
            });
        }

        private static Button CreateLayerActionButton(LayerActionIcon.Kind icon, string tooltip, Action clicked)
        {
            Button button = new Button(clicked) { tooltip = tooltip };
            button.AddToClassList("sprite-editor-layer-action");
            button.Add(new LayerActionIcon(icon));
            return button;
        }

        private void RefreshToolkitLayerHierarchy(bool forceValues = false)
        {
            if (toolkitLayerHierarchyRoot == null)
                return;

            toolkitNextLayerTree.Clear();
            CollectToolkitLayerTree(compositor?.layers, 0);
            bool structureChanged = toolkitLayerTree.Count != toolkitNextLayerTree.Count || toolkitLayerHierarchyRoot.childCount == 0;
            for (int i = 0; !structureChanged && i < toolkitLayerTree.Count; i++)
                structureChanged = !toolkitLayerTree[i].Equals(toolkitNextLayerTree[i]);
            if (!structureChanged)
            {
                toolkitLayerBindings.Refresh(forceValues);
                return;
            }

            ClearToolkitDropIndicator();
            toolkitLayerBindings.Clear();
            toolkitLayerTree.Clear();
            toolkitLayerTree.AddRange(toolkitNextLayerTree);
            toolkitLayerHierarchyRoot.Clear();
            if (compositor == null || compositor.layers.Count == 0)
            {
                SpriteEditorUI.AddHelpBox(
                    toolkitLayerHierarchyRoot,
                    "Add a layer or group to start composing.",
                    HelpBoxMessageType.Info);
                return;
            }

            AddToolkitLayerRows(compositor.layers, 0, toolkitLayerHierarchyRoot);
            toolkitLayerBindings.Refresh(forceValues);
        }

        private void CollectToolkitLayerTree(List<Layer> layers, int depth)
        {
            if (layers == null)
                return;
            for (int i = 0; i < layers.Count; i++)
            {
                Layer layer = layers[i];
                toolkitNextLayerTree.Add(new LayerTreeEntry(layers, layer, i, depth));
                if (layer is GroupLayer group && GetGroupExpanded(group))
                    CollectToolkitLayerTree(group.layers, depth + 1);
            }
            toolkitNextLayerTree.Add(new LayerTreeEntry(layers, null, -1, depth));
        }

        private void AddToolkitLayerRows(List<Layer> layers, int depth, VisualElement root)
        {
            if (layers == null)
                return;

            for (int i = 0; i < layers.Count; i++)
            {
                Layer layer = layers[i];
                if (layer == null)
                {
                    root.Add(BuildMissingLayerRow(layers, i, depth));
                    continue;
                }

                VisualElement row = layer is GroupLayer group
                    ? BuildToolkitGroupRow(group, layers, i, depth)
                    : BuildToolkitLeafRow(layer, layers, i, depth);
                root.Add(row);

                if (layer is GroupLayer expandedGroup && GetGroupExpanded(expandedGroup))
                    AddToolkitLayerRows(expandedGroup.layers, depth + 1, root);
            }

            root.Add(BuildContainerEndDropZone(layers, depth));
        }

        private VisualElement CreateToolkitLayerRow(Layer layer, int depth)
        {
            VisualElement row = SpriteEditorUI.CreateRow();
            row.userData = layer.Id;
            row.style.height = ToolkitLayerRowHeight;
            row.style.flexShrink = 0f;
            row.style.marginBottom = 1f;
            row.style.paddingLeft = 3f + depth * ToolkitLayerIndent;
            row.style.paddingRight = 3f;
            ApplyLayerSelectionStyle(row, layer.Id);
            row.style.borderTopLeftRadius = 2f;
            row.style.borderTopRightRadius = 2f;
            row.style.borderBottomLeftRadius = 2f;
            row.style.borderBottomRightRadius = 2f;
            VisualElement activeOutline = new VisualElement { pickingMode = PickingMode.Ignore };
            activeOutline.AddToClassList("sprite-editor-layer-active-outline");
            row.Add(activeOutline);
            toolkitLayerBindings.Add(() =>
            {
                if (row != activeDropElement)
                    ApplyLayerSelectionStyle(row, layer.Id);
            });
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || evt.target is VisualElement target &&
                    target.ClassListContains("sprite-editor-layer-drag-handle"))
                    return;
                SelectLayerFromPointer(layer, evt);
                if (evt.ctrlKey || evt.commandKey || evt.shiftKey)
                {
                    evt.PreventDefault();
                    evt.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);
            return row;
        }

        private VisualElement BuildToolkitGroupRow(
            GroupLayer group,
            List<Layer> container,
            int index,
            int depth)
        {
            VisualElement row = CreateToolkitLayerRow(group, depth);
            row.Add(CreateToolkitDragHandle(group));

            Toggle enabled = new Toggle();
            enabled.tooltip = "Enable or disable this group and all of its descendants.";
            enabled.style.width = 20f;
            enabled.SetValueWithoutNotify(group.enabled);
            toolkitLayerBindings.Track(enabled, () => group.enabled);
            enabled.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Toggle Sprite Group",
                () => group.enabled = evt.newValue));
            row.Add(enabled);

            Button foldout = SpriteEditorUI.CreateButton(GetGroupExpanded(group) ? "▼" : "▶", () =>
            {
                groupExpansion[group.Id] = !GetGroupExpanded(group);
                RefreshToolkitLayerHierarchy();
            }, 22f);
            foldout.tooltip = "Expand or collapse this group.";
            row.Add(foldout);

            TextField name = new TextField();
            name.style.flexGrow = 1f;
            name.SetValueWithoutNotify(group.layerName);
            toolkitLayerBindings.Track(name, () => group.layerName);
            name.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Rename Sprite Group",
                () => group.layerName = evt.newValue));
            row.Add(name);

            Label count = new Label($"{group.layers.Count} items");
            toolkitLayerBindings.Add(() => count.text = $"{group.layers.Count} items");
            count.style.width = 52f;
            count.style.fontSize = 10f;
            row.Add(count);
            row.Add(SpriteEditorUI.CreateButton("+", () => ShowAddMenu(group.layers, 0), 24f));
            row.Add(CreateLayerMenuButton(() => ShowLayerContextMenu(group, container, index)));
            RegisterToolkitLayerDrop(row, group, container, index, depth);
            return row;
        }

        private VisualElement BuildToolkitLeafRow(
            Layer layer,
            List<Layer> container,
            int index,
            int depth)
        {
            VisualElement row = CreateToolkitLayerRow(layer, depth);
            row.Add(CreateToolkitDragHandle(layer));

            Toggle enabled = new Toggle();
            enabled.tooltip = "Enable or disable this layer.";
            enabled.style.width = 20f;
            enabled.SetValueWithoutNotify(layer.enabled);
            toolkitLayerBindings.Track(enabled, () => layer.enabled);
            enabled.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Toggle Sprite Layer",
                () => layer.enabled = evt.newValue));
            row.Add(enabled);

            Image thumbnail = new Image
            {
                image = layer.GetPreviewTexture(18),
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            thumbnail.style.width = 20f;
            thumbnail.style.height = 20f;
            thumbnail.style.marginRight = 3f;
            thumbnail.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f, 1f);
            row.Add(thumbnail);
            toolkitLayerBindings.Add(() => thumbnail.image = layer.GetPreviewTexture(18));

            TextField name = new TextField();
            name.style.flexGrow = 1f;
            name.style.minWidth = 72f;
            name.SetValueWithoutNotify(layer.layerName);
            toolkitLayerBindings.Track(name, () => layer.layerName);
            name.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Rename Sprite Layer",
                () => layer.layerName = evt.newValue));
            row.Add(name);

            FloatField opacity = new FloatField();
            opacity.tooltip = "Layer opacity from 0 to 1.";
            opacity.style.width = 48f;
            opacity.SetValueWithoutNotify(layer.opacity);
            toolkitLayerBindings.Track(opacity, () => layer.opacity);
            opacity.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Layer Opacity",
                () => layer.opacity = Mathf.Clamp01(evt.newValue)));
            row.Add(opacity);

            EnumField blend = new EnumField(layer.blendMode);
            toolkitLayerBindings.Track(blend, () => (Enum)layer.blendMode);
            blend.style.width = 126f;
            blend.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Layer Blend Mode",
                () => layer.blendMode = (BlendMode)evt.newValue));
            row.Add(blend);

            if (layer is TargetedLayerEffect effect)
            {
                Label warning = new Label("!");
                warning.tooltip = effect.inputMode == EffectInputMode.Specific
                    ? "Select an existing non-cyclic target layer or group in the effect settings."
                    : "This effect needs a layer or group directly below it.";
                warning.style.color = new Color(1f, 0.65f, 0.15f, 1f);
                warning.style.unityFontStyleAndWeight = FontStyle.Bold;
                warning.style.width = 12f;
                row.Add(warning);
                toolkitLayerBindings.Add(() =>
                {
                    warning.style.display = compositor.HasUsableEffectInput(effect, container, index)
                        ? DisplayStyle.None : DisplayStyle.Flex;
                    warning.tooltip = effect.inputMode == EffectInputMode.Specific
                        ? "Select an existing non-cyclic target layer or group in the effect settings."
                        : "This effect needs a layer or group directly below it.";
                });
            }

            row.Add(CreateLayerMenuButton(() => ShowLayerContextMenu(layer, container, index)));
            RegisterToolkitLayerDrop(row, layer, container, index, depth);
            return row;
        }

        private static Button CreateLayerMenuButton(Action clicked)
        {
            Button button = new Button(clicked) { tooltip = "Layer menu" };
            button.AddToClassList("sprite-editor-layer-menu-button");
            button.EnableInClassList("sprite-editor-layer-menu-button--light", !EditorGUIUtility.isProSkin);
            for (int i = 0; i < 3; i++)
            {
                VisualElement dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.AddToClassList("sprite-editor-layer-menu-dot");
                button.Add(dot);
            }
            return button;
        }

        private VisualElement BuildMissingLayerRow(List<Layer> container, int index, int depth)
        {
            VisualElement row = SpriteEditorUI.CreateRow();
            row.style.height = ToolkitLayerRowHeight;
            row.style.paddingLeft = 20f + depth * ToolkitLayerIndent;
            row.style.backgroundColor = SpriteEditorUI.RowColor;
            Label message = new Label("Missing layer data");
            message.style.flexGrow = 1f;
            row.Add(message);
            row.Add(SpriteEditorUI.CreateButton("Remove", () =>
                ExecuteModelChange("Remove Missing Layer", () => container.RemoveAt(index)), 60f));
            return row;
        }

        private VisualElement CreateToolkitDragHandle(Layer layer)
        {
            Label handle = new Label("≡");
            handle.AddToClassList("sprite-editor-layer-drag-handle");
            handle.tooltip = LayerDragHandleContent.tooltip;
            handle.style.width = 18f;
            handle.style.unityTextAlign = TextAnchor.MiddleCenter;
            handle.style.fontSize = 15f;
            handle.AddManipulator(new LayerDragManipulator(this, layer));
            return handle;
        }

        private sealed class LayerDragManipulator : PointerManipulator
        {
            private readonly TextureCompositorWindow owner;
            private readonly Layer layer;
            private Vector2 start;
            private int pointerId = -1;

            public LayerDragManipulator(TextureCompositorWindow owner, Layer layer)
            {
                this.owner = owner;
                this.layer = layer;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<PointerDownEvent>(OnPointerDown);
                target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                target.RegisterCallback<PointerUpEvent>(OnPointerUp);
                target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
                target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                Release();
                target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
                target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            }

            private void OnPointerDown(PointerDownEvent evt)
            {
                if (evt.button != 0 || pointerId >= 0)
                    return;
                owner.SelectLayerFromPointer(layer, evt, preserveSelection: true);
                if (evt.ctrlKey || evt.commandKey || evt.shiftKey)
                {
                    evt.StopImmediatePropagation();
                    return;
                }
                owner.activeLayerDrag?.Cancel();
                owner.activeLayerDrag = this;
                start = evt.position;
                pointerId = evt.pointerId;
                target.CapturePointer(pointerId);
                evt.StopImmediatePropagation();
            }

            private void OnPointerMove(PointerMoveEvent evt)
            {
                if (pointerId != evt.pointerId || Vector2.Distance(start, evt.position) < 4f)
                    return;

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
                DragAndDrop.SetGenericData(DraggedLayerIdKey, layer.Id);
                DragAndDrop.SetGenericData(DraggedCompositorIdKey, owner.compositor);
                DragAndDrop.SetGenericData(DraggedLayersKey, owner.GetSelectedRoots());
                DragAndDrop.StartDrag(string.IsNullOrEmpty(layer.layerName) ? "Layer" : layer.layerName);
                Release();
                evt.StopImmediatePropagation();
            }

            private void OnPointerUp(PointerUpEvent evt)
            {
                if (pointerId != evt.pointerId || evt.button != 0)
                    return;
                Release();
                evt.StopImmediatePropagation();
            }

            private void OnCaptureOut(PointerCaptureOutEvent evt)
            {
                if (pointerId == evt.pointerId)
                    Release();
            }

            private void OnDetach(DetachFromPanelEvent evt) => Release();

            public void Cancel() => Release();

            private void Release()
            {
                int previousPointer = pointerId;
                pointerId = -1;
                if (owner.activeLayerDrag == this)
                    owner.activeLayerDrag = null;
                if (previousPointer >= 0 && target.HasPointerCapture(previousPointer))
                    target.ReleasePointer(previousPointer);
            }
        }

        private void RegisterToolkitLayerDrop(
            VisualElement row,
            Layer rowLayer,
            List<Layer> rowContainer,
            int rowIndex,
            int depth)
        {
            row.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (!TryGetToolkitDrop(
                        row,
                        evt.mousePosition,
                        rowLayer,
                        rowContainer,
                        rowIndex,
                        out Layer dragged,
                        out List<Layer> destination,
                        out int destinationIndex,
                        out GroupLayer groupToExpand,
                        out bool insertBefore))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                SetToolkitDropIndicator(row, groupToExpand != null, insertBefore, depth);
                evt.StopImmediatePropagation();
            });
            row.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (!TryGetToolkitDrop(
                        row,
                        evt.mousePosition,
                        rowLayer,
                        rowContainer,
                        rowIndex,
                        out Layer dragged,
                        out List<Layer> destination,
                        out int destinationIndex,
                        out GroupLayer groupToExpand,
                        out _))
                {
                    return;
                }

                DragAndDrop.AcceptDrag();
                ClearToolkitDropIndicator();
                PerformLayerDrop(dragged, destination, destinationIndex, groupToExpand);
                ClearLayerDragData();
                evt.StopImmediatePropagation();
            });
            row.RegisterCallback<DragLeaveEvent>(_ => ClearToolkitDropIndicator());
        }

        private bool TryGetToolkitDrop(
            VisualElement row,
            Vector2 mousePosition,
            Layer rowLayer,
            List<Layer> rowContainer,
            int rowIndex,
            out Layer dragged,
            out List<Layer> destination,
            out int destinationIndex,
            out GroupLayer groupToExpand,
            out bool insertBefore)
        {
            dragged = GetDraggedLayer();
            destination = null;
            destinationIndex = -1;
            groupToExpand = null;
            insertBefore = false;
            if (dragged == null)
                return false;

            Vector2 local = row.WorldToLocal(mousePosition);
            float height = Mathf.Max(1f, row.resolvedStyle.height);
            bool dropInside = rowLayer is GroupLayer &&
                              local.y >= height * 0.25f &&
                              local.y <= height * 0.75f;
            insertBefore = local.y < height * 0.5f;
            if (dropInside)
            {
                groupToExpand = (GroupLayer)rowLayer;
                destination = groupToExpand.layers;
                destinationIndex = 0;
            }
            else
            {
                destination = rowContainer;
                destinationIndex = insertBefore ? rowIndex : rowIndex + 1;
            }
            return CanDropLayer(dragged, destination, destinationIndex);
        }

        private VisualElement BuildContainerEndDropZone(List<Layer> destination, int depth)
        {
            VisualElement zone = new VisualElement();
            zone.style.height = 8f;
            zone.style.marginLeft = 4f + depth * ToolkitLayerIndent;
            zone.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                Layer dragged = GetDraggedLayer();
                if (dragged == null || !CanDropLayer(dragged, destination, destination.Count))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                SetToolkitDropIndicator(zone, false, true, depth);
                evt.StopImmediatePropagation();
            });
            zone.RegisterCallback<DragPerformEvent>(evt =>
            {
                Layer dragged = GetDraggedLayer();
                if (dragged == null || !CanDropLayer(dragged, destination, destination.Count))
                    return;
                DragAndDrop.AcceptDrag();
                ClearToolkitDropIndicator();
                PerformLayerDrop(dragged, destination, destination.Count, null);
                ClearLayerDragData();
                evt.StopImmediatePropagation();
            });
            zone.RegisterCallback<DragLeaveEvent>(_ => ClearToolkitDropIndicator());
            return zone;
        }

        private void SetToolkitDropIndicator(
            VisualElement element,
            bool insideGroup,
            bool insertBefore,
            int depth)
        {
            if (activeDropElement != element)
            {
                ClearToolkitDropIndicator();
                activeDropMarginLeft = element.style.marginLeft;
            }
            activeDropElement = element;

            if (insideGroup)
            {
                element.style.backgroundColor = GroupDropHighlightColor;
                element.style.borderTopWidth = 1f;
                element.style.borderRightWidth = 1f;
                element.style.borderBottomWidth = 1f;
                element.style.borderLeftWidth = 1f;
                SetBorderColor(element, DropIndicatorColor);
            }
            else
            {
                element.style.borderTopWidth = insertBefore ? 2f : 0f;
                element.style.borderBottomWidth = insertBefore ? 0f : 2f;
                element.style.borderLeftWidth = 0f;
                element.style.borderRightWidth = 0f;
                element.style.borderTopColor = DropIndicatorColor;
                element.style.borderBottomColor = DropIndicatorColor;
                element.style.marginLeft = Mathf.Max(element.resolvedStyle.marginLeft, depth * ToolkitLayerIndent);
            }
        }

        private void ClearToolkitDropIndicator()
        {
            if (activeDropElement == null)
                return;

            activeDropElement.style.borderTopWidth = 0f;
            activeDropElement.style.borderRightWidth = 0f;
            activeDropElement.style.borderBottomWidth = 0f;
            activeDropElement.style.borderLeftWidth = 0f;
            activeDropElement.style.marginLeft = activeDropMarginLeft;
            if (activeDropElement.userData is string layerId)
            {
                activeDropElement.style.backgroundColor = StyleKeyword.Null;
                ApplyLayerSelectionStyle(activeDropElement, layerId);
            }
            else
            {
                activeDropElement.style.backgroundColor = Color.clear;
            }
            activeDropElement = null;
        }

        private static void SetBorderColor(VisualElement element, Color color)
        {
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
        }

        private void RefreshToolkitPreviewHeader(bool forceValues = false)
        {
            if (toolkitPreviewHeader == null || compositor == null)
                return;

            DrawingLayer layer = GetSelectedLayer() as DrawingLayer;
            if (toolkitHeaderBuilt && ReferenceEquals(toolkitHeaderLayer, layer))
            {
                toolkitHeaderBindings.Refresh(forceValues);
                return;
            }

            toolkitHeaderBuilt = true;
            toolkitHeaderLayer = layer;
            toolkitHeaderBindings.Clear();
            toolkitPreviewHeader.Clear();
            BuildToolkitPreviewHeader(layer);
            toolkitHeaderBindings.Refresh(forceValues);
        }

        private void BuildToolkitPreviewHeader(DrawingLayer layer)
        {
            toolkitPreviewActions.Clear();
            AddPreviewTransformButton(toolkitPreviewActions);
            if (layer != null)
                toolkitPreviewActions.Add(SpriteEditorUI.CreateToolbarButton("Clear", () => ClearDrawingLayer(layer), 46f));
            toolkitPreviewActions.Add(SpriteEditorUI.CreateToolbarButton("Refresh", () => RequestPreview(true), 64f));
            AddPreviewTransformSettings();

            if (layer == null)
                return;

            VisualElement brushRow = SpriteEditorUI.CreateToolbar();
            EnumField tool = CompactField(new EnumField(layer.tool), 72f);
            toolkitHeaderBindings.Track(tool, () => (Enum)layer.tool);
            tool.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Drawing Tool",
                () => layer.tool = (PaintToolMode)evt.newValue));
            brushRow.Add(tool);

            ColorField primary = CompactField(new ColorField(), 54f);
            primary.tooltip = PrimaryBrushColorContent.tooltip;
            primary.SetValueWithoutNotify(layer.brushColor);
            toolkitHeaderBindings.Track(primary, () => layer.brushColor);
            primary.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Foreground Brush Color",
                () => layer.brushColor = evt.newValue));
            brushRow.Add(primary);
            ColorField secondary = CompactField(new ColorField(), 54f);
            secondary.tooltip = SecondaryBrushColorContent.tooltip;
            secondary.SetValueWithoutNotify(layer.secondaryBrushColor);
            toolkitHeaderBindings.Track(secondary, () => layer.secondaryBrushColor);
            secondary.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Background Brush Color",
                () => layer.secondaryBrushColor = evt.newValue));
            brushRow.Add(secondary);

            brushRow.Add(CreateCompactLabel("Size", 30f));
            FloatField size = CompactField(new FloatField(), 46f);
            size.SetValueWithoutNotify(layer.brushSize);
            toolkitHeaderBindings.Track(size, () => layer.brushSize);
            size.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Brush Size",
                () => layer.brushSize = Mathf.Max(1f, evt.newValue)));
            brushRow.Add(size);
            brushRow.Add(CreateCompactLabel("Hard", 32f));
            Slider hardness = new Slider(0f, 1f) { value = layer.brushHardness };
            hardness.style.flexGrow = 1f;
            hardness.style.minWidth = 42f;
            Label hardnessValue = CreateCompactLabel($"{layer.brushHardness * 100f:0}%", 38f);
            toolkitHeaderBindings.Track(hardness, () => layer.brushHardness);
            toolkitHeaderBindings.Add(() => hardnessValue.text = $"{layer.brushHardness * 100f:0}%");
            hardness.RegisterValueChangedCallback(evt =>
            {
                hardnessValue.text = $"{evt.newValue * 100f:0}%";
                ApplyToolkitChange("Change Brush Hardness", () => layer.brushHardness = Mathf.Clamp01(evt.newValue));
            });
            brushRow.Add(hardness);
            brushRow.Add(hardnessValue);
            toolkitPreviewHeader.Add(brushRow);

            VisualElement mirrorRow = SpriteEditorUI.CreateToolbar();
            Toggle mirrorX = CompactField(new Toggle("Mirror X"), 82f);
            mirrorX.tooltip = MirrorVerticalContent.tooltip;
            mirrorX.SetValueWithoutNotify(layer.mirrorAcrossVerticalAxis);
            toolkitHeaderBindings.Track(mirrorX, () => layer.mirrorAcrossVerticalAxis);
            mirrorX.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Drawing Symmetry",
                () => layer.mirrorAcrossVerticalAxis = evt.newValue));
            mirrorRow.Add(mirrorX);
            Toggle mirrorY = CompactField(new Toggle("Mirror Y"), 82f);
            mirrorY.tooltip = MirrorHorizontalContent.tooltip;
            mirrorY.SetValueWithoutNotify(layer.mirrorAcrossHorizontalAxis);
            toolkitHeaderBindings.Track(mirrorY, () => layer.mirrorAcrossHorizontalAxis);
            mirrorY.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Drawing Symmetry",
                () => layer.mirrorAcrossHorizontalAxis = evt.newValue));
            mirrorRow.Add(mirrorY);
            Vector2Field center = new Vector2Field("Center");
            center.style.flexGrow = 1f;
            center.style.minWidth = 128f;
            center.labelElement.style.width = 44f;
            center.labelElement.style.minWidth = 44f;
            center.SetValueWithoutNotify(layer.patternCenter);
            toolkitHeaderBindings.Track(center, () => layer.patternCenter);
            center.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Pattern Center",
                () => layer.patternCenter = new Vector2(
                    Mathf.Clamp01(evt.newValue.x),
                    Mathf.Clamp01(evt.newValue.y))));
            mirrorRow.Add(center);
            toolkitPreviewHeader.Add(mirrorRow);

            VisualElement repeatRow = SpriteEditorUI.CreateToolbar();
            repeatRow.Add(CreateCompactLabel("Repeat", 44f));
            EnumField repeat = CompactField(new EnumField(layer.repeatMode), 88f);
            toolkitHeaderBindings.Track(repeat, () => (Enum)layer.repeatMode);
            repeat.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Repeat Mode",
                () => layer.repeatMode = (PaintRepeatMode)evt.newValue));
            repeatRow.Add(repeat);
            VisualElement counts = SpriteEditorUI.CreateRow();
            Label countLabel = CreateCompactLabel("Count", 38f);
            counts.Add(countLabel);
            IntegerField countX = CompactField(new IntegerField(), 38f);
            toolkitHeaderBindings.Track(countX, () => layer.repeatCount);
            countX.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Repeat Count", () => layer.repeatCount = Mathf.Clamp(evt.newValue, 2, 64)));
            counts.Add(countX);
            VisualElement secondaryCount = SpriteEditorUI.CreateRow();
            secondaryCount.Add(CreateCompactLabel("Y", 14f));
            IntegerField countY = CompactField(new IntegerField(), 38f);
            toolkitHeaderBindings.Track(countY, () => layer.repeatSecondaryCount);
            countY.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Repeat Count", () => layer.repeatSecondaryCount = Mathf.Clamp(evt.newValue, 2, 64)));
            secondaryCount.Add(countY);
            counts.Add(secondaryCount);
            repeatRow.Add(counts);
            toolkitHeaderBindings.Add(() =>
            {
                counts.style.display = layer.repeatMode == PaintRepeatMode.None ? DisplayStyle.None : DisplayStyle.Flex;
                secondaryCount.style.display = layer.repeatMode == PaintRepeatMode.Grid ? DisplayStyle.Flex : DisplayStyle.None;
                countLabel.text = layer.repeatMode == PaintRepeatMode.Grid ? "X" : "Count";
            });
            VisualElement repeatSpacer = new VisualElement();
            repeatSpacer.style.flexGrow = 1f;
            repeatRow.Add(repeatSpacer);
            EnumField elementMode = CompactField(new EnumField(layer.repeatElementMode), 116f);
            elementMode.SetEnabled(layer.repeatMode != PaintRepeatMode.None);
            toolkitHeaderBindings.Track(elementMode, () => (Enum)layer.repeatElementMode);
            toolkitHeaderBindings.Add(() => elementMode.SetEnabled(layer.repeatMode != PaintRepeatMode.None));
            elementMode.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Repeat Element Mode",
                () => layer.repeatElementMode = (PaintRepeatElementMode)evt.newValue));
            repeatRow.Add(elementMode);
            toolkitPreviewHeader.Add(repeatRow);

            VisualElement boundaryRow = SpriteEditorUI.CreateToolbar();
            Label edgeLabel = CreateCompactLabel("Edges", 38f);
            edgeLabel.tooltip = RepeatBoundaryContent.tooltip;
            boundaryRow.Add(edgeLabel);
            EnumField boundary = CompactField(new EnumField(layer.repeatBoundaryMode), 84f);
            boundary.SetEnabled(layer.repeatMode != PaintRepeatMode.None);
            toolkitHeaderBindings.Track(boundary, () => (Enum)layer.repeatBoundaryMode);
            toolkitHeaderBindings.Add(() => boundary.SetEnabled(layer.repeatMode != PaintRepeatMode.None));
            boundary.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Repeat Boundary",
                () => layer.repeatBoundaryMode = (PaintRepeatBoundaryMode)evt.newValue));
            boundaryRow.Add(boundary);
            VisualElement boundarySpacer = new VisualElement();
            boundarySpacer.style.flexGrow = 1f;
            boundaryRow.Add(boundarySpacer);
            FloatField spacing = CompactField(new FloatField("Step"), 72f);
            spacing.tooltip = BrushSpacingContent.tooltip;
            spacing.labelElement.style.width = 30f;
            spacing.labelElement.style.minWidth = 30f;
            spacing.labelElement.style.flexShrink = 0f;
            spacing.SetValueWithoutNotify(layer.brushSpacing * 100f);
            toolkitHeaderBindings.Track(spacing, () => layer.brushSpacing * 100f);
            spacing.RegisterValueChangedCallback(evt =>
            {
                float clampedPercent = Mathf.Clamp(
                    evt.newValue,
                    DrawingLayer.MinimumBrushSpacing * 100f,
                    DrawingLayer.MaximumBrushSpacing * 100f);
                ApplyToolkitChange(
                    "Change Brush Step",
                    () => layer.brushSpacing = clampedPercent * 0.01f);
            });
            boundaryRow.Add(spacing);
            boundaryRow.Add(CreateCompactLabel("%", 14f));
            toolkitPreviewHeader.Add(boundaryRow);

            VisualElement qualityRow = SpriteEditorUI.CreateToolbar();
            Label qualityLabel = CreateCompactLabel("Live Quality", 78f);
            qualityLabel.tooltip = LivePreviewQualityContent.tooltip;
            qualityRow.Add(qualityLabel);
            Slider quality = new Slider(
                MinimumPaintingPreviewScale * 100f,
                MaximumPaintingPreviewScale * 100f)
            {
                value = paintingPreviewScale * 100f
            };
            quality.style.flexGrow = 1f;
            Label qualityValue = CreateCompactLabel($"{paintingPreviewScale * 100f:0.#}%", 44f);
            quality.RegisterValueChangedCallback(evt =>
            {
                paintingPreviewScale = ClampPaintingPreviewScale(evt.newValue * 0.01f);
                EditorPrefs.SetFloat(PaintingPreviewScalePrefKey, paintingPreviewScale);
                qualityValue.text = $"{paintingPreviewScale * 100f:0.#}%";
            });
            qualityRow.Add(quality);
            qualityRow.Add(qualityValue);
            toolkitPreviewHeader.Add(qualityRow);
        }

        private static T CompactField<T>(T field, float width) where T : VisualElement
        {
            field.style.width = width;
            field.style.height = ToolkitPreviewHeaderRowHeight - 2f;
            field.style.marginLeft = 1f;
            field.style.marginRight = 1f;
            return field;
        }

        private static Label CreateCompactLabel(string text, float width)
        {
            Label result = new Label(text);
            result.style.width = width;
            result.style.unityTextAlign = TextAnchor.MiddleLeft;
            return result;
        }

        private void ApplyToolkitChange(
            string undoName,
            Action change)
        {
            if (compositor == null || change == null)
                return;

            Undo.RecordObject(compositor, undoName);
            applyingToolkitChange = true;
            try
            {
                change();
                CommitModelChange();
            }
            finally
            {
                applyingToolkitChange = false;
            }

            RefreshToolkitInterface();
        }

        private void UpdateToolkitPreviewPresentation()
        {
            if (toolkitPreviewCanvas == null || compositor == null)
                return;

            DrawingLayer drawing = GetSelectedLayer() as DrawingLayer;
            RefreshPreviewTransformTool();
            bool transforming = IsPreviewTransformEnabled;
            toolkitPreviewCanvas.SetDocument(previewTexture, compositor.width, compositor.height,
                transforming ? null : drawing, transforming);
            if (toolkitPreviewError != null)
            {
                toolkitPreviewError.text = previewError ?? string.Empty;
                toolkitPreviewError.style.display = string.IsNullOrEmpty(previewError) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (toolkitPreviewFooter != null)
            {
                if (transforming)
                {
                    toolkitPreviewFooter.text = "Drag move • handles scale • circle rotate • gold cross pivot • Shift constrain • Esc cancel • T exit";
                }
                else if (drawing != null)
                {
                    toolkitPreviewFooter.text = previewTexture != null
                        ? $"LMB paint • RMB erase • Shift lines • X colors • [ ] size • {drawing.brushSize:0.#} px"
                        : "Rendering painting preview…";
                }
                else
                {
                    toolkitPreviewFooter.text = previewTexture != null
                        ? "Transparent canvas • auto refresh"
                        : "Rendering preview…";
                }
            }
        }

        private void OnPreviewPointerEnter(PointerEnterEvent evt)
        {
            UpdatePreviewCursor(evt.localPosition, evt.altKey);
        }

        private void OnPreviewPointerLeave(PointerLeaveEvent evt)
        {
            if (paintingLayer == null)
                toolkitPreviewCanvas?.SetCursor(false, default, false);
        }

        private void OnPreviewPointerDown(PointerDownEvent evt)
        {
            DrawingLayer layer = GetSelectedLayer() as DrawingLayer;
            if (IsPreviewTransformEnabled || paintingLayer != null || layer == null || (evt.button != 0 && evt.button != 1) || evt.altKey)
                return;
            if (!toolkitPreviewCanvas.ImageRect.Contains(evt.localPosition) ||
                !TryMapPreviewToLayerUv(evt.localPosition, toolkitPreviewCanvas.ImageRect, layer, out Vector2 startUv))
            {
                return;
            }

            Focus();
            toolkitPreviewCanvas.Focus();
            paintingLayer = layer;
            paintingMouseButton = evt.button;
            paintingPointerId = evt.pointerId;
            paintingErase = evt.button == 1 || layer.tool == PaintToolMode.Eraser;
            paintingPointerMoved = false;
            bool connect = evt.shiftKey && ReferenceEquals(lineAnchorLayer, layer) &&
                           lineAnchorCanvasSize == new Vector2Int(compositor.width, compositor.height);
            Vector2 originUv = connect ? lineAnchorUv : startUv;
            lastPaintingUv = originUv;
            hasLastPaintingUv = true;
            toolkitPreviewCanvas.CapturePointer(evt.pointerId);
            Undo.RecordObject(compositor, "Paint Stroke");
            layer.PrepareStroke(compositor.width, compositor.height, "Paint Stroke");
            layer.BeginStroke(originUv);
            RememberPaintingPoint(originUv);
            if (connect && originUv != startUv)
                PaintTowardsLayerPoint(startUv);
            else
                layer.PaintPoint(startUv, compositor.width, compositor.height, paintingErase);
            paintingShiftHeld = false;
            SetPaintingShift(evt.shiftKey);
            RefreshPreviewDuringPainting();
            UpdatePreviewCursor(evt.localPosition, false);
            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }

        private void OnPreviewPointerMove(PointerMoveEvent evt)
        {
            if (paintingLayer == null || paintingPointerId != evt.pointerId)
            {
                UpdatePreviewCursor(evt.localPosition, evt.altKey);
                return;
            }

            Vector2 paintPosition = ConstrainPaintingPosition(evt.localPosition, evt.shiftKey);
            paintingPointerMoved |= evt.deltaPosition.sqrMagnitude > 0f;
            UpdatePreviewCursor(paintPosition, evt.altKey);

            if (TryMapPreviewToLayerUv(
                    paintPosition,
                    toolkitPreviewCanvas.ImageRect,
                    paintingLayer,
                    out Vector2 dragUv))
            {
                PaintTowardsLayerPoint(dragUv);
            }
            else
            {
                hasLastPaintingUv = false;
            }

            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }

        private void PaintTowardsLayerPoint(Vector2 pointUv)
        {
            if (!paintingLayer.IsStrokePointInsideRepeatShape(pointUv, compositor.width, compositor.height))
            {
                if (hasLastPaintingUv && paintingLayer.TryClipStrokeSegmentToRepeatShape(
                        lastPaintingUv, pointUv, compositor.width, compositor.height, out Vector2 clippedUv))
                {
                    paintingLayer.PaintSegment(lastPaintingUv, clippedUv, compositor.width, compositor.height, false, paintingErase);
                    RememberPaintingPoint(clippedUv);
                    RefreshPreviewDuringPainting();
                }
                hasLastPaintingUv = false;
                return;
            }

            if (hasLastPaintingUv)
            {
                if (lastPaintingUv == pointUv)
                    return;
                paintingLayer.PaintSegment(lastPaintingUv, pointUv, compositor.width, compositor.height, false, paintingErase);
            }
            else
            {
                paintingLayer.PaintPoint(pointUv, compositor.width, compositor.height, paintingErase);
            }
            RememberPaintingPoint(pointUv);
            hasLastPaintingUv = true;
            RefreshPreviewDuringPainting();
        }

        private void SetPaintingShift(bool held)
        {
            if (paintingShiftHeld == held)
                return;
            paintingShiftHeld = held;
            paintingLockedAxis = 0;
            paintingAxisAnchor = lastPaintingDocumentUv;
        }

        private Vector2 ConstrainPaintingPosition(Vector2 position, bool shift)
        {
            SetPaintingShift(shift);
            if (!shift)
                return position;

            Rect rect = toolkitPreviewCanvas.ImageRect;
            Vector2 anchor = new Vector2(
                rect.x + paintingAxisAnchor.x * rect.width,
                rect.y + (1f - paintingAxisAnchor.y) * rect.height);
            Vector2 delta = position - anchor;
            if (paintingLockedAxis == 0)
            {
                if (delta.sqrMagnitude < 4f)
                    return anchor;
                paintingLockedAxis = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? 1 : 2;
            }
            return paintingLockedAxis == 1 ? new Vector2(position.x, anchor.y) : new Vector2(anchor.x, position.y);
        }

        private void OnPreviewPointerUp(PointerUpEvent evt)
        {
            if (paintingLayer == null ||
                paintingPointerId != evt.pointerId ||
                paintingMouseButton != evt.button)
            {
                return;
            }

            Vector2 paintPosition = ConstrainPaintingPosition(evt.localPosition, evt.shiftKey);
            if (paintingPointerMoved &&
                TryMapPreviewToLayerUv(paintPosition, toolkitPreviewCanvas.ImageRect, paintingLayer, out Vector2 endUv))
                PaintTowardsLayerPoint(endUv);

            paintingPointerId = -1;
            if (toolkitPreviewCanvas.HasPointerCapture(evt.pointerId))
                toolkitPreviewCanvas.ReleasePointer(evt.pointerId);
            FinishPaintingStroke();
            UpdatePreviewCursor(evt.localPosition, evt.altKey);
            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }

        private void OnPreviewPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (paintingLayer == null || paintingPointerId != evt.pointerId)
                return;
            paintingPointerId = -1;
            FinishPaintingStroke();
        }

        private void UpdatePreviewCursor(Vector2 localPosition, bool alt)
        {
            DrawingLayer layer = GetSelectedLayer() as DrawingLayer;
            bool visible = !IsPreviewTransformEnabled && layer != null &&
                           !alt &&
                           toolkitPreviewCanvas != null &&
                           toolkitPreviewCanvas.ImageRect.Contains(localPosition);
            toolkitPreviewCanvas?.SetCursor(
                visible,
                localPosition,
                paintingLayer == layer ? paintingErase : layer != null && layer.tool == PaintToolMode.Eraser);
        }

        private void OnToolkitKeyDown(KeyDownEvent evt)
        {
            if ((evt.ctrlKey || evt.commandKey) && !evt.altKey && !evt.shiftKey && evt.keyCode == KeyCode.S)
            {
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                if (compositor != null && AssetDatabase.Contains(compositor))
                    SaveAsset();
                else
                    SaveAsAsset();
                return;
            }

            if (IsTextInputTarget(evt.target as VisualElement))
                return;

            if (HandlePreviewTransformKey(evt))
                return;

            if (paintingLayer != null && (evt.keyCode == KeyCode.LeftShift || evt.keyCode == KeyCode.RightShift))
            {
                SetPaintingShift(true);
                evt.StopImmediatePropagation();
                return;
            }

            bool actionModifier = evt.ctrlKey || evt.commandKey;
            bool undo = actionModifier && !evt.altKey && evt.keyCode == KeyCode.Z && !evt.shiftKey;
            bool redo = actionModifier && !evt.altKey &&
                        ((evt.keyCode == KeyCode.Z && evt.shiftKey) ||
                         (evt.keyCode == KeyCode.Y && !evt.shiftKey));
            if (undo || redo)
            {
                FinishPreviewTransform();
                FinishPaintingStroke();
                if (undo)
                    Undo.PerformUndo();
                else
                    Undo.PerformRedo();
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                return;
            }

            if (!(GetSelectedLayer() is DrawingLayer layer))
                return;

            bool swapColors = !actionModifier && !evt.altKey && evt.keyCode == KeyCode.X;
            if (swapColors)
            {
                ApplyToolkitChange("Swap Brush Colors", layer.SwapBrushColors);
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                return;
            }

            bool decrease = evt.keyCode == KeyCode.LeftBracket || evt.character == '[';
            bool increase = evt.keyCode == KeyCode.RightBracket || evt.character == ']';
            if (!decrease && !increase)
                return;

            float nextSize = decrease
                ? Mathf.Max(1f, Mathf.Round(layer.brushSize / 1.2f))
                : Mathf.Max(1f, Mathf.Round(layer.brushSize * 1.2f));
            if (Mathf.Approximately(nextSize, layer.brushSize))
                nextSize = Mathf.Max(1f, layer.brushSize + (increase ? 1f : -1f));
            ApplyToolkitChange("Change Brush Size", () => layer.brushSize = nextSize);
            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }

        private void OnToolkitKeyUp(KeyUpEvent evt)
        {
            if (paintingLayer != null && (evt.keyCode == KeyCode.LeftShift || evt.keyCode == KeyCode.RightShift))
            {
                SetPaintingShift(evt.shiftKey);
                evt.StopImmediatePropagation();
            }
        }

        private static bool IsTextInputTarget(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current is TextField ||
                    current is IntegerField ||
                    current is FloatField ||
                    current is Vector2Field ||
                    current.ClassListContains("unity-base-text-field__input"))
                {
                    return true;
                }
            }
            return false;
        }

        private sealed class SpritePreviewElement : VisualElement
        {
            private readonly VisualElement checker;
            private readonly Image image;
            private readonly VisualElement overlay;
            private Texture2D texture;
            private DrawingLayer drawingLayer;
            private int documentWidth = 1;
            private int documentHeight = 1;
            private bool cursorVisible;
            private bool cursorErase;
            private bool transformMode;
            private Vector2 cursorPosition;

            public Rect ImageRect { get; private set; }

            public SpritePreviewElement()
            {
                focusable = true;
                style.minHeight = 96f;
                style.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.08f, 0.08f, 0.08f, 1f)
                    : new Color(0.58f, 0.58f, 0.58f, 1f);

                checker = new VisualElement { pickingMode = PickingMode.Ignore };
                checker.style.position = Position.Absolute;
                checker.style.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.26f, 0.26f, 0.26f, 1f)
                    : new Color(0.76f, 0.76f, 0.76f, 1f);
                checker.generateVisualContent += DrawCheckerboard;
                Add(checker);

                image = new Image
                {
                    scaleMode = ScaleMode.StretchToFill,
                    pickingMode = PickingMode.Ignore
                };
                image.style.position = Position.Absolute;
                Add(image);

                overlay = new VisualElement { pickingMode = PickingMode.Ignore };
                overlay.style.position = Position.Absolute;
                overlay.generateVisualContent += DrawOverlay;
                Add(overlay);

                RegisterCallback<GeometryChangedEvent>(_ => UpdateImageLayout());
            }

            public void SetDocument(Texture2D nextTexture, int width, int height, DrawingLayer layer, bool transforming = false)
            {
                transformMode = transforming;
                texture = nextTexture;
                documentWidth = Mathf.Max(1, width);
                documentHeight = Mathf.Max(1, height);
                drawingLayer = layer;
                if (drawingLayer == null)
                    cursorVisible = false;
                overlay.style.display = drawingLayer == null
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
                image.image = texture;
                UpdateImageLayout();
                if (drawingLayer != null)
                    overlay.MarkDirtyRepaint();
            }

            public void SetCursor(bool visible, Vector2 position, bool erase)
            {
                cursorVisible = visible;
                cursorPosition = position;
                cursorErase = erase;
                overlay.MarkDirtyRepaint();
            }

            private void UpdateImageLayout()
            {
                Rect available = contentRect;
                float inset = transformMode ? 36f : 4f;
                available.x += inset;
                available.y += inset;
                available.width = Mathf.Max(0f, available.width - inset * 2f);
                available.height = Mathf.Max(0f, available.height - inset * 2f);
                float aspect = texture != null && texture.height > 0
                    ? (float)texture.width / texture.height
                    : (float)documentWidth / documentHeight;
                Rect nextRect = FitRect(available, aspect);
                if (nextRect == ImageRect)
                    return;
                ImageRect = nextRect;
                PositionElement(checker, ImageRect);
                PositionElement(image, ImageRect);
                PositionElement(overlay, ImageRect);
                checker.MarkDirtyRepaint();
                overlay.MarkDirtyRepaint();
            }

            private static void PositionElement(VisualElement element, Rect rect)
            {
                element.style.left = rect.x;
                element.style.top = rect.y;
                element.style.width = rect.width;
                element.style.height = rect.height;
            }

            private void DrawCheckerboard(MeshGenerationContext context)
            {
                const float tileSize = 16f;
                Rect rect = checker.contentRect;
                if (rect.width <= 0f || rect.height <= 0f)
                    return;

                Color light = EditorGUIUtility.isProSkin
                    ? new Color(0.30f, 0.30f, 0.30f, 1f)
                    : new Color(0.84f, 0.84f, 0.84f, 1f);
                Color dark = EditorGUIUtility.isProSkin
                    ? new Color(0.23f, 0.23f, 0.23f, 1f)
                    : new Color(0.70f, 0.70f, 0.70f, 1f);
                Painter2D painter = context.painter2D;
                int rows = Mathf.CeilToInt(rect.height / tileSize);
                int columns = Mathf.CeilToInt(rect.width / tileSize);
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        painter.fillColor = ((row + column) & 1) == 0 ? light : dark;
                        FillRect(
                            painter,
                            column * tileSize,
                            row * tileSize,
                            Mathf.Min(tileSize, rect.width - column * tileSize),
                            Mathf.Min(tileSize, rect.height - row * tileSize));
                    }
                }
            }

            private void DrawOverlay(MeshGenerationContext context)
            {
                if (drawingLayer == null)
                    return;

                Rect rect = overlay.contentRect;
                Painter2D painter = context.painter2D;
                Color guide = new Color(0.20f, 0.70f, 1f, 0.55f);
                painter.lineWidth = 1f;
                painter.strokeColor = guide;

                if (drawingLayer.mirrorAcrossVerticalAxis)
                {
                    float x = rect.width * drawingLayer.patternCenter.x;
                    StrokeLine(painter, new Vector2(x, 0f), new Vector2(x, rect.height));
                }
                if (drawingLayer.mirrorAcrossHorizontalAxis)
                {
                    float y = rect.height * (1f - drawingLayer.patternCenter.y);
                    StrokeLine(painter, new Vector2(0f, y), new Vector2(rect.width, y));
                }

                int count = Mathf.Clamp(drawingLayer.repeatCount, 2, 64);
                if (drawingLayer.repeatMode == PaintRepeatMode.Horizontal ||
                    drawingLayer.repeatMode == PaintRepeatMode.Grid)
                {
                    for (int i = 1; i < count; i++)
                    {
                        float x = rect.width * i / count;
                        StrokeLine(painter, new Vector2(x, 0f), new Vector2(x, rect.height));
                    }
                }
                if (drawingLayer.repeatMode == PaintRepeatMode.Vertical ||
                    drawingLayer.repeatMode == PaintRepeatMode.Grid)
                {
                    int verticalCount = drawingLayer.repeatMode == PaintRepeatMode.Grid
                        ? Mathf.Clamp(drawingLayer.repeatSecondaryCount, 2, 64)
                        : count;
                    for (int i = 1; i < verticalCount; i++)
                    {
                        float y = rect.height * i / verticalCount;
                        StrokeLine(painter, new Vector2(0f, y), new Vector2(rect.width, y));
                    }
                }
                if (drawingLayer.repeatMode == PaintRepeatMode.Radial)
                {
                    Vector2 center = new Vector2(
                        rect.width * drawingLayer.patternCenter.x,
                        rect.height * (1f - drawingLayer.patternCenter.y));
                    float length = Mathf.Sqrt(rect.width * rect.width + rect.height * rect.height);
                    for (int i = 0; i < count; i++)
                    {
                        float angle = -Mathf.PI + i * Mathf.PI * 2f / count;
                        Vector2 direction = new Vector2(
                            Mathf.Cos(angle) * rect.width,
                            -Mathf.Sin(angle) * rect.height).normalized;
                        StrokeLine(painter, center, center + direction * length);
                    }
                }

                if (!cursorVisible)
                    return;
                Vector2 localCursor = cursorPosition - ImageRect.position;
                float pixelScale = rect.width / Mathf.Max(1f, documentWidth);
                float transformScale =
                    (Mathf.Abs(drawingLayer.transform.scale.x) + Mathf.Abs(drawingLayer.transform.scale.y)) * 0.5f;
                float radius = Mathf.Max(
                    2f,
                    drawingLayer.brushSize * pixelScale * Mathf.Max(0.0001f, transformScale) * 0.5f);
                painter.lineWidth = 1f;
                painter.strokeColor = new Color(0f, 0f, 0f, 0.95f);
                StrokeCircle(painter, localCursor, radius + 1f);
                painter.strokeColor = cursorErase
                    ? new Color(1f, 0.35f, 0.25f, 1f)
                    : new Color(1f, 1f, 1f, 0.95f);
                StrokeCircle(painter, localCursor, radius);
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

            private static void FillRect(Painter2D painter, float x, float y, float width, float height)
            {
                if (width <= 0f || height <= 0f)
                    return;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, y));
                painter.LineTo(new Vector2(x + width, y));
                painter.LineTo(new Vector2(x + width, y + height));
                painter.LineTo(new Vector2(x, y + height));
                painter.ClosePath();
                painter.Fill();
            }

            private static void StrokeLine(Painter2D painter, Vector2 from, Vector2 to)
            {
                painter.BeginPath();
                painter.MoveTo(from);
                painter.LineTo(to);
                painter.Stroke();
            }

            private static void StrokeCircle(Painter2D painter, Vector2 center, float radius)
            {
                painter.BeginPath();
                painter.Arc(center, radius, 0f, 360f);
                painter.ClosePath();
                painter.Stroke();
            }
        }
    }
}
