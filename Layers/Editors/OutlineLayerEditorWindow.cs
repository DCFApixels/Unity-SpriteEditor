using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed class OutlineLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(OutlineLayer);

        public static void Open(OutlineLayer layer, TextureCompositor compositor)
        {
            OutlineLayerEditorWindow window = GetWindow<OutlineLayerEditorWindow>(true, "Outline Settings");
            window.Initialize(layer, compositor);
            window.Show();
        }

        protected override void BuildSettings(VisualElement root, Layer source)
        {
            OutlineLayer layer = (OutlineLayer)source;
            AddEffectTarget(root, layer);
            SpriteEditorUI.AddTextureTransform(
                root,
                () => layer.transform,
                value => layer.transform = value,
                ApplyLayerChange);

            EnumField metric = SpriteEditorUI.ConfigureField(new EnumField("Distance Algorithm", layer.metric));
            metric.RegisterValueChangedCallback(evt => ApplyLayerChange(
                "Change Outline Algorithm",
                () => layer.metric = (DistanceMetric)evt.newValue));
            root.Add(metric);

            ColorField color = SpriteEditorUI.ConfigureField(new ColorField("Color"));
            color.SetValueWithoutNotify(layer.outlineColor);
            color.RegisterValueChangedCallback(evt =>
                ApplyLayerChange("Change Outline Color", () => layer.outlineColor = evt.newValue));
            root.Add(color);

            FloatField width = SpriteEditorUI.ConfigureField(new FloatField("Width (px)"));
            width.SetValueWithoutNotify(layer.outlineWidth);
            width.RegisterValueChangedCallback(evt => ApplyLayerChange(
                "Change Outline Width",
                () => layer.outlineWidth = Mathf.Max(0f, evt.newValue)));
            root.Add(width);

            FloatField softness = SpriteEditorUI.ConfigureField(new FloatField("Softness (px)"));
            softness.SetValueWithoutNotify(layer.outlineSoftness);
            softness.RegisterValueChangedCallback(evt => ApplyLayerChange(
                "Change Outline Softness",
                () => layer.outlineSoftness = Mathf.Max(0f, evt.newValue)));
            root.Add(softness);

            EnumField position = SpriteEditorUI.ConfigureField(new EnumField("Position", layer.outlinePosition));
            position.RegisterValueChangedCallback(evt => ApplyLayerChange(
                "Change Outline Position",
                () => layer.outlinePosition = (OutlineLayer.OutlinePosition)evt.newValue));
            root.Add(position);
        }
    }
}
