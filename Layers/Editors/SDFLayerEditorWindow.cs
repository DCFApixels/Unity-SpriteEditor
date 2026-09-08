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
            SDFLayerEditorWindow window = GetWindow<SDFLayerEditorWindow>(true, "SDF Settings");
            window.Initialize(layer, compositor);
            window.Show();
        }

        protected override void BuildSettings(VisualElement root, Layer source)
        {
            SDFLayer layer = (SDFLayer)source;
            AddEffectTarget(root, layer);
            SpriteEditorUI.AddTextureTransform(
                root,
                () => layer.transform,
                value => layer.transform = value,
                ApplyLayerChange,
                SettingsBindings);

            EnumField metric = SpriteEditorUI.ConfigureField(new EnumField("Distance Algorithm", layer.metric));
            SettingsBindings.Track(metric, () => (Enum)layer.metric);
            metric.RegisterValueChangedCallback(evt => ApplyLayerChange(
                "Change SDF Algorithm",
                () => layer.metric = (DistanceMetric)evt.newValue));
            root.Add(metric);

            EnumField sourceChannel = SpriteEditorUI.ConfigureField(
                new EnumField("Source Channel", layer.sourceChannel));
            SettingsBindings.Track(sourceChannel, () => (Enum)layer.sourceChannel);
            sourceChannel.RegisterValueChangedCallback(evt => ApplyLayerChange(
                "Change SDF Source Channel",
                () => layer.sourceChannel = (SDFLayer.SourceChannel)evt.newValue));
            root.Add(sourceChannel);

            SliderInt threshold = new SliderInt("Threshold", 0, 255);
            SpriteEditorUI.ConfigureField(threshold);
            threshold.showInputField = true;
            threshold.SetValueWithoutNotify(layer.threshold);
            SettingsBindings.Track(threshold, () => (int)layer.threshold);
            threshold.RegisterValueChangedCallback(evt => ApplyLayerChange(
                "Change SDF Threshold",
                () => layer.threshold = (byte)Mathf.Clamp(evt.newValue, 0, 255)));
            root.Add(threshold);

            FloatField maxDistance = SpriteEditorUI.ConfigureField(
                new FloatField("Max Distance (px, 0 = auto)"));
            maxDistance.SetValueWithoutNotify(layer.maxDistanceNormalization);
            SettingsBindings.Track(maxDistance, () => layer.maxDistanceNormalization);
            maxDistance.RegisterValueChangedCallback(evt => ApplyLayerChange(
                "Change SDF Max Distance",
                () => layer.maxDistanceNormalization = Mathf.Max(0f, evt.newValue)));
            root.Add(maxDistance);

            EnumField distancePosition = SpriteEditorUI.ConfigureField(
                new EnumField("Position", layer.distancePosition));
            SettingsBindings.Track(distancePosition, () => (Enum)layer.distancePosition);
            distancePosition.RegisterValueChangedCallback(evt => ApplyLayerChange(
                "Change SDF Position",
                () => layer.distancePosition = (SDFLayer.DistancePosition)evt.newValue));
            root.Add(distancePosition);

            Toggle inverted = SpriteEditorUI.ConfigureField(new Toggle("Inverted"));
            inverted.SetValueWithoutNotify(layer.inverted);
            SettingsBindings.Track(inverted, () => layer.inverted);
            inverted.RegisterValueChangedCallback(evt =>
                ApplyLayerChange("Invert SDF", () => layer.inverted = evt.newValue));
            root.Add(inverted);

            GradientField gradient = SpriteEditorUI.ConfigureField(new GradientField("Gradient"));
            gradient.SetValueWithoutNotify(layer.gradient);
            SettingsBindings.Track(gradient, () => layer.gradient);
            gradient.RegisterValueChangedCallback(evt =>
                ApplyLayerChange("Change SDF Gradient", () => layer.gradient = GradientUtility.Create(evt.newValue)));
            root.Add(gradient);
        }
    }
}
