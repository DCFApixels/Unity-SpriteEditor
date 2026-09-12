using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed class GaussianBlurLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(GaussianBlurLayer);
        protected override string PreviewTitle => "Preview (Gaussian Blur)";
        public static void Open(GaussianBlurLayer layer, TextureCompositor compositor) =>
            OpenPropertiesWindow<GaussianBlurLayerEditorWindow>(layer, compositor);
        protected override void BuildSettings(VisualElement root, Layer source) =>
            BuildFields(root, (GaussianBlurLayer)source, Compositor, ApplyLayerChange, SettingsBindings, AddEffectTarget);

        internal static void BuildFields(VisualElement root, GaussianBlurLayer layer, TextureCompositor compositor,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings,
            Action<VisualElement, TargetedLayerEffect> addEffectTarget)
        {
            addEffectTarget(root, layer);
            var strength = SpriteEditorUI.ConfigureField(new Slider("Strength (%)", 0f, GaussianBlurLayer.MaximumStrength * 100f)
                { showInputField = true, tooltip = "0: original; 100: normal blur; above 100: denser translucent blur without changing radius or color brightness. Fully opaque areas are unchanged above 100." });
            strength.SetValueWithoutNotify(layer.strength * 100f);
            bindings.Track(strength, () => layer.strength * 100f);
            strength.RegisterValueChangedCallback(evt => applyChange("Change Gaussian Strength", () =>
                layer.strength = float.IsNaN(evt.newValue) || float.IsInfinity(evt.newValue) ? layer.strength :
                    Mathf.Clamp(evt.newValue / 100f, 0f, GaussianBlurLayer.MaximumStrength)));
            root.Add(strength);
            var radius = SpriteEditorUI.ConfigureField(new Slider("Radius (px)", 0f, GaussianBlurLayer.MaximumRadius)
                { showInputField = true, tooltip = "Kernel extent in original canvas pixels (three standard deviations). Zero leaves the source unchanged." });
            radius.SetValueWithoutNotify(layer.radius);
            bindings.Track(radius, () => layer.radius);
            radius.RegisterValueChangedCallback(evt => applyChange("Change Gaussian Radius",
                () => layer.radius = Mathf.Clamp(evt.newValue, 0f, GaussianBlurLayer.MaximumRadius)));
            root.Add(radius);
            var edges = SpriteEditorUI.ConfigureField(new EnumField("Edges", layer.edges)
                { tooltip = "Sampling outside the canvas. Repeat supports seamless textures; independent of tiled preview and Transform tiling." });
            bindings.Track(edges, () => (Enum)layer.edges);
            edges.RegisterValueChangedCallback(evt => applyChange("Change Gaussian Edges",
                () => layer.edges = (GaussianBlurLayer.EdgeMode)evt.newValue));
            root.Add(edges);
        }
    }
}
