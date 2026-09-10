using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        [SerializeField] private bool colorSettingsExpanded;
        [SerializeField] private bool layerPropertiesExpanded = true;
        [SerializeField] private bool layerFxExpanded;
        [NonSerialized] private ScrollView toolkitLayerSettingsScroll;
        [NonSerialized] private Label toolkitLayerSettingsTitle;
        [NonSerialized] private Layer toolkitInspectorLayer;
        [NonSerialized] private TextureCompositor toolkitInspectorDocument;
        [NonSerialized] private bool toolkitInspectorBuilt;
        [NonSerialized] private EffectTargetSettingsView toolkitInspectorEffectTarget;
        [NonSerialized] private LayerShaderFXView toolkitInspectorShaderFX;
        private readonly SpriteEditorUI.ValueBindings toolkitInspectorBindings = new SpriteEditorUI.ValueBindings();

        private void ResetToolkitLayerInspector()
        {
            toolkitInspectorBuilt = false;
            toolkitInspectorLayer = null;
            toolkitInspectorDocument = null;
            toolkitInspectorEffectTarget = null;
            toolkitInspectorShaderFX = null;
            toolkitInspectorBindings.Clear();
        }

        private void RefreshToolkitLayerInspector(bool forceValues)
        {
            if (toolkitLayerSettingsScroll == null)
                return;

            Layer selected = GetSelectedLayer();
            string title = selected == null ? "Layer Settings" : selected.layerName;
            if (toolkitLayerSettingsTitle != null && toolkitLayerSettingsTitle.text != title)
                toolkitLayerSettingsTitle.text = title;
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
            toolkitInspectorShaderFX?.Refresh();
            toolkitInspectorBindings.Refresh(forceValues);
        }

        private void BuildToolkitLayerInspector(VisualElement root, Layer layer)
        {
            if (layer == null)
            {
                SpriteEditorUI.AddHelpBox(root, "Select a layer below to edit its settings.", HelpBoxMessageType.Info);
                return;
            }

            toolkitInspectorShaderFX = SpriteEditorUI.BuildLayerInspectorSections(root, layer, compositor,
                ApplyToolkitChange, toolkitInspectorBindings, properties => BuildToolkitLayerProperties(properties, layer),
                colorSettingsExpanded, value => colorSettingsExpanded = value,
                layerPropertiesExpanded, value => layerPropertiesExpanded = value,
                layerFxExpanded, value => layerFxExpanded = value);
        }

        private void BuildToolkitLayerProperties(VisualElement root, Layer layer)
        {
            switch (layer)
            {
                case ShaderProcessorLayer:
                    SpriteEditorUI.AddHelpBox(root, "Processes the composited layers below. Normal blends between the original and processed image using Opacity. In a Pass Through group, the external backdrop is included. Add or edit Shader FX below.", HelpBoxMessageType.Info);
                    break;
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
                case NoiseLayer noise:
                    NoiseLayerEditorWindow.BuildFields(root, noise, compositor, ApplyToolkitChange, toolkitInspectorBindings);
                    break;
                case OutlineLayer outline:
                    OutlineLayerEditorWindow.BuildFields(root, outline, compositor, ApplyToolkitChange, toolkitInspectorBindings,
                        AddToolkitInspectorEffectTarget);
                    break;
                case SDFLayer sdf:
                    SDFLayerEditorWindow.BuildFields(root, sdf, compositor, ApplyToolkitChange, toolkitInspectorBindings,
                        AddToolkitInspectorEffectTarget);
                    break;
                case NormalMapLayer normalMap:
                    NormalMapLayerEditorWindow.BuildFields(root, normalMap, compositor, ApplyToolkitChange, toolkitInspectorBindings,
                        AddToolkitInspectorEffectTarget);
                    break;
                case GaussianBlurLayer gaussian:
                    GaussianBlurLayerEditorWindow.BuildFields(root, gaussian, compositor, ApplyToolkitChange, toolkitInspectorBindings,
                        AddToolkitInspectorEffectTarget);
                    break;
                case MotionBlurLayer motion:
                    MotionBlurLayerEditorWindow.BuildFields(root, motion, compositor, ApplyToolkitChange, toolkitInspectorBindings,
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
