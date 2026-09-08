using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        [NonSerialized] private ScrollView toolkitLayerSettingsScroll;
        [NonSerialized] private Layer toolkitInspectorLayer;
        [NonSerialized] private TextureCompositor toolkitInspectorDocument;
        [NonSerialized] private bool toolkitInspectorBuilt;
        [NonSerialized] private EffectTargetSettingsView toolkitInspectorEffectTarget;
        private readonly SpriteEditorUI.ValueBindings toolkitInspectorBindings = new SpriteEditorUI.ValueBindings();

        private void ResetToolkitLayerInspector()
        {
            toolkitInspectorBuilt = false;
            toolkitInspectorLayer = null;
            toolkitInspectorDocument = null;
            toolkitInspectorEffectTarget = null;
            toolkitInspectorBindings.Clear();
        }

        private void RefreshToolkitLayerInspector(bool forceValues)
        {
            if (toolkitLayerSettingsScroll == null)
                return;

            Layer selected = GetSelectedLayer();
            // Rebind only when selection/document identity changes (including Undo replacement).
            // Normal value changes must preserve text editing, pointer capture and scroll position.
            if (!toolkitInspectorBuilt || !ReferenceEquals(toolkitInspectorLayer, selected) ||
                toolkitInspectorDocument != compositor)
            {
                ResetToolkitLayerInspector();
                toolkitInspectorBuilt = true;
                toolkitInspectorLayer = selected;
                toolkitInspectorDocument = compositor;
                toolkitLayerSettingsScroll.Clear();
                toolkitLayerSettingsScroll.scrollOffset = Vector2.zero;
                BuildToolkitLayerInspector(toolkitLayerSettingsScroll, selected);
            }
            toolkitInspectorEffectTarget?.Invalidate();
            toolkitInspectorBindings.Refresh(forceValues);
        }

        private void BuildToolkitLayerInspector(VisualElement root, Layer layer)
        {
            if (layer == null)
            {
                SpriteEditorUI.AddHelpBox(root, "Select a layer below to edit its settings.", HelpBoxMessageType.Info);
                return;
            }

            Label title = new Label(layer.layerName)
            {
                name = "selectedLayerTitle",
                enableRichText = false,
                pickingMode = PickingMode.Ignore
            };
            title.AddToClassList("sprite-editor-layer-title");
            root.Add(title);
            toolkitInspectorBindings.Add(() =>
            {
                if (title.text != layer.layerName)
                    title.text = layer.layerName;
            });

            if (layer is GroupLayer)
            {
                SpriteEditorUI.AddHelpBox(root,
                    "Groups are pass-through: their children blend directly into the layer stack. " +
                    "Transform and modifiers are edited on individual layers.",
                    HelpBoxMessageType.Info);
                return;
            }

            switch (layer)
            {
                case DrawingLayer drawing:
                    DrawingLayerEditorWindow.BuildFields(root, drawing, compositor, ApplyToolkitChange, toolkitInspectorBindings);
                    break;
                case FileLayer file:
                    FileLayerEditorWindow.BuildFields(root, file, compositor, ApplyToolkitChange, toolkitInspectorBindings);
                    break;
                case ColorFillLayer fill:
                    ColorFillLayerEditorWindow.BuildFields(root, fill, compositor, ApplyToolkitChange, toolkitInspectorBindings);
                    break;
                case GradientLayer gradient:
                    GradientLayerEditorWindow.BuildFields(root, gradient, compositor, ApplyToolkitChange, toolkitInspectorBindings);
                    break;
                case OutlineLayer outline:
                    OutlineLayerEditorWindow.BuildFields(root, outline, compositor, ApplyToolkitChange, toolkitInspectorBindings,
                        AddToolkitInspectorEffectTarget);
                    break;
                case SDFLayer sdf:
                    SDFLayerEditorWindow.BuildFields(root, sdf, compositor, ApplyToolkitChange, toolkitInspectorBindings,
                        AddToolkitInspectorEffectTarget);
                    break;
            }
        }

        private void AddToolkitInspectorEffectTarget(VisualElement root, TargetedLayerEffect effect)
        {
            toolkitInspectorEffectTarget = new EffectTargetSettingsView(
                compositor, ApplyToolkitChange, toolkitInspectorBindings);
            toolkitInspectorEffectTarget.Build(root, effect);
        }
    }
}
