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
            OpenPropertiesWindow<GradientLayerEditorWindow>(layer, compositor);
        }

        protected override void BuildSettings(VisualElement root, Layer source)
        {
            BuildFields(root, (GradientLayer)source, Compositor, ApplyLayerChange, SettingsBindings);
        }

        internal static void BuildFields(
            VisualElement root, GradientLayer layer, TextureCompositor compositor,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings)
        {
            SpriteEditorUI.AddTextureTransform(
                root,
                layer,
                compositor,
                applyChange,
                bindings);

            EnumField gradientType = SpriteEditorUI.ConfigureField(
                new EnumField("Gradient Type", layer.gradientType));
            bindings.Track(gradientType, () => (Enum)layer.gradientType);
            gradientType.RegisterValueChangedCallback(evt =>
            {
                applyChange(
                    "Change Gradient Type",
                    () => layer.gradientType = (GradientLayer.GradientType)evt.newValue);
            });
            root.Add(gradientType);

            GradientField gradient = SpriteEditorUI.ConfigureField(SpriteEditorColorInputs.Bind(new GradientField("Gradient"), bindings, () => layer.gradient));
            gradient.RegisterValueChangedCallback(evt =>
                applyChange("Change Gradient", () => layer.gradient = GradientUtility.Create(evt.newValue)));
            root.Add(gradient);

            Vector2Field center = SpriteEditorUI.ConfigureField(new Vector2Field("Center"));
            bindings.Track(center, () => layer.center);
            center.RegisterValueChangedCallback(evt =>
                applyChange("Change Gradient Center", () => layer.center = evt.newValue));
            root.Add(center);

            FloatField radius = SpriteEditorUI.ConfigureField(new FloatField("Radius"));
            bindings.Track(radius, () => layer.radius);
            radius.RegisterValueChangedCallback(evt =>
                applyChange("Change Gradient Radius", () => layer.radius = Mathf.Max(0f, evt.newValue)));
            root.Add(radius);

            VisualElement circularSettings = new VisualElement();
            FloatField repetitions = SpriteEditorUI.ConfigureField(new FloatField("Repetitions"));
            bindings.Track(repetitions, () => layer.circularRepetitions);
            repetitions.RegisterValueChangedCallback(evt => applyChange(
                "Change Gradient Repetitions",
                () => layer.circularRepetitions = Mathf.Max(float.Epsilon, evt.newValue)));
            circularSettings.Add(repetitions);

            EnumField wrapMode = SpriteEditorUI.ConfigureField(
                new EnumField("Wrap Mode", layer.circularWrapMode));
            bindings.Track(wrapMode, () => (Enum)layer.circularWrapMode);
            wrapMode.RegisterValueChangedCallback(evt => applyChange(
                "Change Gradient Wrap Mode",
                () => layer.circularWrapMode = (GradientLayer.WrapMode)evt.newValue));
            circularSettings.Add(wrapMode);
            root.Add(circularSettings);
            bindings.Add(() =>
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
