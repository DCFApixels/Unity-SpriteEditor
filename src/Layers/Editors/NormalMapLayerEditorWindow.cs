using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed class NormalMapLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(NormalMapLayer);
        protected override string PreviewTitle => "Preview (Normal Map)";
        public static void Open(NormalMapLayer layer, TextureCompositor compositor) =>
            OpenPropertiesWindow<NormalMapLayerEditorWindow>(layer, compositor);
        protected override void BuildSettings(VisualElement root, Layer source) =>
            BuildFields(root, (NormalMapLayer)source, Compositor, ApplyLayerChange, SettingsBindings, AddEffectTarget);

        internal static void BuildFields(VisualElement root, NormalMapLayer layer, TextureCompositor compositor,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings,
            Action<VisualElement, TargetedLayerEffect> addEffectTarget)
        {
            addEffectTarget(root, layer);
            SpriteEditorUI.AddTextureTransform(root, layer, compositor, applyChange, bindings);
            void Choice<T>(string label, Func<T> get, Action<T> set, string tip = null) where T : Enum
            {
                var field = SpriteEditorUI.ConfigureField(new EnumField(label, get()));
                field.tooltip = tip;
                bindings.Track(field, () => (Enum)get());
                field.RegisterValueChangedCallback(evt => applyChange("Change Normal Map " + label, () => set((T)evt.newValue)));
                root.Add(field);
            }
            void Number(string label, Func<float> get, Action<float> set, float min, float max, string tip = null,
                bool textureOnly = false)
            {
                var field = SpriteEditorUI.ConfigureField(new Slider(label, min, max) { showInputField = true });
                field.tooltip = tip;
                field.SetValueWithoutNotify(get());
                bindings.Track(field, get);
                field.RegisterValueChangedCallback(evt => applyChange("Change Normal Map " + label,
                    () => set(Mathf.Clamp(evt.newValue, min, max))));
                if (textureOnly) bindings.Add(() => field.SetEnabled(layer.mode == NormalMapLayer.GenerationMode.Texture));
                root.Add(field);
            }
            void Flag(string label, Func<bool> get, Action<bool> set, string tip = null)
            {
                var field = SpriteEditorUI.ConfigureField(new Toggle(label));
                field.tooltip = tip;
                field.SetValueWithoutNotify(get());
                bindings.Track(field, get);
                field.RegisterValueChangedCallback(evt => applyChange("Change Normal Map " + label, () => set(evt.newValue)));
                root.Add(field);
            }
            Choice("Mode", () => layer.mode, v => layer.mode = v,
                "Texture infers height from image contrast at multiple scales; it cannot recover true geometry or reliably separate lighting from surface color.");
            Choice("Source Channel", () => layer.sourceChannel, v => layer.sourceChannel = v);
            Choice("Input Space", () => layer.inputSpace, v => layer.inputSpace = v,
                "Color Values uses displayed RGB values. Linear uses working linear values, suitable for data textures imported without sRGB. Alpha is unchanged.");
            Number("Strength", () => layer.strength, v => layer.strength = v, 0f, 128f,
                "Height-to-slope amplitude in full-resolution canvas pixels. Zero produces a flat normal.");
            Number("Black Level", () => layer.blackLevel, v => layer.blackLevel = Mathf.Min(v, layer.whiteLevel - .0001f), 0f, 1f);
            Number("White Level", () => layer.whiteLevel, v => layer.whiteLevel = Mathf.Max(v, layer.blackLevel + .0001f), .0001f, 16f);
            Number("Gamma", () => layer.gamma, v => layer.gamma = v, .05f, 8f);
            Flag("Invert Height", () => layer.inverted, v => layer.inverted = v);
            Number("Smoothing (px)", () => layer.smoothing, v => layer.smoothing = v, 0f, 64f,
                "Suppress fine noise before calculating derivatives. Radius is in full-resolution canvas pixels.");
            Choice("Derivative", () => layer.derivative, v => layer.derivative = v);
            Number("Medium Radius (px)", () => layer.mediumRadius, v => layer.mediumRadius = Mathf.Min(v, layer.largeRadius), .5f, 128f, textureOnly: true);
            Number("Large Radius (px)", () => layer.largeRadius, v => layer.largeRadius = Mathf.Max(v, layer.mediumRadius), .5f, 512f, textureOnly: true);
            Number("Fine Detail", () => layer.fineDetail, v => layer.fineDetail = v, 0f, 8f, textureOnly: true);
            Number("Medium Detail", () => layer.mediumDetail, v => layer.mediumDetail = v, 0f, 8f, textureOnly: true);
            Number("Large Detail", () => layer.largeDetail, v => layer.largeDetail = v, 0f, 8f, textureOnly: true);
            Number("Light Removal", () => layer.lightRemoval, v => layer.lightRemoval = v, 0f, 1f,
                "Attenuates the broadest brightness band. This may also remove real large-scale relief.", textureOnly: true);
            Choice("Edges", () => layer.edges, v => layer.edges = v, "Sampling at canvas edges; independent of the layer's Transform tiling.");
            Flag("Ignore Transparent", () => layer.ignoreTransparent, v => layer.ignoreTransparent = v,
                "Normalize smoothing by source alpha to avoid dark fringes. Alpha height always includes transparency as height.");
            Flag("Flip X", () => layer.flipX, v => layer.flipX = v);
            Flag("Flip Y", () => layer.flipY, v => layer.flipY = v);
            Choice("Alpha", () => layer.alphaMode, v => layer.alphaMode = v);
            Choice("Output", () => layer.output, v => layer.output = v, "Height outputs the reconstructed height for tuning. Switch back to Normal for a normal map.");
            Choice("Encoding", () => layer.encoding, v => layer.encoding = v,
                "Packed Color keeps normal channel values correct in PNG/TGA/PSD and the color preview. Linear Data keeps raw 0–1 vector data in linear EXR/Texture assets; its color preview looks brighter.");
        }
    }
}
