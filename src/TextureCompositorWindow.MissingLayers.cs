using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        private sealed class MissingLayerSlot
        {
            internal TextureCompositor Document;
            internal List<Layer> Container;
            internal Layer[] Snapshot;
            internal int Index;
            internal string PropertyPath;

            internal bool IsValid(TextureCompositor document)
            {
                if (document == null || document != Document || Container.Count != Snapshot.Length ||
                    Index < 0 || Index >= Container.Count || Container[Index] != null) return false;
                for (int i = 0; i < Snapshot.Length; i++)
                    if (!ReferenceEquals(Container[i], Snapshot[i])) return false;
                return FindContainerPath(document.layers, Container, "layers") == PropertyPath;
            }
        }

        [NonSerialized] private MissingLayerSlot selectedMissingLayer;
        [NonSerialized] private MissingLayerSlot toolkitInspectorMissingLayer;
        private readonly Dictionary<(List<Layer> container, int index), string> missingLayerNames =
            new Dictionary<(List<Layer> container, int index), string>();

        private string MissingLayerDisplayName(List<Layer> container, int index) =>
            missingLayerNames.TryGetValue((container, index), out string name) && !string.IsNullOrEmpty(name)
                ? name : "Missing Reference";

        private void RefreshMissingLayerNames()
        {
            missingLayerNames.Clear();
            if (compositor == null || !toolkitLayerTree.Exists(entry => entry.Layer == null && entry.Index >= 0)) return;
            var records = MissingLayerRecovery.ReadRecords(compositor);
            using (var serialized = new SerializedObject(compositor))
                foreach (var entry in toolkitLayerTree)
                {
                    if (entry.Layer != null || entry.Index < 0) continue;
                    string path = FindContainerPath(compositor.layers, entry.Container, "layers");
                    var property = path == null ? null : serialized.FindProperty(path)?.GetArrayElementAtIndex(entry.Index);
                    long referenceId = property?.managedReferenceId ?? -2;
                    var record = referenceId < 0 ? null : records.Find(item => item.ReferenceId == referenceId);
                    if (!string.IsNullOrEmpty(record?.LayerName)) missingLayerNames[(entry.Container, entry.Index)] = record.LayerName;
                }
        }

        private bool HasSelectedMissingLayer => selectedMissingLayer != null && selectedMissingLayer.IsValid(compositor);

        private static string FindContainerPath(List<Layer> layers, List<Layer> target, string path)
        {
            if (ReferenceEquals(layers, target)) return path;
            if (layers == null) return null;
            for (int i = 0; i < layers.Count; i++)
                if (layers[i] is GroupLayer group)
                {
                    string found = FindContainerPath(group.layers, target, path + ".Array.data[" + i + "].layers");
                    if (found != null) return found;
                }
            return null;
        }

        private void SelectMissingLayer(List<Layer> container, int index)
        {
            if (compositor == null || index < 0 || index >= container.Count || container[index] != null) return;
            string path = FindContainerPath(compositor.layers, container, "layers");
            if (path == null) return;
            FinishPreviewTransform();
            FinishPaintingStroke();
            SelectOnlyLayer(null);
            selectedMissingLayer = new MissingLayerSlot
            {
                Document = compositor, Container = container, Index = index,
                Snapshot = container.ToArray(), PropertyPath = path
            };
            rootVisualElement.Focus();
            RefreshToolkitInterface();
        }

        private void RemoveSelectedMissingLayer()
        {
            if (!HasSelectedMissingLayer) return;
            MissingLayerSlot slot = selectedMissingLayer;
            ExecuteContextChange("Remove Missing Layer", () =>
            {
                slot.Container.RemoveAt(slot.Index);
                SelectOnlyLayer(null);
            });
        }

        private VisualElement CreateMissingLayerRow(List<Layer> container, int index, int depth)
        {
            var row = new VisualElement();
            var outline = new VisualElement { pickingMode = PickingMode.Ignore };
            outline.AddToClassList("sprite-editor-layer-active-outline");
            row.Add(outline);
            void RefreshSelection()
            {
                ApplyLayerSelectionStyle(row, null);
                bool selected = selectedMissingLayer != null && ReferenceEquals(selectedMissingLayer.Container, container) && selectedMissingLayer.Index == index;
                row.EnableInClassList("sprite-editor-layer-row--selected", selected);
                row.EnableInClassList("sprite-editor-layer-row--active", selected);
            }
            RefreshSelection();
            toolkitLayerBindings.Add(RefreshSelection);

            var visibility = new Button { tooltip = "This layer cannot render until its missing type is replaced." };
            visibility.AddToClassList("sprite-editor-layer-enabled");
            visibility.AddToClassList("sprite-editor-layer-enabled--hidden");
            var eye = new LayerActionIcon(LayerActionIcon.Kind.EyeOff);
            eye.AddToClassList("sprite-editor-layer-eye-off");
            visibility.Add(eye);
            visibility.SetEnabled(false);
            row.Add(visibility);
            var cell = CreateLayerNameCell(row, depth, null);
            var warning = new Label("!") { pickingMode = PickingMode.Ignore };
            warning.AddToClassList("sprite-editor-layer-thumbnail");
            warning.AddToClassList("sprite-editor-missing-thumbnail");
            cell.Add(warning);
            var name = new TextField { value = MissingLayerDisplayName(container, index), isReadOnly = true, focusable = false, pickingMode = PickingMode.Ignore };
            name.AddToClassList("sprite-editor-layer-name");
            name.Query<VisualElement>().ForEach(element => { element.pickingMode = PickingMode.Ignore; element.focusable = false; });
            cell.Add(name);
            toolkitLayerBindings.Add(() =>
            {
                string displayName = MissingLayerDisplayName(container, index);
                if (name.value != displayName) name.SetValueWithoutNotify(displayName);
            });
            var opacity = new FloatField { showMixedValue = true };
            opacity.AddToClassList("sprite-editor-layer-opacity");
            opacity.SetEnabled(false);
            row.Add(opacity);
            var blend = new EnumField(BlendMode.Normal) { showMixedValue = true };
            blend.AddToClassList("sprite-editor-layer-blend");
            blend.SetEnabled(false);
            row.Add(blend);
            row.tooltip = "The layer type is unavailable. Select this row to replace it and recover compatible settings.";
            void ShowMenu()
            {
                SelectMissingLayer(container, index);
                MissingLayerSlot menuSlot = selectedMissingLayer;
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Remove"), false, () =>
                {
                    if (ReferenceEquals(menuSlot, selectedMissingLayer)) RemoveSelectedMissingLayer();
                });
                menu.ShowAsContext();
            }
            row.Add(CreateLayerMenuButton(ShowMenu));
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 && evt.button != 1) return;
                SelectMissingLayer(container, index);
                // Keep the menu button's own Clickable; other controls are only placeholders.
                if (!(evt.target is VisualElement target) || !target.ClassListContains("sprite-editor-layer-menu-button"))
                    evt.StopPropagation();
            }, TrickleDown.TrickleDown);
            row.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button == 1) { ShowMenu(); evt.StopPropagation(); }
            });
            return row;
        }

        private static readonly Type[] MissingLayerReplacements =
        {
            typeof(DrawingLayer), typeof(FileLayer), typeof(ColorFillLayer), typeof(GradientLayer),
            typeof(NoiseLayer), typeof(OutlineLayer), typeof(SDFLayer), typeof(NormalMapLayer),
            typeof(BlurLayer), typeof(MakeSeamlessLayer), typeof(ShaderProcessorLayer), typeof(GroupLayer)
        };

        private void BuildMissingLayerInspector(VisualElement root)
        {
            MissingLayerSlot slot = selectedMissingLayer;
            root.Add(new HelpBox("Unity cannot load this layer's type. Its script may have been removed or renamed. " +
                "Choose a replacement to keep this position in the stack and transfer saved settings with matching field paths. " +
                "Unsupported values use the new layer's defaults; check the result after replacement.", HelpBoxMessageType.Warning));
            var content = new VisualElement();
            content.AddToClassList("sprite-editor-inspector-section-content");
            root.Add(content);

            var records = MissingLayerRecovery.ReadRecords(compositor);
            long referenceId = -2;
            using (var serialized = new SerializedObject(compositor))
            {
                var property = serialized.FindProperty(slot.PropertyPath)?.GetArrayElementAtIndex(slot.Index);
                if (property != null) referenceId = property.managedReferenceId;
            }
            int exact = records.FindIndex(record => record.ReferenceId == referenceId && referenceId >= 0);
            var sources = new List<string> { "Choose saved layer data…", "Use new layer defaults" };
            foreach (var record in records) sources.Add(record.Label);
            var source = new DropdownField("Saved data", sources, exact >= 0 ? exact + 2 : 0);
            content.Add(source);
            var names = new List<string>();
            foreach (Type type in MissingLayerReplacements)
                names.Add(TextureCompositor.LayerMenuName((Layer)Activator.CreateInstance(type)));
            var replacement = new DropdownField("Replace with", names, 0);
            content.Add(replacement);
            var feedback = new HelpBox("", HelpBoxMessageType.Info);
            content.Add(feedback);
            var apply = new Button { text = "Replace Layer" };
            content.Add(apply);
            Layer draft = null;
            MissingLayerRecovery.Report report = null;
            void Prepare()
            {
                draft = null;
                apply.SetEnabled(false);
                feedback.messageType = HelpBoxMessageType.Info;
                if (source.index == 0)
                {
                    feedback.text = records.Count == 0
                        ? "Unity has no readable saved layer data. You can replace this entry using new layer defaults."
                        : "Unity no longer identifies which saved record belongs to this row. Choose it by its name; records are never matched by order.";
                    return;
                }
                try
                {
                    var candidate = (Layer)Activator.CreateInstance(MissingLayerReplacements[replacement.index]);
                    var record = source.index >= 2 ? records[source.index - 2] : null;
                    report = MissingLayerRecovery.Copy(record, candidate, compositor);
                    if (!string.IsNullOrEmpty(record?.LayerName))
                    {
                        missingLayerNames[(slot.Container, slot.Index)] = record.LayerName;
                        toolkitLayerBindings.Refresh(true);
                        if (toolkitLayerSettingsTitle != null) toolkitLayerSettingsTitle.text = record.LayerName;
                    }
                    feedback.text = record == null ? "No saved settings will be transferred." : report.Summary +
                        " Options with matching fields are copied by value, so check settings such as Mode.";
                    feedback.messageType = record == null || report.Skipped.Count > 0 ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info;
                    draft = candidate;
                    apply.SetEnabled(true);
                }
                catch (Exception e)
                {
                    feedback.text = e.Message;
                    feedback.messageType = HelpBoxMessageType.Warning;
                }
            }
            source.RegisterValueChangedCallback(_ => Prepare());
            replacement.RegisterValueChangedCallback(_ => Prepare());
            apply.clicked += () =>
            {
                if (!ReferenceEquals(slot, selectedMissingLayer) || !slot.IsValid(compositor))
                { feedback.text = "The layer stack changed. Select the missing row again."; apply.SetEnabled(false); return; }
                Prepare();
                if (draft == null) return;
                Layer layer = draft;
                bool useDefaultName = source.index == 1 || string.IsNullOrEmpty(layer.layerName);
                ExecuteContextChange("Replace Missing Layer", () =>
                {
                    if (useDefaultName) layer.layerName = compositor.AllocateLayerName(layer);
                    if (layer is DrawingLayer drawing && drawing.StoredTexture == null)
                    {
                        try
                        {
                            drawing.InitializeCanvas(compositor.width, compositor.height);
                            drawing.InvalidatePaintSurface();
                            drawing.MakeTexturePersistent(compositor);
                            Undo.RegisterCreatedObjectUndo(drawing.StoredTexture, "Replace Missing Layer");
                        }
                        catch
                        {
                            if (drawing.StoredTexture != null) DestroyImmediate(drawing.StoredTexture, true);
                            drawing.InvalidatePaintSurface();
                            throw;
                        }
                    }
                    slot.Container[slot.Index] = layer;
                    compositor.NormalizeModel();
                    if (layer is ShaderProcessorLayer && layer.modifiers.Count == 0) compositor.AddEmbeddedShaderFX(layer);
                    SelectOnlyLayer(layer.Id);
                });
                if (compositor.FindLayer(layer.Id) == layer)
                    ShowNotification(new GUIContent("Layer replaced — " + report.Copied + " values transferred."));
            };
            Prepare();
        }
    }
}
