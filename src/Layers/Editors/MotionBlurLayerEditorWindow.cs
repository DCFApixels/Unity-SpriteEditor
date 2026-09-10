using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed class MotionBlurLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(MotionBlurLayer);
        protected override string PreviewTitle => "Preview (Motion Blur)";
        public static void Open(MotionBlurLayer layer, TextureCompositor compositor) =>
            OpenPropertiesWindow<MotionBlurLayerEditorWindow>(layer, compositor);
        protected override void BuildSettings(VisualElement root, Layer source) =>
            BuildFields(root, (MotionBlurLayer)source, Compositor, ApplyLayerChange, SettingsBindings, AddEffectTarget);

        internal static void BuildFields(VisualElement root, MotionBlurLayer layer, TextureCompositor compositor,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings,
            Action<VisualElement, TargetedLayerEffect> addEffectTarget)
        {
            addEffectTarget(root, layer);
            var mode = SpriteEditorUI.ConfigureField(new EnumField("Mode", layer.mode));
            bindings.Track(mode, () => (Enum)layer.mode);
            mode.RegisterValueChangedCallback(evt => applyChange("Change Motion Blur Mode",
                () => layer.mode = (MotionBlurLayer.BlurMode)evt.newValue));
            root.Add(mode);

            void Slider(VisualElement parent, string label, string tooltip, Func<float> get, Action<float> set, float min, float max)
            {
                var field = SpriteEditorUI.ConfigureField(new Slider(label, min, max) { showInputField = true, tooltip = tooltip });
                bindings.Track(field, get);
                field.RegisterValueChangedCallback(evt => applyChange("Change Motion Blur " + label,
                    () => set(MotionBlurLayer.Limit(evt.newValue, min, max, get()))));
                parent.Add(field);
            }

            Slider(root, "Strength (%)", "0: original; 100: normal blur; above 100: denser translucent trails without changing length or color brightness. Fully opaque areas are unchanged above 100.",
                () => layer.strength * 100f, value => layer.strength = value / 100f, 0f, MotionBlurLayer.MaximumStrength * 100f);

            var linear = new VisualElement();
            Slider(linear, "Distance (px)", "Total length in original canvas pixels. Zero leaves the source unchanged.",
                () => layer.distance, value => layer.distance = value, 0f, MotionBlurLayer.MaximumDistance);
            Slider(linear, "Angle (deg)", "Zero points right; positive angles turn counterclockwise.",
                () => layer.angle, value => layer.angle = value, -180f, 180f);
            root.Add(linear);

            var circular = new VisualElement();
            Slider(circular, "Arc (deg)", "Total rotation swept by the blur. Zero leaves the source unchanged.",
                () => layer.arc, value => layer.arc = value, 0f, 360f);
            var center = SpriteEditorUI.ConfigureField(new Vector2Field("Center"));
            center.tooltip = "Normalized canvas position: (0, 0) bottom-left, (1, 1) top-right. Independent of the output Transform pivot.";
            bindings.Track(center, () => layer.center);
            center.RegisterValueChangedCallback(evt => applyChange("Change Motion Blur Center", () => layer.center = new Vector2(
                MotionBlurLayer.Limit(evt.newValue.x, 0f, 1f, layer.center.x),
                MotionBlurLayer.Limit(evt.newValue.y, 0f, 1f, layer.center.y))));
            circular.Add(center);
            root.Add(circular);

            var direction = SpriteEditorUI.ConfigureField(new EnumField("Direction", layer.direction));
            direction.tooltip = "Centered spreads both ways. Forward follows Angle in Linear mode and turns counterclockwise in Circular mode; Backward reverses it.";
            bindings.Track(direction, () => (Enum)layer.direction);
            direction.RegisterValueChangedCallback(evt => applyChange("Change Motion Blur Direction",
                () => layer.direction = (MotionBlurLayer.MotionDirection)evt.newValue));
            root.Add(direction);
            var edges = SpriteEditorUI.ConfigureField(new EnumField("Edges", layer.edges));
            edges.tooltip = "Sampling outside the source canvas, independent of tiled preview and Transform tiling.";
            bindings.Track(edges, () => (Enum)layer.edges);
            edges.RegisterValueChangedCallback(evt => applyChange("Change Motion Blur Edges",
                () => layer.edges = (MotionBlurLayer.EdgeMode)evt.newValue));
            root.Add(edges);

            bindings.Add(() =>
            {
                linear.EnableInClassList("sprite-editor-hidden", layer.mode != MotionBlurLayer.BlurMode.Linear);
                circular.EnableInClassList("sprite-editor-hidden", layer.mode != MotionBlurLayer.BlurMode.Circular);
            });
        }
    }
}
