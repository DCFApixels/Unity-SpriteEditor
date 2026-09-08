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
            OpenPropertiesWindow<OutlineLayerEditorWindow>(layer, compositor);
        }

        protected override void BuildSettings(VisualElement root, Layer source)
        {
            BuildFields(root, (OutlineLayer)source, ApplyLayerChange, SettingsBindings, AddEffectTarget);
        }

        internal static void BuildFields(
            VisualElement root, OutlineLayer layer,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings,
            Action<VisualElement, TargetedLayerEffect> addEffectTarget)
        {
            addEffectTarget(root, layer);
            SpriteEditorUI.AddTextureTransform(
                root,
                () => layer.transform,
                value => layer.transform = value,
                applyChange,
                bindings);

            EnumField metric = SpriteEditorUI.ConfigureField(new EnumField("Distance Algorithm", layer.metric));
            bindings.Track(metric, () => (Enum)layer.metric);
            metric.RegisterValueChangedCallback(evt => applyChange(
                "Change Outline Algorithm",
                () => layer.metric = (DistanceMetric)evt.newValue));
            root.Add(metric);

            ColorField color = SpriteEditorUI.ConfigureField(new ColorField("Color"));
            color.SetValueWithoutNotify(layer.outlineColor);
            bindings.Track(color, () => layer.outlineColor);
            color.RegisterValueChangedCallback(evt =>
                applyChange("Change Outline Color", () => layer.outlineColor = evt.newValue));
            root.Add(color);

            FloatField width = SpriteEditorUI.ConfigureField(new FloatField("Width (px)"));
            width.SetValueWithoutNotify(layer.outlineWidth);
            bindings.Track(width, () => layer.outlineWidth);
            width.RegisterValueChangedCallback(evt => applyChange(
                "Change Outline Width",
                () => layer.outlineWidth = Mathf.Max(0f, evt.newValue)));
            root.Add(width);

            FloatField softness = SpriteEditorUI.ConfigureField(new FloatField("Softness (px)"));
            softness.SetValueWithoutNotify(layer.outlineSoftness);
            bindings.Track(softness, () => layer.outlineSoftness);
            softness.RegisterValueChangedCallback(evt => applyChange(
                "Change Outline Softness",
                () => layer.outlineSoftness = Mathf.Max(0f, evt.newValue)));
            root.Add(softness);

            EnumField position = SpriteEditorUI.ConfigureField(new EnumField("Position", layer.outlinePosition));
            bindings.Track(position, () => (Enum)layer.outlinePosition);
            position.RegisterValueChangedCallback(evt => applyChange(
                "Change Outline Position",
                () => layer.outlinePosition = (OutlineLayer.OutlinePosition)evt.newValue));
            root.Add(position);
        }
    }
}
