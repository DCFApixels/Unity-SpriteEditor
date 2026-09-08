using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed class GradientLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(GradientLayer);

        public static void Open(GradientLayer layer, TextureCompositor compositor)
        {
            GradientLayerEditorWindow window = GetWindow<GradientLayerEditorWindow>(true, "Gradient Settings");
            window.Initialize(layer, compositor);
            window.Show();
        }

        protected override void BuildSettings(VisualElement root, Layer source)
        {
            GradientLayer layer = (GradientLayer)source;
            SpriteEditorUI.AddTextureTransform(
                root,
                () => layer.transform,
                value => layer.transform = value,
                ApplyLayerChange,
                SettingsBindings);

            EnumField gradientType = SpriteEditorUI.ConfigureField(
                new EnumField("Gradient Type", layer.gradientType));
            SettingsBindings.Track(gradientType, () => (Enum)layer.gradientType);
            gradientType.RegisterValueChangedCallback(evt =>
            {
                ApplyLayerChange(
                    "Change Gradient Type",
                    () => layer.gradientType = (GradientLayer.GradientType)evt.newValue);
            });
            root.Add(gradientType);

            GradientField gradient = SpriteEditorUI.ConfigureField(new GradientField("Gradient"));
            gradient.SetValueWithoutNotify(layer.gradient);
            SettingsBindings.Track(gradient, () => layer.gradient);
            gradient.RegisterValueChangedCallback(evt =>
                ApplyLayerChange("Change Gradient", () => layer.gradient = GradientUtility.Create(evt.newValue)));
            root.Add(gradient);

            Vector2Field center = SpriteEditorUI.ConfigureField(new Vector2Field("Center"));
            SettingsBindings.Track(center, () => layer.center);
            center.RegisterValueChangedCallback(evt =>
                ApplyLayerChange("Change Gradient Center", () => layer.center = evt.newValue));
            root.Add(center);

            FloatField radius = SpriteEditorUI.ConfigureField(new FloatField("Radius"));
            SettingsBindings.Track(radius, () => layer.radius);
            radius.RegisterValueChangedCallback(evt =>
                ApplyLayerChange("Change Gradient Radius", () => layer.radius = Mathf.Max(0f, evt.newValue)));
            root.Add(radius);

            VisualElement circularSettings = new VisualElement();
            FloatField repetitions = SpriteEditorUI.ConfigureField(new FloatField("Repetitions"));
            SettingsBindings.Track(repetitions, () => layer.circularRepetitions);
            repetitions.RegisterValueChangedCallback(evt => ApplyLayerChange(
                "Change Gradient Repetitions",
                () => layer.circularRepetitions = Mathf.Max(float.Epsilon, evt.newValue)));
            circularSettings.Add(repetitions);

            EnumField wrapMode = SpriteEditorUI.ConfigureField(
                new EnumField("Wrap Mode", layer.circularWrapMode));
            SettingsBindings.Track(wrapMode, () => (Enum)layer.circularWrapMode);
            wrapMode.RegisterValueChangedCallback(evt => ApplyLayerChange(
                "Change Gradient Wrap Mode",
                () => layer.circularWrapMode = (GradientLayer.WrapMode)evt.newValue));
            circularSettings.Add(wrapMode);
            root.Add(circularSettings);
            SettingsBindings.Add(() =>
            {
                bool radial = layer.gradientType == GradientLayer.GradientType.Radial ||
                              layer.gradientType == GradientLayer.GradientType.Diamond ||
                              layer.gradientType == GradientLayer.GradientType.Square;
                bool circular = layer.gradientType == GradientLayer.GradientType.Circular;
                center.style.display = radial || circular ? DisplayStyle.Flex : DisplayStyle.None;
                radius.style.display = radial ? DisplayStyle.Flex : DisplayStyle.None;
                circularSettings.style.display = circular ? DisplayStyle.Flex : DisplayStyle.None;
            });
        }
    }
}
