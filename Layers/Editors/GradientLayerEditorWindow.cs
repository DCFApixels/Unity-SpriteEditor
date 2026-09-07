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
                ApplyLayerChange);

            EnumField gradientType = SpriteEditorUI.ConfigureField(
                new EnumField("Gradient Type", layer.gradientType));
            gradientType.RegisterValueChangedCallback(evt =>
            {
                ApplyLayerChange(
                    "Change Gradient Type",
                    () => layer.gradientType = (GradientLayer.GradientType)evt.newValue);
                RebuildInterface();
            });
            root.Add(gradientType);

            GradientField gradient = SpriteEditorUI.ConfigureField(new GradientField("Gradient"));
            gradient.SetValueWithoutNotify(layer.gradient);
            gradient.RegisterValueChangedCallback(evt =>
                ApplyLayerChange("Change Gradient", () => layer.gradient = GradientUtility.Create(evt.newValue)));
            root.Add(gradient);

            if (layer.gradientType == GradientLayer.GradientType.Radial ||
                layer.gradientType == GradientLayer.GradientType.Diamond ||
                layer.gradientType == GradientLayer.GradientType.Square)
            {
                Vector2Field center = SpriteEditorUI.ConfigureField(new Vector2Field("Center"));
                center.SetValueWithoutNotify(layer.center);
                center.RegisterValueChangedCallback(evt =>
                    ApplyLayerChange("Change Gradient Center", () => layer.center = evt.newValue));
                root.Add(center);

                FloatField radius = SpriteEditorUI.ConfigureField(new FloatField("Radius"));
                radius.SetValueWithoutNotify(layer.radius);
                radius.RegisterValueChangedCallback(evt =>
                    ApplyLayerChange("Change Gradient Radius", () => layer.radius = Mathf.Max(0f, evt.newValue)));
                root.Add(radius);
            }
            else if (layer.gradientType == GradientLayer.GradientType.Circular)
            {
                Vector2Field center = SpriteEditorUI.ConfigureField(new Vector2Field("Center"));
                center.SetValueWithoutNotify(layer.center);
                center.RegisterValueChangedCallback(evt =>
                    ApplyLayerChange("Change Gradient Center", () => layer.center = evt.newValue));
                root.Add(center);

                FloatField repetitions = SpriteEditorUI.ConfigureField(new FloatField("Repetitions"));
                repetitions.SetValueWithoutNotify(layer.circularRepetitions);
                repetitions.RegisterValueChangedCallback(evt => ApplyLayerChange(
                    "Change Gradient Repetitions",
                    () => layer.circularRepetitions = Mathf.Max(float.Epsilon, evt.newValue)));
                root.Add(repetitions);

                EnumField wrapMode = SpriteEditorUI.ConfigureField(
                    new EnumField("Wrap Mode", layer.circularWrapMode));
                wrapMode.RegisterValueChangedCallback(evt => ApplyLayerChange(
                    "Change Gradient Wrap Mode",
                    () => layer.circularWrapMode = (GradientLayer.WrapMode)evt.newValue));
                root.Add(wrapMode);
            }
        }
    }
}
