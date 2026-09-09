using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    internal static class LayerColorSettingsView
    {
        internal static DropdownField GroupBlend(GroupLayer group, Action<BlendMode, bool> change,
            SpriteEditorUI.ValueBindings bindings)
        {
            var choices = new List<string> { "Pass Through" };
            foreach (BlendMode mode in Enum.GetValues(typeof(BlendMode))) choices.Add(ObjectNames.NicifyVariableName(mode.ToString()));
            var field = new DropdownField(choices, 0);
            bindings.Track(field, () => group.compositing == GroupCompositing.PassThrough
                ? choices[0] : choices[(int)group.blendMode + 1]);
            field.RegisterValueChangedCallback(evt =>
            {
                int index = choices.IndexOf(evt.newValue);
                if (index >= 0) change(index == 0 ? group.blendMode : (BlendMode)(index - 1), index == 0);
            });
            return field;
        }

        internal static void Build(VisualElement root, Layer layer, Action<string, Action> apply,
            SpriteEditorUI.ValueBindings bindings, bool expanded, Action<bool> expansionChanged)
        {
            SpriteEditorUI.ApplyWindowStyles(root);
            var container = new VisualElement();
            container.AddToClassList(HelpBox.ussClassName);
            container.AddToClassList("sprite-editor-color-card");
            var card = new Foldout { text = "Color & Blending", value = expanded };
            card.RegisterValueChangedCallback(evt =>
            {
                if (evt.target == card) expansionChanged(evt.newValue);
            });
            container.Add(card);
            var preset = new DropdownField(new List<string> { "Standard", "HDR" }, 0);
            preset.AddToClassList("sprite-editor-color-preset");
            preset.tooltip = "Set both Color Range and Blend Range. An empty value means the ranges differ.";
            bindings.Track(preset, () => layer.colorRange == LayerColorRange.Standard && layer.blendRange == LayerBlendRange.Standard
                ? "Standard" : layer.colorRange == LayerColorRange.HDR && layer.blendRange == LayerBlendRange.HDR ? "HDR" : string.Empty);
            preset.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != "Standard" && evt.newValue != "HDR") return;
                apply("Change Layer Color and Blend Ranges", () =>
                {
                    bool hdr = evt.newValue == "HDR";
                    SetColorRange(layer, hdr ? LayerColorRange.HDR : LayerColorRange.Standard);
                    layer.blendRange = hdr ? LayerBlendRange.HDR : LayerBlendRange.Standard;
                });
            });
            container.Add(preset);
            VisualElement transform = root.Q<VisualElement>(className: "sprite-editor-transform-card");
            if (transform != null)
                transform.parent.Insert(transform.parent.IndexOf(transform) + 1, container);
            else
                root.Insert(0, container);
            var color = SpriteEditorUI.ConfigureField(new EnumField("Color Range", layer.colorRange));
            color.tooltip = "Standard clamps this layer after its FX. HDR keeps signed linear values beyond 0–1.";
            bindings.Track(color, () => (Enum)layer.colorRange);
            color.RegisterValueChangedCallback(evt => apply("Change Layer Color Range",
                () => SetColorRange(layer, (LayerColorRange)evt.newValue)));
            card.Add(color);
            var blend = SpriteEditorUI.ConfigureField(new EnumField("Blend Range", layer.blendRange));
            blend.tooltip = "Standard uses bounded blend functions in the legacy color space. HDR evaluates extended functions in linear light. Neither clamps the entire backdrop.";
            bindings.Track(blend, () => (Enum)layer.blendRange);
            blend.RegisterValueChangedCallback(evt => apply("Change Layer Blend Range", () => layer.blendRange = (LayerBlendRange)evt.newValue));
            card.Add(blend);
            bindings.Add(() =>
            {
                bool active = !(layer is GroupLayer group) || group.compositing == GroupCompositing.Isolated;
                color.SetEnabled(active); blend.SetEnabled(active); preset.SetEnabled(active);
            });
            if (layer is DrawingLayer pixels)
            {
                var storage = new Label();
                storage.AddToClassList("sprite-editor-storage-description");
                bindings.Add(() => storage.text = HdrUtility.IsHdr(pixels.StoredTexture)
                    ? "Drawing storage: 16-bit float / channel" : "Drawing storage: 8-bit / channel");
                card.Add(storage);
                var compact = new Button(() =>
                {
                    if (EditorUtility.DisplayDialog("Convert Drawing to 8-bit?",
                        "Clamp source pixels to 0–1 and reduce their precision. Color Range becomes Standard. Undo restores the pixels and storage format.", "Convert", "Cancel"))
                        apply("Convert Drawing to 8-bit", pixels.ConvertTo8Bit);
                }) { text = "Convert to 8-bit" };
                compact.AddToClassList("sprite-editor-compact-storage");
                bindings.Add(() => compact.SetEnabled(HdrUtility.IsHdr(pixels.StoredTexture)));
                card.Add(compact);
            }
        }

        private static void SetColorRange(Layer layer, LayerColorRange range)
        {
            if (layer is DrawingLayer drawing) drawing.SetColorRange(range);
            else layer.colorRange = range;
        }
    }
}
