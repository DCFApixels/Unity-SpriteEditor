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
            CancelPreviewZoomGesture();
            FinishPreviewTransform();
            if (compositor == null)
                SetCompositor(CreateTemporaryCompositor());

            VisualElement root = rootVisualElement;
            root.UnregisterCallback<KeyDownEvent>(OnToolkitKeyDown, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyUpEvent>(OnToolkitKeyUp, TrickleDown.TrickleDown);
            root.UnregisterCallback<DragExitedEvent>(OnToolkitDragExited);
            root.UnregisterCallback<PointerDownEvent>(OnOpacityPointerDown, TrickleDown.TrickleDown);
            ResetOpacityEntry();
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
            root.RegisterCallback<PointerDownEvent>(OnOpacityPointerDown, TrickleDown.TrickleDown);

            toolkitDocumentRoot = new VisualElement();
            toolkitDocumentRoot.style.flexShrink = 0f;
            root.Add(toolkitDocumentRoot);

            VisualElement workspace = new VisualElement { name = "spriteEditorWorkspace" };
            workspace.AddToClassList("sprite-editor-workspace");
            root.Add(workspace);
            workspace.Add(BuildPreviewToolToolbar());

            if (settingsPaneWidth <= 0f)
                settingsPaneWidth = DefaultSettingsPaneWidth;
            TwoPaneSplitView split = new TwoPaneSplitView(
                1,
                Mathf.Max(SettingsPaneMinWidth, settingsPaneWidth),
                TwoPaneSplitViewOrientation.Horizontal);
            SpriteEditorUI.StyleSplitView(split);
            split.AddToClassList("sprite-editor-workspace-split");
            split.style.flexGrow = 1f;
            split.style.minHeight = 0f;
            workspace.Add(split);

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

            VisualElement layerSettingsPane = new VisualElement();
            layerSettingsPane.AddToClassList("sprite-editor-layer-settings-pane");
            toolkitLayerSettingsTitle = CreatePaneHeader("Layer Settings", "selectedLayerTitle");
            layerSettingsPane.Add(toolkitLayerSettingsTitle);
            settingsSplit.Add(layerSettingsPane);

            toolkitLayerSettingsScroll = new ScrollView(ScrollViewMode.Vertical);
            toolkitLayerSettingsScroll.name = "selected-layer-settings";
            toolkitLayerSettingsScroll.style.paddingLeft = 8f;
            toolkitLayerSettingsScroll.style.paddingRight = 8f;
            toolkitLayerSettingsScroll.style.paddingBottom = 8f;
            layerSettingsPane.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.newRect.height >= 100f)
                    layerSettingsPaneHeight = evt.newRect.height;
            });
            layerSettingsPane.Add(toolkitLayerSettingsScroll);

            VisualElement layersPane = new VisualElement();
            layersPane.AddToClassList("sprite-editor-layers-pane");
            layersPane.Add(CreatePaneHeader("Layers", "layersTitle"));
            settingsSplit.Add(layersPane);

            toolkitSettingsScroll = new ScrollView(ScrollViewMode.Vertical);
            toolkitSettingsScroll.name = "layer-list";
            toolkitSettingsScroll.AddManipulator(new ProjectTextureDropManipulator(this));
            toolkitSettingsScroll.style.minHeight = 0f;
            toolkitSettingsScroll.style.flexGrow = 1f;
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

            toolkitPreviewCanvas = new SpritePreviewElement(previewViewport);
            toolkitPreviewCanvas.AddManipulator(new ProjectTextureDropManipulator(this, prependToRoot: true));
            toolkitPreviewCanvas.style.flexGrow = 1f;
            toolkitPreviewCanvas.style.marginLeft = PanePadding;
            toolkitPreviewCanvas.style.marginRight = PanePadding;
            toolkitPreviewCanvas.style.marginTop = PanePadding;
            toolkitPreviewCanvas.style.marginBottom = PanePadding;
            BuildPreviewZoomTool();
            BuildPreviewTransformTool();
            toolkitPreviewCanvas.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
            toolkitPreviewCanvas.RegisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
            toolkitPreviewCanvas.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);
            toolkitPreviewCanvas.RegisterCallback<PointerEnterEvent>(OnPreviewPointerEnter);
            toolkitPreviewCanvas.RegisterCallback<PointerLeaveEvent>(OnPreviewPointerLeave);
            toolkitPreviewCanvas.RegisterCallback<PointerCaptureOutEvent>(OnPreviewPointerCaptureOut);
            pane.Add(toolkitPreviewCanvas);

            pane.Add(BuildPreviewFooter());
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
                objectType = typeof(UnityEngine.Object),
                allowSceneObjects = false
            };
            toolkitDocumentField.style.flexGrow = 1f;
            toolkitDocumentField.style.minWidth = 140f;
            toolkitDocumentField.tooltip = "A Sprite Editor document or its generated texture/sprite. Double-click the saved asset in Project to edit its layers.";
            toolkitSettingsBindings.Track(toolkitDocumentField,
                () => compositor.OutputTexture != null ? (UnityEngine.Object)compositor.OutputTexture : compositor);
            toolkitDocumentField.RegisterValueChangedCallback(evt =>
            {
                TextureCompositor selected = TextureCompositor.FindDocument(evt.newValue);
                if (selected == null || selected == compositor)
                {
                    toolkitDocumentField.SetValueWithoutNotify(compositor.OutputTexture != null ? (UnityEngine.Object)compositor.OutputTexture : compositor);
                    return;
                }

                if (ResolveUnsavedTemporaryDocument())
                {
                    SetCompositor(selected);
                }
                else
                {
                    toolkitDocumentField.SetValueWithoutNotify(compositor.OutputTexture != null ? (UnityEngine.Object)compositor.OutputTexture : compositor);
                }
            });
            toolbar.Add(toolkitDocumentField);
            Button save = SpriteEditorUI.CreateToolbarButton("Save", SaveAsset, 46f);
            save.tooltip = "Save layers and update the embedded full-resolution texture and sprite (Ctrl+S).";
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
            AddTiledPreviewControl();

            toolkitPreviewActions = new VisualElement();
            toolkitPreviewActions.AddToClassList("sprite-editor-preview-actions");
            toolkitCanvasToolbar.Add(toolkitPreviewActions);
        }

        private void BuildToolkitSettings()
        {
            toolkitSettingsScroll.Clear();
            toolkitSettingsScroll.Add(BuildLayerTableHeader());

            toolkitLayerHierarchyRoot = new VisualElement();
            toolkitLayerHierarchyRoot.style.flexShrink = 0f;
            toolkitSettingsScroll.Add(toolkitLayerHierarchyRoot);

            toolkitSettingsScroll.scrollOffset = scrollPosition;
            BuildToolkitLayerFooter();
        }

        private VisualElement BuildLayerTableHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("sprite-editor-layer-table-header");
            var showAll = new Button(() =>
            {
                if (compositor == null) return;
                FinishPreviewTransform();
                FinishPaintingStroke();
                ApplyToolkitChange("Show All Layers", () => ShowAllLayers(compositor.layers));
            }) { tooltip = "Show all layers and groups" };
            showAll.AddToClassList("sprite-editor-layer-enabled");
            showAll.Add(new LayerActionIcon(LayerActionIcon.Kind.Eye));
            header.Add(showAll);
            var name = new Label("Name");
            name.AddToClassList("sprite-editor-layer-name-cell");
            header.Add(name);
            var alpha = new VisualElement { tooltip = "Opacity" };
            alpha.AddToClassList("sprite-editor-layer-opacity");
            alpha.Add(new LayerActionIcon(LayerActionIcon.Kind.Alpha));
            header.Add(alpha);
            var blend = new Label("Blend");
            blend.AddToClassList("sprite-editor-layer-blend");
            header.Add(blend);
            var menuSpace = new VisualElement { pickingMode = PickingMode.Ignore };
            menuSpace.AddToClassList("sprite-editor-layer-menu-space");
            header.Add(menuSpace);
            return header;
        }

        private static void ShowAllLayers(List<Layer> layers)
        {
            foreach (Layer layer in layers)
            {
                if (layer == null) continue;
                layer.enabled = true;
                if (layer is GroupLayer group) ShowAllLayers(group.layers);
            }
        }

        private static Label CreatePaneHeader(string text, string name)
        {
            Label header = new Label(text) { name = name, enableRichText = false };
            header.AddToClassList("sprite-editor-pane-header");
            header.EnableInClassList("sprite-editor-pane-header--light", !EditorGUIUtility.isProSkin);
            return header;
        }

        private void BuildToolkitLayerFooter()
        {
            toolkitLayerFooter.Clear();
            Button add = CreateLayerActionButton(
                LayerActionIcon.Kind.Add, "Add layer. Drop layers here to duplicate them.", ShowAddMenuForSelection);
            Button group = CreateLayerActionButton(
                LayerActionIcon.Kind.Group, "Group selected layers", GroupSelectedLayer);
            Button delete = CreateLayerActionButton(
                LayerActionIcon.Kind.Delete, "Delete selected layers", DeleteSelectedLayers);
            group.AddToClassList("sprite-editor-layer-action--separated");
            delete.AddToClassList("sprite-editor-layer-action--separated");
            group.tooltip = "Group selected layers. You can also drop layers here.";
            delete.tooltip = "Delete selected layers. You can also drop layers here.";
            add.AddManipulator(new LayerFooterDropManipulator(this, LayerFooterDropAction.Duplicate));
            group.AddManipulator(new LayerFooterDropManipulator(this, LayerFooterDropAction.Group));
            delete.AddManipulator(new LayerFooterDropManipulator(this, LayerFooterDropAction.Delete));
            toolkitLayerFooter.Add(add);
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
            row.style.paddingLeft = 3f;
            row.style.paddingRight = 3f;
            ApplyLayerSelectionStyle(row, layer.Id);
            row.style.borderTopLeftRadius = 2f;
            row.style.borderTopRightRadius = 2f;
            row.style.borderBottomLeftRadius = 2f;
            row.style.borderBottomRightRadius = 2f;
            VisualElement activeOutline = new VisualElement { pickingMode = PickingMode.Ignore };
            activeOutline.AddToClassList("sprite-editor-layer-active-outline");
            row.Add(activeOutline);
            var dropMarker = new VisualElement { pickingMode = PickingMode.Ignore };
            dropMarker.AddToClassList("sprite-editor-layer-drop-marker");
            row.Add(dropMarker);
            toolkitLayerBindings.Add(() =>
            {
                if (row != activeDropElement)
                    ApplyLayerSelectionStyle(row, layer.Id);
            });
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    evt.PreventDefault();
                    evt.StopImmediatePropagation();
                    FinishPreviewTransform();
                    FinishPaintingStroke();
                    Focus();
                    if (!IsLayerSelected(layer.Id))
                    {
                        SelectOnlyLayer(layer.Id);
                        RefreshToolkitInterface();
                    }
                    return;
                }
                if (evt.button != 0 || IsLayerDragArea(row, evt.target as VisualElement))
                    return;
                for (VisualElement field = evt.target as VisualElement; field != null && field != row; field = field.parent)
                {
                    if (!field.ClassListContains("sprite-editor-layer-multi-edit")) continue;
                    if (!IsLayerSelected(layer.Id))
                    {
                        FinishPreviewTransform();
                        FinishPaintingStroke();
                        SelectOnlyLayer(layer.Id);
                        RefreshToolkitInterface();
                    }
                    return;
                }
                if (evt.target is VisualElement menuTarget &&
                    menuTarget.ClassListContains("sprite-editor-layer-menu-button"))
                {
                    if (!IsLayerSelected(layer.Id)) SelectLayerFromPointer(layer, evt);
                    return;
                }
                SelectLayerFromPointer(layer, evt);
                if (evt.ctrlKey || evt.commandKey || evt.shiftKey)
                {
                    evt.PreventDefault();
                    evt.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);
            row.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button != 1) return;
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                if (compositor != null && compositor.TryFindLayer(layer, out List<Layer> container, out int index))
                    ShowLayerContextMenu(layer, container, index);
            }, TrickleDown.TrickleDown);
            row.AddManipulator(new LayerDragManipulator(this, layer));
            return row;
        }

        private static bool IsLayerDragArea(VisualElement row, VisualElement element)
        {
            for (; element != null && element != row; element = element.parent)
            {
                if (element.ClassListContains("sprite-editor-group-foldout")) return true;
                if (element is Button || element.focusable || element.ClassListContains("unity-base-field"))
                    return false;
            }
            return element == row;
        }

        private static VisualElement CreateLayerNameCell(VisualElement row, int depth)
        {
            var cell = new VisualElement();
            cell.AddToClassList("sprite-editor-layer-name-cell");
            cell.style.paddingLeft = depth * ToolkitLayerIndent;
            row.Add(cell);
            return cell;
        }

        private void ToggleLayerGroup(GroupLayer group)
        {
            groupExpansion[group.Id] = !GetGroupExpanded(group);
            RefreshToolkitLayerHierarchy();
        }

        private VisualElement BuildToolkitGroupRow(
            GroupLayer group,
            List<Layer> container,
            int index,
            int depth)
        {
            VisualElement row = CreateToolkitLayerRow(group, depth);

            row.Add(CreateLayerVisibilityButton(group));
            VisualElement nameCell = CreateLayerNameCell(row, depth);
            var foldout = new VisualElement { focusable = true, tooltip = "Expand or collapse group; drag to move" };
            foldout.AddToClassList("sprite-editor-group-foldout");
            foldout.EnableInClassList("sprite-editor-layer-menu-button--light", !EditorGUIUtility.isProSkin);
            foldout.Add(new Label(GetGroupExpanded(group) ? "▼" : "▶") { pickingMode = PickingMode.Ignore });
            foldout.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Space && evt.keyCode != KeyCode.Return) return;
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                ToggleLayerGroup(group);
            });
            nameCell.Add(foldout);

            TextField name = new TextField();
            name.AddToClassList("sprite-editor-layer-name");
            name.SetValueWithoutNotify(group.layerName);
            toolkitLayerBindings.Track(name, () => group.layerName);
            name.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Rename Sprite Group",
                () => group.layerName = evt.newValue));
            nameCell.Add(name);

            toolkitLayerBindings.Add(() => name.tooltip = $"{group.layers.Count} items");
            var opacity = new FloatField { tooltip = "Group opacity from 0 to 1." };
            opacity.AddToClassList("sprite-editor-layer-opacity");
            opacity.AddToClassList("sprite-editor-layer-multi-edit");
            toolkitLayerBindings.Track(opacity, () => group.opacity);
            opacity.RegisterValueChangedCallback(evt => ApplySelectedOpacity(group, evt.newValue));
            row.Add(opacity);
            var blend = LayerColorSettingsView.GroupBlend(group,
                (mode, passThrough) => ApplySelectedBlend(group, mode, passThrough), toolkitLayerBindings);
            blend.AddToClassList("sprite-editor-layer-blend");
            blend.AddToClassList("sprite-editor-layer-multi-edit");
            row.Add(blend);
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

            row.Add(CreateLayerVisibilityButton(layer));
            VisualElement nameCell = CreateLayerNameCell(row, depth);

            Image thumbnail = new Image
            {
                image = layer.GetPreviewTexture(18),
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            thumbnail.AddToClassList("sprite-editor-layer-thumbnail");
            nameCell.Add(thumbnail);
            toolkitLayerBindings.Add(() => thumbnail.image = layer.GetPreviewTexture(18));

            TextField name = new TextField();
            name.AddToClassList("sprite-editor-layer-name");
            name.SetValueWithoutNotify(layer.layerName);
            toolkitLayerBindings.Track(name, () => layer.layerName);
            name.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Rename Sprite Layer",
                () => layer.layerName = evt.newValue));
            nameCell.Add(name);

            FloatField opacity = new FloatField();
            opacity.AddToClassList("sprite-editor-layer-multi-edit");
            opacity.tooltip = "Layer opacity from 0 to 1.";
            opacity.AddToClassList("sprite-editor-layer-opacity");
            opacity.SetValueWithoutNotify(layer.opacity);
            toolkitLayerBindings.Track(opacity, () => layer.opacity);
            opacity.RegisterValueChangedCallback(evt => ApplySelectedOpacity(layer, evt.newValue));
            row.Add(opacity);

            EnumField blend = new EnumField(layer.blendMode);
            blend.AddToClassList("sprite-editor-layer-multi-edit");
            toolkitLayerBindings.Track(blend, () => (Enum)layer.blendMode);
            blend.AddToClassList("sprite-editor-layer-blend");
            blend.RegisterValueChangedCallback(evt => ApplySelectedBlend(layer, (BlendMode)evt.newValue));
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
                nameCell.Add(warning);
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

        private Button CreateLayerVisibilityButton(Layer layer)
        {
            var button = new Button(() => ApplyToolkitChange(
                layer is GroupLayer ? "Toggle Sprite Group" : "Toggle Sprite Layer",
                () => layer.enabled = !layer.enabled));
            button.AddToClassList("sprite-editor-layer-enabled");
            var eye = new LayerActionIcon(LayerActionIcon.Kind.Eye);
            eye.AddToClassList("sprite-editor-layer-eye");
            var eyeOff = new LayerActionIcon(LayerActionIcon.Kind.EyeOff);
            eyeOff.AddToClassList("sprite-editor-layer-eye-off");
            button.Add(eye);
            button.Add(eyeOff);
            void Refresh()
            {
                button.EnableInClassList("sprite-editor-layer-enabled--hidden", !layer.enabled);
                button.tooltip = layer is GroupLayer
                    ? (layer.enabled ? "Hide group and its descendants" : "Show group")
                    : (layer.enabled ? "Hide layer" : "Show layer");
            }
            Refresh();
            toolkitLayerBindings.Add(Refresh);
            return button;
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

        private sealed class LayerDragManipulator : PointerManipulator
        {
            private readonly TextureCompositorWindow owner;
            private readonly Layer layer;
            private Vector2 start;
            private int pointerId = -1;
            private VisualElement pressedFoldout;

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
                if (evt.button != 0 || pointerId >= 0 || !IsLayerDragArea(target, evt.target as VisualElement))
                    return;
                if (evt.ctrlKey || evt.commandKey || evt.shiftKey)
                {
                    owner.SelectLayerFromPointer(layer, evt, preserveSelection: true);
                    evt.StopImmediatePropagation();
                    return;
                }
                owner.FinishPreviewTransform();
                owner.FinishPaintingStroke();
                owner.activeLayerDrag?.Cancel();
                owner.activeLayerDrag = this;
                for (var element = evt.target as VisualElement; element != null && element != target; element = element.parent)
                    if (element.ClassListContains("sprite-editor-group-foldout"))
                    {
                        pressedFoldout = element;
                        element.Focus();
                        break;
                    }
                start = evt.position;
                pointerId = evt.pointerId;
                target.CapturePointer(pointerId);
                evt.StopImmediatePropagation();
            }

            private void OnPointerMove(PointerMoveEvent evt)
            {
                if (pointerId != evt.pointerId)
                    return;
                if ((evt.pressedButtons & 1) == 0)
                {
                    Release();
                    return;
                }
                if (Vector2.Distance(start, evt.position) < 4f)
                    return;

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
                bool selected = owner.IsLayerSelected(layer.Id);
                DragAndDrop.SetGenericData(DraggedLayerIdKey, selected ? owner.selectedLayerId : layer.Id);
                DragAndDrop.SetGenericData(DraggedCompositorIdKey, owner.compositor);
                DragAndDrop.SetGenericData(DraggedLayersKey, selected ? owner.GetSelectedRoots() : new List<Layer> { layer });
                DragAndDrop.StartDrag(string.IsNullOrEmpty(layer.layerName) ? "Layer" : layer.layerName);
                Release();
                evt.StopImmediatePropagation();
            }

            private void OnPointerUp(PointerUpEvent evt)
            {
                if (pointerId != evt.pointerId || evt.button != 0)
                    return;
                VisualElement foldout = pressedFoldout;
                Release();
                if (foldout != null)
                {
                    evt.StopImmediatePropagation();
                    if (layer is GroupLayer group && foldout.worldBound.Contains(evt.position))
                        owner.ToggleLayerGroup(group);
                    return;
                }
                if (owner.IsLayerSelected(layer.Id))
                    owner.ActivateSelectedLayer(layer.Id);
                else
                    owner.SelectOnlyLayer(layer.Id);
                owner.selectionAnchorId = layer.Id;
                owner.RefreshToolkitInterface();
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
                pressedFoldout = null;
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
            zone.userData = destination;
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

            if (element.userData is string)
            {
                element.EnableInClassList("sprite-editor-layer-row--drop-inside", insideGroup);
                element.EnableInClassList("sprite-editor-layer-row--drop-before", !insideGroup && insertBefore);
                element.EnableInClassList("sprite-editor-layer-row--drop-after", !insideGroup && !insertBefore);
                return;
            }

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

            activeDropElement.RemoveFromClassList("sprite-editor-layer-row--drop-inside");
            activeDropElement.RemoveFromClassList("sprite-editor-layer-row--drop-before");
            activeDropElement.RemoveFromClassList("sprite-editor-layer-row--drop-after");

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

            if (toolkitHeaderBuilt)
            {
                toolkitHeaderBindings.Refresh(forceValues);
                return;
            }

            toolkitHeaderBuilt = true;
            CancelPreviewZoomGesture();
            toolkitHeaderBindings.Clear();
            toolkitPreviewHeader.Clear();
            BuildToolkitPreviewHeader();
            toolkitHeaderBindings.Refresh(forceValues);
        }

        private void BuildToolkitPreviewHeader()
        {
            toolkitPreviewActions.Clear();
            Button clear = SpriteEditorUI.CreateToolbarButton("Clear", () =>
            {
                if (GetSelectedLayer() is DrawingLayer drawing) ClearDrawingLayer(drawing);
            }, 46f);
            toolkitHeaderBindings.Add(() => clear.SetEnabled(GetSelectedLayer() is DrawingLayer));
            toolkitPreviewActions.Add(clear);
            toolkitPreviewActions.Add(SpriteEditorUI.CreateToolbarButton("Refresh", () => RequestPreview(true), 64f));
            AddPreviewTransformSettings();
            AddPreviewZoomSettings();

            VisualElement emptyRow = SpriteEditorUI.CreateToolbar();
            toolkitHeaderBindings.Add(() => emptyRow.EnableInClassList("sprite-editor-tool-options--hidden",
                previewTool != PreviewTool.None || previewSettingsTool != PreviewTool.None));
            toolkitPreviewHeader.Add(emptyRow);

            AddFillSettings();
            VisualElement brushRow = SpriteEditorUI.CreateToolbar();
            BindPreviewSettingsRow(brushRow, PreviewTool.Brush);
            EnumField tool = CompactField(new EnumField(paintSettings.tool), 72f);
            toolkitHeaderBindings.Track(tool, () => (Enum)paintSettings.tool);
            tool.RegisterValueChangedCallback(evt => ApplyPaintToolChange(
                () => paintSettings.tool = (PaintToolMode)evt.newValue));
            brushRow.Add(tool);

            AddPaintColorFields(brushRow);

            FloatField size = CompactField(new FloatField("Size"), 76f);
            size.AddToClassList("sprite-editor-brush-size");
            size.SetValueWithoutNotify(paintSettings.brushSize);
            toolkitHeaderBindings.Track(size, () => paintSettings.brushSize);
            size.RegisterValueChangedCallback(evt => ApplyPaintToolChange(
                () => paintSettings.brushSize = Mathf.Max(1f, evt.newValue)));
            brushRow.Add(size);
            brushRow.Add(CreateCompactLabel("Hard", 32f));
            Slider hardness = new Slider(0f, 1f) { value = paintSettings.brushHardness };
            hardness.style.flexGrow = 1f;
            hardness.style.minWidth = 42f;
            Label hardnessValue = CreateCompactLabel($"{paintSettings.brushHardness * 100f:0}%", 38f);
            toolkitHeaderBindings.Track(hardness, () => paintSettings.brushHardness);
            toolkitHeaderBindings.Add(() => hardnessValue.text = $"{paintSettings.brushHardness * 100f:0}%");
            hardness.RegisterValueChangedCallback(evt =>
            {
                hardnessValue.text = $"{evt.newValue * 100f:0}%";
                ApplyPaintToolChange(() => paintSettings.brushHardness = Mathf.Clamp01(evt.newValue));
            });
            brushRow.Add(hardness);
            brushRow.Add(hardnessValue);
            FloatField spacing = CompactField(new FloatField("Step"), 72f);
            spacing.tooltip = BrushSpacingContent.tooltip;
            spacing.labelElement.style.width = 30f;
            spacing.labelElement.style.minWidth = 30f;
            spacing.labelElement.style.flexShrink = 0f;
            spacing.SetValueWithoutNotify(paintSettings.brushSpacing * 100f);
            toolkitHeaderBindings.Track(spacing, () => paintSettings.brushSpacing * 100f);
            spacing.RegisterValueChangedCallback(evt =>
            {
                float clampedPercent = Mathf.Clamp(
                    evt.newValue,
                    DrawingLayer.MinimumBrushSpacing * 100f,
                    DrawingLayer.MaximumBrushSpacing * 100f);
                ApplyPaintToolChange(
                    () => paintSettings.brushSpacing = clampedPercent * 0.01f);
            });
            brushRow.Add(spacing);
            brushRow.Add(CreateCompactLabel("%", 14f));
            toolkitPreviewHeader.Add(brushRow);
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
            RefreshPreviewToolToolbar();
            RefreshPreviewTransformTool();
            bool transforming = IsPreviewTransformEnabled;
            toolkitPreviewCanvas.SetTiled(tiledPreview);
            toolkitPreviewCanvas.SetDocument(channelPreviewTexture != null ? (Texture)channelPreviewTexture : previewTexture,
                compositor.width, compositor.height,
                IsPreviewBrushEnabled ? drawing : null, transforming,
                previewTool == PreviewTool.Brush ? paintSettings : null);
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
                else if (IsPreviewZoomEnabled)
                {
                    toolkitPreviewFooter.text = "Click zoom in • Alt-click zoom out • Area drag to frame • MMB pan • Wheel zoom • Esc cancel";
                }
                else if (IsPreviewFillEnabled)
                {
                    toolkitPreviewFooter.text = "LMB fill • G fill tool • X colors • All Layers / Contiguous / Tolerance / Antialias / Expand";
                }
                else if (IsPreviewBrushEnabled)
                {
                    toolkitPreviewFooter.text = previewTexture != null
                        ? $"LMB paint • RMB erase • Shift lines • X colors • [ ] size • {paintSettings.brushSize:0.#} px"
                        : "Rendering painting preview…";
                }
                else if (previewTool == PreviewTool.Brush || previewTool == PreviewTool.Fill)
                {
                    toolkitPreviewFooter.text = GetSelectedLayer() == null
                        ? "Select a layer to paint or fill • Tool settings are shared"
                        : "Click to convert the selected layer to Drawing • Tool settings are shared";
                }
                else if (previewTool == PreviewTool.Transform)
                {
                    toolkitPreviewFooter.text = "Select a non-group layer to transform";
                }
                else
                {
                    toolkitPreviewFooter.text = previewTexture != null
                        ? (tiledPreview ? "Tiled canvas • seamless brush and eraser • auto refresh" : "Transparent canvas • auto refresh")
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
            if (HandlePaintConversionPrompt(evt)) return;
            if (HandleFillPointerDown(evt)) return;
            DrawingLayer layer = GetSelectedLayer() as DrawingLayer;
            if (!IsPreviewBrushEnabled || paintingLayer != null || layer == null || (evt.button != 0 && evt.button != 1) || evt.altKey)
                return;
            if (!PreviewContainsPaintPoint(evt.localPosition) ||
                !TryMapPreviewToLayerUv(evt.localPosition, toolkitPreviewCanvas.ImageRect, layer, out Vector2 startUv))
            {
                return;
            }

            Focus();
            toolkitPreviewCanvas.Focus();
            bool erase = evt.button == 1 || paintSettings.tool == PaintToolMode.Eraser;
            if (!erase && (previewChannels & 8) == 0)
            {
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                return;
            }
            paintingLayer = layer;
            paintingMouseButton = evt.button;
            paintingPointerId = evt.pointerId;
            paintingErase = erase;
            paintingPointerMoved = false;
            bool connect = evt.shiftKey && ReferenceEquals(lineAnchorLayer, layer) &&
                           lineAnchorCanvasSize == new Vector2Int(compositor.width, compositor.height);
            Vector2 originUv = connect ? lineAnchorUv : startUv;
            lastPaintingUv = originUv;
            hasLastPaintingUv = true;
            toolkitPreviewCanvas.CapturePointer(evt.pointerId);
            Undo.RecordObject(compositor, "Paint Stroke");
            layer.PrepareStroke(compositor.width, compositor.height, "Paint Stroke");
            if (tiledPreview)
                layer.BeginTiledStroke(originUv, compositor.width, compositor.height);
            else
                layer.BeginStroke(originUv);
            RememberPaintingPoint(originUv);
            if (connect && originUv != startUv)
                PaintTowardsLayerPoint(startUv);
            else
                layer.PaintPoint(startUv, compositor.width, compositor.height, GetPaintingParameters());
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

            if (toolkitPreviewCanvas.contentRect.Contains(evt.localPosition) && TryMapPreviewToLayerUv(
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
                    paintingLayer.PaintSegment(lastPaintingUv, clippedUv, compositor.width, compositor.height, false,
                        GetPaintingParameters());
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
                paintingLayer.PaintSegment(lastPaintingUv, pointUv, compositor.width, compositor.height, false,
                    GetPaintingParameters());
            }
            else
            {
                paintingLayer.PaintPoint(pointUv, compositor.width, compositor.height, GetPaintingParameters());
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
            if (paintingPointerMoved && toolkitPreviewCanvas.contentRect.Contains(evt.localPosition) &&
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
            bool visible = previewTool == PreviewTool.Brush &&
                           !(previewZoomManipulator?.IsPanning ?? false) &&
                           !alt &&
                           PreviewContainsPaintPoint(localPosition);
            toolkitPreviewCanvas?.SetCursor(
                visible,
                localPosition,
                paintingLayer != null ? paintingErase : paintSettings.tool == PaintToolMode.Eraser);
        }

        private void OnToolkitKeyDown(KeyDownEvent evt)
        {
            if ((evt.ctrlKey || evt.commandKey) && !evt.altKey && !evt.shiftKey && evt.keyCode == KeyCode.S)
            {
                ResetOpacityEntry();
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                if (compositor != null && AssetDatabase.Contains(compositor))
                    SaveAsset();
                else
                    SaveAsAsset();
                return;
            }

            if (IsTextInputTarget(evt.target as VisualElement))
            {
                ResetOpacityEntry();
                return;
            }

            if (HandleOpacityKey(evt))
                return;
            ResetOpacityEntry();

            if (evt.keyCode == KeyCode.Escape && previewZoomManipulator != null && previewZoomManipulator.IsDragging)
            {
                CancelPreviewZoomGesture();
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                return;
            }

            if (HandlePreviewTransformKey(evt))
                return;

            if (paintingLayer != null && (evt.keyCode == KeyCode.LeftShift || evt.keyCode == KeyCode.RightShift))
            {
                SetPaintingShift(true);
                evt.StopImmediatePropagation();
                return;
            }

            bool actionModifier = evt.ctrlKey || evt.commandKey;
            if (actionModifier && !evt.shiftKey && evt.keyCode == KeyCode.E)
            {
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                if (compositor != null) MergeSelectedLayers(GetSelectedRoots(), evt.altKey);
                return;
            }
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

            if (previewTool != PreviewTool.Brush && previewTool != PreviewTool.Fill)
                return;

            bool swapColors = !actionModifier && !evt.altKey && evt.keyCode == KeyCode.X;
            if (swapColors)
            {
                ApplyPaintToolChange(paintSettings.SwapBrushColors);
                evt.PreventDefault();
                evt.StopImmediatePropagation();
                return;
            }

            if (previewTool != PreviewTool.Brush) return;
            bool decrease = evt.keyCode == KeyCode.LeftBracket || evt.character == '[';
            bool increase = evt.keyCode == KeyCode.RightBracket || evt.character == ']';
            if (!decrease && !increase)
                return;

            float nextSize = decrease
                ? Mathf.Max(1f, Mathf.Round(paintSettings.brushSize / 1.2f))
                : Mathf.Max(1f, Mathf.Round(paintSettings.brushSize * 1.2f));
            if (Mathf.Approximately(nextSize, paintSettings.brushSize))
                nextSize = Mathf.Max(1f, paintSettings.brushSize + (increase ? 1f : -1f));
            ApplyPaintToolChange(() => paintSettings.brushSize = nextSize);
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
            private readonly PreviewViewport viewport;
            private readonly VisualElement checker;
            private Texture2D checkerTexture;
            private readonly Image image;
            private readonly VisualElement tiledImage;
            private readonly VisualElement overlay;
            private Texture texture;
            private DrawingLayer drawingLayer;
            private PaintToolSettings brushSettings;
            private int documentWidth = 1;
            private int documentHeight = 1;
            private bool cursorVisible;
            private bool cursorErase;
            private bool transformMode;
            private Vector2 cursorPosition;
            private bool tiled;
            private Rect presentationRect;

            public Rect ImageRect { get; private set; }
            public float PixelScale => ImageRect.width / Mathf.Max(1, documentWidth);
            public event Action ViewChanged;

            public SpritePreviewElement(PreviewViewport viewport)
            {
                this.viewport = viewport;
                AddToClassList("sprite-editor-preview-canvas");
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
                RegisterCallback<AttachToPanelEvent>(_ => CreateCheckerTexture());
                RegisterCallback<DetachFromPanelEvent>(_ => ReleaseCheckerTexture());

                image = new Image
                {
                    scaleMode = ScaleMode.StretchToFill,
                    pickingMode = PickingMode.Ignore
                };
                image.style.position = Position.Absolute;
                Add(image);

                tiledImage = new VisualElement { pickingMode = PickingMode.Ignore };
                tiledImage.AddToClassList("sprite-editor-tiled-image");
                tiledImage.generateVisualContent += DrawTiledImage;
                Add(tiledImage);

                overlay = new VisualElement { pickingMode = PickingMode.Ignore };
                overlay.style.position = Position.Absolute;
                overlay.generateVisualContent += DrawOverlay;
                Add(overlay);

                RegisterCallback<GeometryChangedEvent>(_ => UpdateImageLayout());
            }

            public void SetTiled(bool enabled)
            {
                if (tiled == enabled) return;
                tiled = enabled;
                image.EnableInClassList("sprite-editor-preview-image--hidden", tiled);
                tiledImage.EnableInClassList("sprite-editor-preview-image--visible", tiled);
                UpdateImageLayout(true);
            }

            public void SetDocument(Texture nextTexture, int width, int height, DrawingLayer layer, bool transforming = false, PaintToolSettings brush = null)
            {
                transformMode = transforming;
                texture = nextTexture;
                if (texture is RenderTexture) texture.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                documentWidth = Mathf.Max(1, width);
                documentHeight = Mathf.Max(1, height);
                drawingLayer = layer;
                brushSettings = brush;
                if (brushSettings == null)
                    cursorVisible = false;
                overlay.style.display = brushSettings == null
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
                image.image = texture;
                image.MarkDirtyRepaint();
                tiledImage.MarkDirtyRepaint();
                UpdateImageLayout();
                overlay.MarkDirtyRepaint();
            }

            public void ClearTexture()
            {
                texture = null;
                image.image = null;
                tiledImage.MarkDirtyRepaint();
            }

            public void SetCursor(bool visible, Vector2 position, bool erase)
            {
                cursorVisible = visible;
                cursorPosition = position;
                cursorErase = erase;
                overlay.MarkDirtyRepaint();
            }

            public void ZoomAt(Vector2 point, float scale)
            {
                viewport.ZoomAt(contentRect, new Vector2(documentWidth, documentHeight), ImageRect, point, scale);
                UpdateImageLayout();
            }

            public void Frame(Rect region)
            {
                viewport.Frame(contentRect, new Vector2(documentWidth, documentHeight), ImageRect, region);
                UpdateImageLayout();
            }

            public void Pan(Vector2 delta)
            {
                viewport.Pan(contentRect, new Vector2(documentWidth, documentHeight), ImageRect, delta);
                UpdateImageLayout();
            }

            public void UpdateImageLayout()
            {
                UpdateImageLayout(false);
            }

            private void UpdateImageLayout(bool force)
            {
                Rect nextRect = viewport.ImageRect(contentRect, new Vector2(documentWidth, documentHeight));
                Rect nextPresentation = tiled ? contentRect : nextRect;
                if (!force && nextRect == ImageRect && nextPresentation == presentationRect)
                    return;
                ImageRect = nextRect;
                presentationRect = nextPresentation;
                PositionElement(checker, presentationRect);
                PositionElement(image, ImageRect);
                PositionElement(tiledImage, contentRect);
                PositionElement(overlay, presentationRect);
                checker.MarkDirtyRepaint();
                tiledImage.MarkDirtyRepaint();
                overlay.MarkDirtyRepaint();
                ViewChanged?.Invoke();
            }

            private static void PositionElement(VisualElement element, Rect rect)
            {
                element.style.left = rect.x;
                element.style.top = rect.y;
                element.style.width = rect.width;
                element.style.height = rect.height;
            }

            private void CreateCheckerTexture()
            {
                if (checkerTexture != null) return;
                Color light = EditorGUIUtility.isProSkin
                    ? new Color(0.30f, 0.30f, 0.30f, 1f)
                    : new Color(0.84f, 0.84f, 0.84f, 1f);
                Color dark = EditorGUIUtility.isProSkin
                    ? new Color(0.23f, 0.23f, 0.23f, 1f)
                    : new Color(0.70f, 0.70f, 0.70f, 1f);
                checkerTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = "Sprite Editor Checkerboard",
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Repeat
                };
                checkerTexture.SetPixels(new[] { light, dark, dark, light });
                checkerTexture.Apply(false, false);
                checker.MarkDirtyRepaint();
            }

            public void ReleaseCheckerTexture()
            {
                if (checkerTexture == null) return;
                UnityEngine.Object.DestroyImmediate(checkerTexture);
                checkerTexture = null;
            }

            private void DrawCheckerboard(MeshGenerationContext context)
            {
                Rect rect = checker.contentRect;
                if (checkerTexture == null || rect.width <= 0f || rect.height <= 0f) return;

                float u = rect.width / 32f;
                float v = rect.height / 32f;
                context.AllocateTempMesh(4, 6, out var vertices, out var indices);
                vertices[0] = new Vertex { position = new Vector3(rect.xMin, rect.yMin, Vertex.nearZ), tint = Color.white, uv = Vector2.zero };
                vertices[1] = new Vertex { position = new Vector3(rect.xMax, rect.yMin, Vertex.nearZ), tint = Color.white, uv = new Vector2(u, 0f) };
                vertices[2] = new Vertex { position = new Vector3(rect.xMax, rect.yMax, Vertex.nearZ), tint = Color.white, uv = new Vector2(u, v) };
                vertices[3] = new Vertex { position = new Vector3(rect.xMin, rect.yMax, Vertex.nearZ), tint = Color.white, uv = new Vector2(0f, v) };
                indices[0] = 0;
                indices[1] = 1;
                indices[2] = 2;
                indices[3] = 0;
                indices[4] = 2;
                indices[5] = 3;
#if UNITY_6000_3_OR_NEWER
                context.DrawMesh(vertices, indices, checkerTexture, TextureOptions.SkipDynamicAtlas);
#else
                context.DrawMesh(vertices, indices, checkerTexture);
#endif
            }

            private void DrawTiledImage(MeshGenerationContext context)
            {
                Rect rect = tiledImage.contentRect;
                if (!tiled || texture == null || rect.width <= 0f || rect.height <= 0f ||
                    ImageRect.width <= 0f || ImageRect.height <= 0f) return;
                float left = Mathf.Repeat((contentRect.xMin - ImageRect.xMin) / ImageRect.width, 1f);
                float top = Mathf.Repeat(1f - (contentRect.yMin - ImageRect.yMin) / ImageRect.height, 1f);
                float right = left + rect.width / ImageRect.width;
                float bottom = top - rect.height / ImageRect.height;
                context.AllocateTempMesh(4, 6, out var vertices, out var indices);
                vertices[0] = new Vertex { position = new Vector3(rect.xMin, rect.yMin, Vertex.nearZ), tint = Color.white, uv = new Vector2(left, top) };
                vertices[1] = new Vertex { position = new Vector3(rect.xMax, rect.yMin, Vertex.nearZ), tint = Color.white, uv = new Vector2(right, top) };
                vertices[2] = new Vertex { position = new Vector3(rect.xMax, rect.yMax, Vertex.nearZ), tint = Color.white, uv = new Vector2(right, bottom) };
                vertices[3] = new Vertex { position = new Vector3(rect.xMin, rect.yMax, Vertex.nearZ), tint = Color.white, uv = new Vector2(left, bottom) };
                indices[0] = 0; indices[1] = 1; indices[2] = 2;
                indices[3] = 0; indices[4] = 2; indices[5] = 3;
#if UNITY_6000_3_OR_NEWER
                context.DrawMesh(vertices, indices, texture, TextureOptions.SkipDynamicAtlas);
#else
                context.DrawMesh(vertices, indices, texture);
#endif
            }

            private void DrawOverlay(MeshGenerationContext context)
            {
                if (brushSettings == null)
                    return;

                Rect rect = new Rect(ImageRect.position - presentationRect.position, ImageRect.size);
                Painter2D painter = context.painter2D;
                Color guide = new Color(0.20f, 0.70f, 1f, 0.55f);
                painter.lineWidth = 1f;
                painter.strokeColor = guide;

                if (drawingLayer != null)
                    DrawPatternGuides(painter, rect);

                if (!cursorVisible)
                    return;
                Vector2 localCursor = cursorPosition - presentationRect.position;
                float pixelScale = PixelScale;
                float transformScale = drawingLayer == null ? 1f :
                    (Mathf.Abs(drawingLayer.transform.scale.x) + Mathf.Abs(drawingLayer.transform.scale.y)) * 0.5f;
                float radius = Mathf.Max(
                    2f,
                    brushSettings.brushSize * pixelScale * Mathf.Max(0.0001f, transformScale) * 0.5f);
                painter.lineWidth = 1f;
                painter.strokeColor = new Color(0f, 0f, 0f, 0.95f);
                StrokeCircle(painter, localCursor, radius + 1f);
                painter.strokeColor = cursorErase
                    ? new Color(1f, 0.35f, 0.25f, 1f)
                    : new Color(1f, 1f, 1f, 0.95f);
                StrokeCircle(painter, localCursor, radius);
            }

            private void DrawPatternGuides(Painter2D painter, Rect rect)
            {
                if (drawingLayer.UsesMirrorPattern && drawingLayer.mirrorAcrossVerticalAxis)
                    DrawMirrorGuide(painter, rect, true);
                if (drawingLayer.UsesMirrorPattern && drawingLayer.mirrorAcrossHorizontalAxis)
                    DrawMirrorGuide(painter, rect, false);

                int count = Mathf.Clamp(drawingLayer.repeatCount, 2, 64);
                if (drawingLayer.repeatMode == PaintRepeatMode.Horizontal ||
                    drawingLayer.repeatMode == PaintRepeatMode.Grid)
                {
                    for (int i = 1; i < count; i++)
                    {
                        float x = rect.width * i / count;
                        StrokeLine(painter, rect.position + new Vector2(x, 0f), rect.position + new Vector2(x, rect.height));
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
                        StrokeLine(painter, rect.position + new Vector2(0f, y), rect.position + new Vector2(rect.width, y));
                    }
                }
                if (drawingLayer.repeatMode == PaintRepeatMode.Radial)
                {
                    Vector2 center = rect.position + new Vector2(
                        rect.width * drawingLayer.patternCenter.x,
                        rect.height * (1f - drawingLayer.patternCenter.y));
                    float length = Mathf.Sqrt(rect.width * rect.width + rect.height * rect.height);
                    for (int i = 0; i < count; i++)
                    {
                        float angle = drawingLayer.RadialStartAngleRadians + i * Mathf.PI * 2f / count;
                        Vector2 direction = new Vector2(
                            Mathf.Cos(angle) * rect.width / Mathf.Max(1, documentWidth),
                            -Mathf.Sin(angle) * rect.height / Mathf.Max(1, documentHeight)).normalized;
                        StrokeLine(painter, center, center + direction * length);
                    }
                }
            }

            private void DrawMirrorGuide(Painter2D painter, Rect rect, bool vertical)
            {
                Vector2 center = rect.position + new Vector2(rect.width * drawingLayer.patternCenter.x,
                    rect.height * (1f - drawingLayer.patternCenter.y));
                Vector2 axis = drawingLayer.GetMirrorAxisDirection(vertical);
                Vector2 direction = new Vector2(axis.x * rect.width / Mathf.Max(1, documentWidth),
                    -axis.y * rect.height / Mathf.Max(1, documentHeight)).normalized;
                float forward = float.PositiveInfinity, backward = float.PositiveInfinity;
                if (Mathf.Abs(direction.x) > 0.000001f)
                {
                    forward = (direction.x > 0f ? rect.xMax - center.x : rect.xMin - center.x) / direction.x;
                    backward = (direction.x > 0f ? center.x - rect.xMin : center.x - rect.xMax) / direction.x;
                }
                if (Mathf.Abs(direction.y) > 0.000001f)
                {
                    forward = Mathf.Min(forward, (direction.y > 0f ? rect.yMax - center.y : rect.yMin - center.y) / direction.y);
                    backward = Mathf.Min(backward, (direction.y > 0f ? center.y - rect.yMin : center.y - rect.yMax) / direction.y);
                }
                if (!float.IsInfinity(forward) && !float.IsInfinity(backward))
                    StrokeLine(painter, center - direction * backward, center + direction * forward);
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
