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
        [NonSerialized] private VisualElement toolkitDocumentRoot;
        [NonSerialized] private ScrollView toolkitSettingsScroll;
        [NonSerialized] private SpritePreviewElement toolkitPreviewCanvas;
        [NonSerialized] private Label toolkitPreviewFooter;
        [NonSerialized] private VisualElement toolkitPreviewErrorRoot;
        [NonSerialized] private ObjectField toolkitDocumentField;
        [NonSerialized] private VisualElement toolkitLayerHierarchyRoot;
        [NonSerialized] private VisualElement activeDropElement;
        [NonSerialized] private string layerDragPointerCandidateId;
        [NonSerialized] private Vector2 layerDragPointerStart;
        [NonSerialized] private int layerDragPointerId = -1;
        [NonSerialized] private int paintingPointerId = -1;
        [NonSerialized] private bool applyingToolkitChange;
        [NonSerialized] private bool rebuildingToolkit;

        public void CreateGUI()
        {
            if (compositor == null)
                SetCompositor(CreateTemporaryCompositor());

            VisualElement root = rootVisualElement;
            root.Clear();
            root.focusable = true;
            root.style.flexGrow = 1f;
            root.style.backgroundColor = SpriteEditorUI.PanelColor;
            root.RegisterCallback<KeyDownEvent>(OnToolkitKeyDown, TrickleDown.TrickleDown);
            root.RegisterCallback<DragExitedEvent>(_ =>
            {
                ClearToolkitDropIndicator();
                ClearLayerDragData();
            });

            TwoPaneSplitView split = new TwoPaneSplitView(
                0,
                Mathf.Max(PreviewPaneMinWidth, previewPaneWidth),
                TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1f;
            root.Add(split);

            toolkitPreviewPane = BuildToolkitPreviewPane();
            toolkitPreviewPane.style.minWidth = PreviewPaneMinWidth;
            toolkitPreviewPane.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.newRect.width >= PreviewPaneMinWidth)
                    previewPaneWidth = evt.newRect.width;
            });
            split.Add(toolkitPreviewPane);

            VisualElement settingsPane = new VisualElement();
            settingsPane.style.minWidth = SettingsPaneMinWidth;
            settingsPane.style.flexGrow = 1f;
            settingsPane.style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.13f, 0.13f, 0.13f, 1f)
                : new Color(0.76f, 0.76f, 0.76f, 1f);
            split.Add(settingsPane);

            toolkitDocumentRoot = new VisualElement();
            settingsPane.Add(toolkitDocumentRoot);
            toolkitSettingsScroll = new ScrollView(ScrollViewMode.Vertical);
            toolkitSettingsScroll.style.flexGrow = 1f;
            toolkitSettingsScroll.style.paddingLeft = 8f;
            toolkitSettingsScroll.style.paddingRight = 8f;
            toolkitSettingsScroll.style.paddingBottom = 8f;
            settingsPane.Add(toolkitSettingsScroll);

            RebuildToolkitInterface();
        }

        private VisualElement BuildToolkitPreviewPane()
        {
            VisualElement pane = new VisualElement();
            pane.style.flexDirection = FlexDirection.Column;
            pane.style.backgroundColor = SpriteEditorUI.PanelColor;

            toolkitPreviewHeader = new VisualElement();
            toolkitPreviewHeader.style.flexShrink = 0f;
            pane.Add(toolkitPreviewHeader);

            toolkitPreviewErrorRoot = new VisualElement();
            toolkitPreviewErrorRoot.style.flexShrink = 0f;
            toolkitPreviewErrorRoot.style.paddingLeft = PanePadding;
            toolkitPreviewErrorRoot.style.paddingRight = PanePadding;
            pane.Add(toolkitPreviewErrorRoot);

            toolkitPreviewCanvas = new SpritePreviewElement();
            toolkitPreviewCanvas.style.flexGrow = 1f;
            toolkitPreviewCanvas.style.marginLeft = PanePadding;
            toolkitPreviewCanvas.style.marginRight = PanePadding;
            toolkitPreviewCanvas.style.marginTop = PanePadding;
            toolkitPreviewCanvas.style.marginBottom = PanePadding;
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

        private void RebuildToolkitInterface()
        {
            if (rootVisualElement == null || toolkitDocumentRoot == null || rebuildingToolkit)
                return;

            rebuildingToolkit = true;
            try
            {
                Vector2 previousScroll = toolkitSettingsScroll?.scrollOffset ?? scrollPosition;
                BuildToolkitDocumentArea();
                BuildToolkitSettings(previousScroll);
                BuildToolkitPreviewHeader();
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
                    RebuildToolkitInterface();
                }
                else
                {
                    toolkitDocumentField.SetValueWithoutNotify(compositor);
                }
            });
            toolbar.Add(toolkitDocumentField);
            toolbar.Add(SpriteEditorUI.CreateToolbarButton("New", () =>
            {
                if (!ResolveUnsavedTemporaryDocument())
                    return;
                SetCompositor(CreateTemporaryCompositor());
                RebuildToolkitInterface();
            }, 46f));
            toolbar.Add(SpriteEditorUI.CreateToolbarButton("Save As", () =>
            {
                if (SaveAsAsset())
                    RebuildToolkitInterface();
            }, 64f));
            toolkitDocumentRoot.Add(toolbar);

            if (!AssetDatabase.Contains(compositor))
            {
                SpriteEditorUI.AddHelpBox(
                    toolkitDocumentRoot,
                    temporaryDocumentDirty
                        ? "Unsaved compositor. Use Save As to keep this layer tree."
                        : "Temporary compositor. It can be exported directly or saved as an asset.",
                    temporaryDocumentDirty ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info);
            }
        }

        private void BuildToolkitSettings(Vector2 previousScroll)
        {
            toolkitSettingsScroll.Clear();
            toolkitSettingsScroll.Add(SpriteEditorUI.CreateHeading("Output"));

            VisualElement output = SpriteEditorUI.CreateRow();
            IntegerField width = new IntegerField("Width");
            width.style.flexGrow = 1f;
            width.SetValueWithoutNotify(compositor.width);
            width.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Sprite Output Width",
                () => compositor.width = Mathf.Max(1, evt.newValue),
                rebuildHeader: true));
            output.Add(width);

            IntegerField height = new IntegerField("Height");
            height.style.flexGrow = 1f;
            height.SetValueWithoutNotify(compositor.height);
            height.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Sprite Output Height",
                () => compositor.height = Mathf.Max(1, evt.newValue),
                rebuildHeader: true));
            output.Add(height);
            toolkitSettingsScroll.Add(output);

            VisualElement layerHeader = SpriteEditorUI.CreateRow();
            layerHeader.style.marginTop = 8f;
            Label title = SpriteEditorUI.CreateHeading("Layers (top to bottom)");
            title.style.flexGrow = 1f;
            title.style.marginTop = 0f;
            layerHeader.Add(title);
            Label dragHint = new Label("≡ drag");
            dragHint.tooltip = LayerDragHintContent.tooltip;
            dragHint.style.fontSize = 10f;
            dragHint.style.marginRight = 4f;
            layerHeader.Add(dragHint);
            layerHeader.Add(SpriteEditorUI.CreateButton("Add", ShowAddMenuForSelection, 54f));
            Button group = SpriteEditorUI.CreateButton("Group", GroupSelectedLayer, 54f);
            group.SetEnabled(GetSelectedLayer() != null);
            layerHeader.Add(group);
            toolkitSettingsScroll.Add(layerHeader);

            toolkitLayerHierarchyRoot = new VisualElement();
            toolkitLayerHierarchyRoot.style.flexShrink = 0f;
            toolkitSettingsScroll.Add(toolkitLayerHierarchyRoot);
            RebuildToolkitLayerHierarchy();

            Button export = SpriteEditorUI.CreateButton("Export PNG", ExportTexture);
            export.style.marginTop = 10f;
            export.style.height = 24f;
            toolkitSettingsScroll.Add(export);

            scrollPosition = previousScroll;
            toolkitSettingsScroll.schedule.Execute(() =>
            {
                if (toolkitSettingsScroll != null)
                    toolkitSettingsScroll.scrollOffset = scrollPosition;
            });
        }

        private void RebuildToolkitLayerHierarchy()
        {
            if (toolkitLayerHierarchyRoot == null)
                return;

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
            row.style.backgroundColor = layer.Id == selectedLayerId
                ? SpriteEditorUI.SelectedColor
                : SpriteEditorUI.RowColor;
            row.style.borderTopLeftRadius = 2f;
            row.style.borderTopRightRadius = 2f;
            row.style.borderBottomLeftRadius = 2f;
            row.style.borderBottomRightRadius = 2f;
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || selectedLayerId == layer.Id)
                    return;
                selectedLayerId = layer.Id;
                RebuildToolkitLayerHierarchy();
                BuildToolkitPreviewHeader();
                UpdateToolkitPreviewPresentation();
            });
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
            enabled.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Toggle Sprite Group",
                () => group.enabled = evt.newValue));
            row.Add(enabled);

            Button foldout = SpriteEditorUI.CreateButton(GetGroupExpanded(group) ? "▼" : "▶", () =>
            {
                groupExpansion[group.Id] = !GetGroupExpanded(group);
                RebuildToolkitLayerHierarchy();
            }, 22f);
            foldout.tooltip = "Expand or collapse this group.";
            row.Add(foldout);

            TextField name = new TextField();
            name.style.flexGrow = 1f;
            name.SetValueWithoutNotify(group.layerName);
            name.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Rename Sprite Group",
                () => group.layerName = evt.newValue));
            row.Add(name);

            Label count = new Label($"{group.layers.Count} items");
            count.style.width = 52f;
            count.style.fontSize = 10f;
            row.Add(count);
            row.Add(SpriteEditorUI.CreateButton("+", () => ShowAddMenu(group.layers, 0), 24f));
            row.Add(SpriteEditorUI.CreateButton("…", () => ShowLayerContextMenu(group, container, index), 30f));
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

            TextField name = new TextField();
            name.style.flexGrow = 1f;
            name.style.minWidth = 72f;
            name.SetValueWithoutNotify(layer.layerName);
            name.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Rename Sprite Layer",
                () => layer.layerName = evt.newValue));
            row.Add(name);

            FloatField opacity = new FloatField();
            opacity.tooltip = "Layer opacity from 0 to 1.";
            opacity.style.width = 48f;
            opacity.SetValueWithoutNotify(layer.opacity);
            opacity.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Layer Opacity",
                () => layer.opacity = Mathf.Clamp01(evt.newValue)));
            row.Add(opacity);

            EnumField blend = new EnumField(layer.blendMode);
            blend.style.width = 126f;
            blend.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Layer Blend Mode",
                () => layer.blendMode = (BlendMode)evt.newValue));
            row.Add(blend);

            if (layer is TargetedLayerEffect effect &&
                !compositor.HasUsableEffectInput(effect, container, index))
            {
                Label warning = new Label("!");
                warning.tooltip = effect.inputMode == EffectInputMode.Specific
                    ? "Select an existing non-cyclic target layer or group in the effect settings."
                    : "This effect needs a layer or group directly below it.";
                warning.style.color = new Color(1f, 0.65f, 0.15f, 1f);
                warning.style.unityFontStyleAndWeight = FontStyle.Bold;
                warning.style.width = 12f;
                row.Add(warning);
            }

            string editLabel = layer is DrawingLayer ? "Paint" : "Edit";
            row.Add(SpriteEditorUI.CreateButton(editLabel, () =>
            {
                if (layer is DrawingLayer)
                {
                    selectedLayerId = layer.Id;
                    RebuildToolkitLayerHierarchy();
                    BuildToolkitPreviewHeader();
                    UpdateToolkitPreviewPresentation();
                    toolkitPreviewCanvas?.Focus();
                }
                else
                {
                    OpenLayerEditor(layer);
                }
            }, 42f));
            row.Add(SpriteEditorUI.CreateButton("FX", () => ModifierEditorWindow.Open(layer, compositor), 30f));
            row.Add(SpriteEditorUI.CreateButton("…", () => ShowLayerContextMenu(layer, container, index), 30f));
            RegisterToolkitLayerDrop(row, layer, container, index, depth);
            return row;
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
            handle.tooltip = LayerDragHandleContent.tooltip;
            handle.style.width = 18f;
            handle.style.unityTextAlign = TextAnchor.MiddleCenter;
            handle.style.fontSize = 15f;
            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                selectedLayerId = layer.Id;
                layerDragPointerCandidateId = layer.Id;
                layerDragPointerStart = evt.position;
                layerDragPointerId = evt.pointerId;
                handle.CapturePointer(evt.pointerId);
                evt.StopImmediatePropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (layerDragPointerCandidateId != layer.Id ||
                    layerDragPointerId != evt.pointerId ||
                    Vector2.Distance(layerDragPointerStart, evt.position) < 4f)
                {
                    return;
                }

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
                DragAndDrop.SetGenericData(DraggedLayerIdKey, layer.Id);
                DragAndDrop.SetGenericData(DraggedCompositorIdKey, compositor);
                DragAndDrop.StartDrag(string.IsNullOrEmpty(layer.layerName) ? "Layer" : layer.layerName);
                if (handle.HasPointerCapture(evt.pointerId))
                    handle.ReleasePointer(evt.pointerId);
                layerDragPointerCandidateId = null;
                layerDragPointerId = -1;
                evt.StopImmediatePropagation();
            });
            handle.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (layerDragPointerId != evt.pointerId)
                    return;
                if (handle.HasPointerCapture(evt.pointerId))
                    handle.ReleasePointer(evt.pointerId);
                layerDragPointerCandidateId = null;
                layerDragPointerId = -1;
                RebuildToolkitLayerHierarchy();
                BuildToolkitPreviewHeader();
                evt.StopImmediatePropagation();
            });
            return handle;
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
                ClearToolkitDropIndicator();
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
            if (activeDropElement.userData is string layerId)
            {
                activeDropElement.style.backgroundColor = layerId == selectedLayerId
                    ? SpriteEditorUI.SelectedColor
                    : SpriteEditorUI.RowColor;
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

        private void BuildToolkitPreviewHeader()
        {
            if (toolkitPreviewHeader == null || compositor == null)
                return;

            toolkitPreviewHeader.Clear();
            DrawingLayer layer = GetSelectedLayer() as DrawingLayer;
            if (layer == null)
            {
                VisualElement header = SpriteEditorUI.CreateToolbar();
                Label title = new Label("Preview");
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                header.Add(title);
                Label dimensions = new Label($"{Mathf.Max(1, compositor.width)} × {Mathf.Max(1, compositor.height)}");
                dimensions.style.flexGrow = 1f;
                dimensions.style.unityTextAlign = TextAnchor.MiddleCenter;
                header.Add(dimensions);
                header.Add(SpriteEditorUI.CreateToolbarButton("Refresh", () => RequestPreview(true), 64f));
                toolkitPreviewHeader.Add(header);
                return;
            }

            VisualElement titleRow = SpriteEditorUI.CreateToolbar();
            Label drawingTitle = new Label("Preview • Drawing");
            drawingTitle.style.flexGrow = 1f;
            drawingTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleRow.Add(drawingTitle);
            titleRow.Add(SpriteEditorUI.CreateToolbarButton("Clear", () => ClearDrawingLayer(layer), 46f));
            titleRow.Add(SpriteEditorUI.CreateToolbarButton("Refresh", () => RequestPreview(true), 58f));
            toolkitPreviewHeader.Add(titleRow);

            VisualElement brushRow = SpriteEditorUI.CreateToolbar();
            EnumField tool = CompactField(new EnumField(layer.tool), 72f);
            tool.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Drawing Tool",
                () => layer.tool = (PaintToolMode)evt.newValue,
                rebuildHeader: true));
            brushRow.Add(tool);

            ColorField primary = CompactField(new ColorField(), 54f);
            primary.tooltip = PrimaryBrushColorContent.tooltip;
            primary.SetValueWithoutNotify(layer.brushColor);
            primary.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Foreground Brush Color",
                () => layer.brushColor = evt.newValue));
            brushRow.Add(primary);
            ColorField secondary = CompactField(new ColorField(), 54f);
            secondary.tooltip = SecondaryBrushColorContent.tooltip;
            secondary.SetValueWithoutNotify(layer.secondaryBrushColor);
            secondary.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Background Brush Color",
                () => layer.secondaryBrushColor = evt.newValue));
            brushRow.Add(secondary);

            brushRow.Add(CreateCompactLabel("Size", 30f));
            FloatField size = CompactField(new FloatField(), 46f);
            size.SetValueWithoutNotify(layer.brushSize);
            size.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Brush Size",
                () => layer.brushSize = Mathf.Max(1f, evt.newValue)));
            brushRow.Add(size);
            brushRow.Add(CreateCompactLabel("Hard", 32f));
            Slider hardness = new Slider(0f, 1f) { value = layer.brushHardness };
            hardness.style.flexGrow = 1f;
            hardness.style.minWidth = 42f;
            Label hardnessValue = CreateCompactLabel($"{layer.brushHardness * 100f:0}%", 38f);
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
            mirrorX.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Drawing Symmetry",
                () => layer.mirrorAcrossVerticalAxis = evt.newValue));
            mirrorRow.Add(mirrorX);
            Toggle mirrorY = CompactField(new Toggle("Mirror Y"), 82f);
            mirrorY.tooltip = MirrorHorizontalContent.tooltip;
            mirrorY.SetValueWithoutNotify(layer.mirrorAcrossHorizontalAxis);
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
            repeat.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                "Change Repeat Mode",
                () => layer.repeatMode = (PaintRepeatMode)evt.newValue,
                rebuildHeader: true));
            repeatRow.Add(repeat);
            if (layer.repeatMode == PaintRepeatMode.Grid)
            {
                repeatRow.Add(CreateCompactLabel("X", 14f));
                IntegerField countX = CompactField(new IntegerField(), 38f);
                countX.SetValueWithoutNotify(layer.repeatCount);
                countX.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                    "Change Repeat Count",
                    () => layer.repeatCount = Mathf.Clamp(evt.newValue, 2, 64)));
                repeatRow.Add(countX);
                repeatRow.Add(CreateCompactLabel("Y", 14f));
                IntegerField countY = CompactField(new IntegerField(), 38f);
                countY.SetValueWithoutNotify(layer.repeatSecondaryCount);
                countY.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                    "Change Repeat Count",
                    () => layer.repeatSecondaryCount = Mathf.Clamp(evt.newValue, 2, 64)));
                repeatRow.Add(countY);
            }
            else if (layer.repeatMode != PaintRepeatMode.None)
            {
                repeatRow.Add(CreateCompactLabel("Count", 38f));
                IntegerField count = CompactField(new IntegerField(), 42f);
                count.SetValueWithoutNotify(layer.repeatCount);
                count.RegisterValueChangedCallback(evt => ApplyToolkitChange(
                    "Change Repeat Count",
                    () => layer.repeatCount = Mathf.Clamp(evt.newValue, 2, 64)));
                repeatRow.Add(count);
            }
            VisualElement repeatSpacer = new VisualElement();
            repeatSpacer.style.flexGrow = 1f;
            repeatRow.Add(repeatSpacer);
            EnumField elementMode = CompactField(new EnumField(layer.repeatElementMode), 116f);
            elementMode.SetEnabled(layer.repeatMode != PaintRepeatMode.None);
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
            spacing.RegisterValueChangedCallback(evt =>
            {
                float clampedPercent = Mathf.Clamp(
                    evt.newValue,
                    DrawingLayer.MinimumBrushSpacing * 100f,
                    DrawingLayer.MaximumBrushSpacing * 100f);
                spacing.SetValueWithoutNotify(clampedPercent);
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
            Action change,
            bool rebuildLayers = false,
            bool rebuildHeader = false)
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

            if (rebuildLayers)
                RebuildToolkitLayerHierarchy();
            if (rebuildHeader)
                BuildToolkitPreviewHeader();
            UpdateToolkitPreviewPresentation();
        }

        private void UpdateToolkitPreviewPresentation()
        {
            if (toolkitPreviewCanvas == null || compositor == null)
                return;

            DrawingLayer drawing = GetSelectedLayer() as DrawingLayer;
            toolkitPreviewCanvas.SetDocument(previewTexture, compositor.width, compositor.height, drawing);
            toolkitPreviewErrorRoot?.Clear();
            if (!string.IsNullOrEmpty(previewError) && toolkitPreviewErrorRoot != null)
            {
                SpriteEditorUI.AddHelpBox(
                    toolkitPreviewErrorRoot,
                    previewError,
                    HelpBoxMessageType.Error);
            }

            if (toolkitPreviewFooter != null)
            {
                if (drawing != null)
                {
                    toolkitPreviewFooter.text = previewTexture != null
                        ? $"LMB paint • RMB erase • X colors • [ ] size • {drawing.brushSize:0.#} px"
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
            if (layer == null || (evt.button != 0 && evt.button != 1) || evt.altKey)
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
            lastPaintingUv = startUv;
            hasLastPaintingUv = true;
            toolkitPreviewCanvas.CapturePointer(evt.pointerId);
            Undo.RecordObject(compositor, "Paint Stroke");
            layer.PrepareStroke(compositor.width, compositor.height, "Paint Stroke");
            layer.BeginStroke(startUv);
            layer.PaintPoint(startUv, compositor.width, compositor.height, paintingErase);
            RefreshPreviewDuringPainting();
            UpdatePreviewCursor(evt.localPosition, false);
            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }

        private void OnPreviewPointerMove(PointerMoveEvent evt)
        {
            UpdatePreviewCursor(evt.localPosition, evt.altKey);
            if (paintingLayer == null || paintingPointerId != evt.pointerId)
                return;

            if (TryMapPreviewToLayerUv(
                    evt.localPosition,
                    toolkitPreviewCanvas.ImageRect,
                    paintingLayer,
                    out Vector2 dragUv))
            {
                bool insideRepeatShape = paintingLayer.IsStrokePointInsideRepeatShape(
                    dragUv,
                    compositor.width,
                    compositor.height);
                if (!insideRepeatShape)
                {
                    if (hasLastPaintingUv &&
                        paintingLayer.TryClipStrokeSegmentToRepeatShape(
                            lastPaintingUv,
                            dragUv,
                            compositor.width,
                            compositor.height,
                            out Vector2 clippedUv))
                    {
                        paintingLayer.PaintSegment(
                            lastPaintingUv,
                            clippedUv,
                            compositor.width,
                            compositor.height,
                            false,
                            paintingErase);
                        RefreshPreviewDuringPainting();
                    }
                    hasLastPaintingUv = false;
                }
                else if (hasLastPaintingUv)
                {
                    paintingLayer.PaintSegment(
                        lastPaintingUv,
                        dragUv,
                        compositor.width,
                        compositor.height,
                        false,
                        paintingErase);
                    lastPaintingUv = dragUv;
                    RefreshPreviewDuringPainting();
                }
                else
                {
                    paintingLayer.PaintPoint(
                        dragUv,
                        compositor.width,
                        compositor.height,
                        paintingErase);
                    lastPaintingUv = dragUv;
                    hasLastPaintingUv = true;
                    RefreshPreviewDuringPainting();
                }
            }
            else
            {
                hasLastPaintingUv = false;
            }

            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }

        private void OnPreviewPointerUp(PointerUpEvent evt)
        {
            if (paintingLayer == null ||
                paintingPointerId != evt.pointerId ||
                paintingMouseButton != evt.button)
            {
                return;
            }

            if (toolkitPreviewCanvas.HasPointerCapture(evt.pointerId))
                toolkitPreviewCanvas.ReleasePointer(evt.pointerId);
            paintingPointerId = -1;
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
            bool visible = layer != null &&
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
            if (IsTextInputTarget(evt.target as VisualElement))
                return;

            bool actionModifier = evt.ctrlKey || evt.commandKey;
            bool undo = actionModifier && !evt.altKey && evt.keyCode == KeyCode.Z && !evt.shiftKey;
            bool redo = actionModifier && !evt.altKey &&
                        ((evt.keyCode == KeyCode.Z && evt.shiftKey) ||
                         (evt.keyCode == KeyCode.Y && !evt.shiftKey));
            if (undo || redo)
            {
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
                ApplyToolkitChange("Swap Brush Colors", layer.SwapBrushColors, rebuildHeader: true);
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
            ApplyToolkitChange("Change Brush Size", () => layer.brushSize = nextSize, rebuildHeader: true);
            evt.PreventDefault();
            evt.StopImmediatePropagation();
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

            public void SetDocument(Texture2D nextTexture, int width, int height, DrawingLayer layer)
            {
                texture = nextTexture;
                documentWidth = Mathf.Max(1, width);
                documentHeight = Mathf.Max(1, height);
                drawingLayer = layer;
                image.image = texture;
                UpdateImageLayout();
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
                available.x += 4f;
                available.y += 4f;
                available.width = Mathf.Max(0f, available.width - 8f);
                available.height = Mathf.Max(0f, available.height - 8f);
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
