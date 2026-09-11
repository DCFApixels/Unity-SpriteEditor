using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed class SDFLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(SDFLayer);
        protected override string PreviewTitle => "Preview (SDF)";

        public static void Open(SDFLayer layer, TextureCompositor compositor)
        {
            OpenPropertiesWindow<SDFLayerEditorWindow>(layer, compositor);
        }

        protected override void BuildSettings(VisualElement root, Layer source)
        {
            BuildFields(root, (SDFLayer)source, Compositor, ApplyLayerChange, SettingsBindings, AddEffectTarget);
        }

        internal static void BuildFields(
            VisualElement root, SDFLayer layer, TextureCompositor compositor,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings,
            Action<VisualElement, TargetedLayerEffect> addEffectTarget)
        {
            addEffectTarget(root, layer);

            EnumField metric = SpriteEditorUI.ConfigureField(new EnumField("Distance Algorithm", layer.metric));
            metric.tooltip = "Euclidean Antialiased locates the Threshold contour between pixels, including soft edges. Euclidean Exact keeps a hard threshold; useful for pixel masks.";
            bindings.Track(metric, () => (Enum)layer.metric);
            metric.RegisterValueChangedCallback(evt => applyChange(
                "Change SDF Algorithm",
                () => layer.metric = (DistanceMetric)evt.newValue));
            root.Add(metric);

            EnumField sourceChannel = SpriteEditorUI.ConfigureField(
                new EnumField("Source Channel", layer.sourceChannel));
            bindings.Track(sourceChannel, () => (Enum)layer.sourceChannel);
            sourceChannel.RegisterValueChangedCallback(evt => applyChange(
                "Change SDF Source Channel",
                () => layer.sourceChannel = (SDFLayer.SourceChannel)evt.newValue));
            root.Add(sourceChannel);

            SliderInt threshold = new SliderInt("Threshold", 0, 255);
            SpriteEditorUI.ConfigureField(threshold);
            threshold.showInputField = true;
            threshold.SetValueWithoutNotify(layer.threshold);
            bindings.Track(threshold, () => (int)layer.threshold);
            threshold.RegisterValueChangedCallback(evt => applyChange(
                "Change SDF Threshold",
                () => layer.threshold = (byte)Mathf.Clamp(evt.newValue, 0, 255)));
            root.Add(threshold);

            FloatField maxDistance = SpriteEditorUI.ConfigureField(
                new FloatField("Max Distance (px, 0 = auto)"));
            maxDistance.SetValueWithoutNotify(layer.maxDistanceNormalization);
            bindings.Track(maxDistance, () => layer.maxDistanceNormalization);
            maxDistance.RegisterValueChangedCallback(evt => applyChange(
                "Change SDF Max Distance",
                () => layer.maxDistanceNormalization = Mathf.Max(0f, evt.newValue)));
            root.Add(maxDistance);

            EnumField distancePosition = SpriteEditorUI.ConfigureField(
                new EnumField("Position", layer.distancePosition));
            bindings.Track(distancePosition, () => (Enum)layer.distancePosition);
            distancePosition.RegisterValueChangedCallback(evt => applyChange(
                "Change SDF Position",
                () => layer.distancePosition = (SDFLayer.DistancePosition)evt.newValue));
            root.Add(distancePosition);

            Toggle inverted = SpriteEditorUI.ConfigureField(new Toggle("Inverted"));
            inverted.SetValueWithoutNotify(layer.inverted);
            bindings.Track(inverted, () => layer.inverted);
            inverted.RegisterValueChangedCallback(evt =>
                applyChange("Invert SDF", () => layer.inverted = evt.newValue));
            root.Add(inverted);

            GradientField gradient = SpriteEditorUI.ConfigureField(SpriteEditorColorInputs.Bind(new GradientField("Gradient"), bindings, () => layer.gradient));
            gradient.RegisterValueChangedCallback(evt =>
                applyChange("Change SDF Gradient", () => layer.gradient = GradientUtility.Create(evt.newValue)));
            root.Add(gradient);
        }
    }
}
