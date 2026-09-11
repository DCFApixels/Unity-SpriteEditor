using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        [SerializeField] private bool brushesExpanded;
        [NonSerialized] private Button brushTab;
        [NonSerialized] private VisualElement brushDrawer;
        [NonSerialized] private SpriteEditorUI.ValueBindings brushSettingsBindings;

        private void BuildBrushTab(VisualElement tabs)
        {
            brushTab = new Button(() =>
            {
                brushesExpanded = !brushesExpanded;
                if (brushesExpanded) postFxExpanded = false;
                RefreshPostFxPanel();
            }) { tooltip = "Brushes: tip, spacing, scatter, size variation, tint and blending." };
            brushTab.AddToClassList("sprite-editor-post-fx-tab");
            tabs.Add(brushTab);
        }

        private void BuildBrushDrawer(VisualElement panel)
        {
            brushSettingsBindings = new SpriteEditorUI.ValueBindings();
            brushDrawer = new VisualElement();
            brushDrawer.AddToClassList("sprite-editor-post-fx-drawer");
            brushDrawer.Add(CreatePaneHeader("Brushes", "brushesTitle"));
            BuildBrushPresetControls(brushDrawer);
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("sprite-editor-post-fx-settings");
            brushDrawer.Add(scroll);
            panel.Add(brushDrawer);

            scroll.Add(CreateBrushSectionHeader("Tip", () => paintSettings.ResetBrushTip(),
                "Reset Tip: use a procedural brush in Hardness mode, clear the texture, use Alpha, disable texture SDF and restore the default gradient and hardness."));
            var tip = SpriteEditorUI.ConfigureField(new ObjectField("Texture")
                { objectType = typeof(Texture2D), allowSceneObjects = false, value = paintSettings.dynamics.tip,
                  tooltip = "No texture: procedural brush. Assign a texture: textured brush. Source texture import settings are not changed." });
            tip.RegisterValueChangedCallback(evt =>
            {
                var texture = evt.newValue as Texture2D;
                if (texture != null && compositor != null && ReferenceEquals(TextureCompositor.FindDocument(texture), compositor))
                {
                    tip.SetValueWithoutNotify(paintSettings.dynamics.tip);
                    ShowNotification(new GUIContent("Choose a texture other than this document's output."));
                    return;
                }
                ApplyPaintToolChange(() => paintSettings.SetBrushTip(texture));
            });
            brushSettingsBindings.Track(tip, () => (UnityEngine.Object)paintSettings.dynamics.tip);
            scroll.Add(tip);
            var proceduralMode = SpriteEditorUI.ConfigureField(new DropdownField("Mode",
                new System.Collections.Generic.List<string> { "Hardness", "SDF Gradient" }, (int)paintSettings.dynamics.proceduralMode)
            {
                tooltip = "Procedural brush: Hardness controls a soft circular tip; SDF Gradient maps color and opacity from its outer edge (0) to its center (1)."
            });
            proceduralMode.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() =>
                paintSettings.dynamics.proceduralMode = evt.newValue == "SDF Gradient" ? BrushProceduralMode.SdfGradient : BrushProceduralMode.Hardness));
            brushSettingsBindings.Track(proceduralMode, () => paintSettings.dynamics.proceduralMode == BrushProceduralMode.SdfGradient ? "SDF Gradient" : "Hardness");
            brushSettingsBindings.Add(() => proceduralMode.EnableInClassList("sprite-editor-brush-setting--hidden", paintSettings.dynamics.tip != null));
            scroll.Add(proceduralMode);
            var channel = SpriteEditorUI.ConfigureField(new EnumField("Tip Channel", paintSettings.dynamics.tipChannel));
            channel.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.tipChannel = (BrushTipChannel)evt.newValue));
            brushSettingsBindings.Track(channel, () => (Enum)paintSettings.dynamics.tipChannel);
            brushSettingsBindings.Add(() => channel.SetEnabled(paintSettings.dynamics.tip != null));
            scroll.Add(channel);
            var sdf = SpriteEditorUI.ConfigureField(new Toggle("SDF")
            {
                value = paintSettings.dynamics.tipSdf,
                tooltip = "Map the selected distance field through SDF Gradient. Alpha/Color use alpha; Luminance modes use brightness. Gradient alpha controls coverage and its RGB multiplies the brush color."
            });
            sdf.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.tipSdf = evt.newValue));
            brushSettingsBindings.Track(sdf, () => paintSettings.dynamics.tipSdf);
            brushSettingsBindings.Add(() => sdf.SetEnabled(paintSettings.dynamics.tip != null));
            brushSettingsBindings.Add(() => sdf.EnableInClassList("sprite-editor-brush-setting--hidden", paintSettings.dynamics.tip == null));
            scroll.Add(sdf);
            var hardness = AddBrushPercent(scroll, "Hardness", () => paintSettings.brushHardness, v => paintSettings.brushHardness = v,
                "Edge hardness of the procedural brush. Textured brushes use their own coverage or SDF Gradient.");
            brushSettingsBindings.Add(() => hardness.SetEnabled(paintSettings.dynamics.tip == null));
            brushSettingsBindings.Add(() => hardness.EnableInClassList("sprite-editor-brush-setting--hidden", paintSettings.dynamics.UsesSdfGradient));
            var sdfGradient = SpriteEditorUI.ConfigureField(SpriteEditorColorInputs.Bind(new GradientField("SDF Gradient")
            {
                tooltip = "Left = distance 0 (procedural edge), right = 1 (procedural center). Textured brushes use the selected distance field. Alpha keys shape coverage; color keys multiply the palette color and Tint."
            }, brushSettingsBindings, () => paintSettings.dynamics.tipGradient));
            sdfGradient.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.tipGradient = evt.newValue));
            brushSettingsBindings.Add(() => sdfGradient.EnableInClassList("sprite-editor-brush-setting--hidden", !paintSettings.dynamics.UsesSdfGradient));
            scroll.Add(sdfGradient);

            scroll.Add(CreateBrushSectionHeader("Stamps", () => paintSettings.ResetBrushStamps(),
                "Reset Stamps: Random, Fixed rotation, zero Angle Offset, no flips, scatter or size/angle variation. Spacing is unchanged."));
            var spacing = SpriteEditorUI.ConfigureField(new FloatField("Spacing (%)") { value = paintSettings.brushSpacing * 100f,
                tooltip = "Distance between stamp centers as a percentage of brush diameter. 100% is one diameter. The minimum distance is one pixel." });
            spacing.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() =>
                paintSettings.brushSpacing = Mathf.Clamp(float.IsNaN(evt.newValue) ? 16f : evt.newValue, 1f, 400f) * .01f));
            brushSettingsBindings.Track(spacing, () => paintSettings.brushSpacing * 100f);
            scroll.Add(spacing);
            var distance = new Label();
            distance.AddToClassList("sprite-editor-brush-spacing-info");
            brushSettingsBindings.Add(() => distance.text = $"Stamp every {Mathf.Max(1f, paintSettings.brushSize * paintSettings.brushSpacing):0.##} px");
            scroll.Add(distance);
            var algorithm = SpriteEditorUI.ConfigureField(new EnumField("Randomization", paintSettings.dynamics.randomAlgorithm)
            {
                tooltip = "Random uses ordinary randomness. Sobol spreads variation more evenly across stamps. Applies to scatter, size, angle and tint."
            });
            algorithm.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.randomAlgorithm = (BrushRandomAlgorithm)evt.newValue));
            brushSettingsBindings.Track(algorithm, () => (Enum)paintSettings.dynamics.randomAlgorithm);
            scroll.Add(algorithm);
            AddBrushPercent(scroll, "Scatter", () => paintSettings.dynamics.scatter, v => paintSettings.dynamics.scatter = v,
                "Random offset within a disk, measured in brush diameters. 0 keeps every stamp on the stroke.", 400f);
            var scatterBias = SpriteEditorUI.ConfigureField(new Slider("Scatter Bias", -100f, 100f)
            {
                value = paintSettings.dynamics.scatterBias * 100f, showInputField = true,
                tooltip = "Negative: concentrate stamp centers near the stroke. 0: uniform across the disk. Positive: concentrate near the outer edge. Scatter sets the maximum distance."
            });
            scatterBias.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.scatterBias = evt.newValue * .01f));
            brushSettingsBindings.Track(scatterBias, () => paintSettings.dynamics.scatterBias * 100f);
            brushSettingsBindings.Add(() => scatterBias.SetEnabled(paintSettings.dynamics.scatter > 0f));
            scroll.Add(scatterBias);
            AddBrushPercent(scroll, "Size Jitter", () => paintSettings.dynamics.sizeJitter, v => paintSettings.dynamics.sizeJitter = v,
                "Random size around the chosen diameter. 50% produces sizes from 50% to 150%.");
            var rotation = SpriteEditorUI.ConfigureField(new EnumField("Rotation", paintSettings.dynamics.rotationMode)
            {
                tooltip = "Fixed keeps the texture's original orientation. Stroke Direction aligns its right-facing axis with the stroke. The first click uses the original orientation. Angle Offset and then Angle Jitter are added afterwards."
            });
            rotation.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.rotationMode = (BrushRotationMode)evt.newValue));
            brushSettingsBindings.Track(rotation, () => (Enum)paintSettings.dynamics.rotationMode);
            brushSettingsBindings.Add(() => rotation.SetEnabled(paintSettings.dynamics.tip != null));
            scroll.Add(rotation);
            var offset = SpriteEditorUI.ConfigureField(new Slider("Angle Offset (°)", -180f, 180f)
            {
                value = paintSettings.dynamics.angleOffset, showInputField = true,
                tooltip = "Constant angle added after Rotation and before Angle Jitter. 90° turns the tip across the stroke in Stroke Direction mode. Requires a texture tip."
            });
            offset.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.angleOffset = evt.newValue));
            brushSettingsBindings.Track(offset, () => paintSettings.dynamics.angleOffset);
            brushSettingsBindings.Add(() => offset.SetEnabled(paintSettings.dynamics.tip != null));
            scroll.Add(offset);
            var angle = SpriteEditorUI.ConfigureField(new Slider("Angle Jitter (°)", 0f, 180f)
            {
                value = paintSettings.dynamics.angleJitter, showInputField = true,
                tooltip = "Random offset added after Rotation and Angle Offset, from minus to plus this angle. 180° covers all directions. A procedural brush is unchanged by rotation."
            });
            angle.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.angleJitter = evt.newValue));
            brushSettingsBindings.Track(angle, () => paintSettings.dynamics.angleJitter);
            brushSettingsBindings.Add(() => angle.SetEnabled(paintSettings.dynamics.tip != null));
            scroll.Add(angle);

            var flipX = SpriteEditorUI.ConfigureField(new Slider("Flip X", 0f, 1f)
            {
                value = paintSettings.dynamics.flipX, showInputField = true,
                tooltip = "Chance to mirror each texture stamp horizontally in the tip's local axes: 0 never, 0.5 half, 1 always. Uses Randomization."
            });
            flipX.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.flipX = evt.newValue));
            brushSettingsBindings.Track(flipX, () => paintSettings.dynamics.flipX);
            brushSettingsBindings.Add(() => flipX.SetEnabled(paintSettings.dynamics.tip != null));
            scroll.Add(flipX);
            var flipY = SpriteEditorUI.ConfigureField(new Slider("Flip Y", 0f, 1f)
            {
                value = paintSettings.dynamics.flipY, showInputField = true,
                tooltip = "Chance to mirror each texture stamp vertically in the tip's local axes: 0 never, 0.5 half, 1 always. Uses Randomization."
            });
            flipY.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.flipY = evt.newValue));
            brushSettingsBindings.Track(flipY, () => paintSettings.dynamics.flipY);
            brushSettingsBindings.Add(() => flipY.SetEnabled(paintSettings.dynamics.tip != null));
            scroll.Add(flipY);

            scroll.Add(CreateBrushSectionHeader("Color", () => paintSettings.ResetBrushColor(),
                "Reset Color: opaque white Tint, Normal blending applied per stroke. Palette colors, Opacity and Flow are unchanged."));
            var tintRow = new VisualElement();
            tintRow.AddToClassList("sprite-editor-brush-tint-row");
            var gradient = SpriteEditorUI.ConfigureField(new GradientField("Tint") { value = paintSettings.dynamics.tintGradient,
                tooltip = "Different color or alpha keys give each stamp a random tint. Identical keys give one tint. Opaque white leaves the palette color unchanged." });
            gradient.AddToClassList("sprite-editor-brush-tint-gradient");
            gradient.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.tintGradient = evt.newValue));
            brushSettingsBindings.Track(gradient, () => paintSettings.dynamics.tintGradient);
            tintRow.Add(gradient);
            var resetTint = new Button(() =>
            {
                ApplyPaintToolChange(() => paintSettings.dynamics.ResetTint());
                gradient.SetValueWithoutNotify(paintSettings.dynamics.tintGradient);
            }) { text = "↺", tooltip = "Reset Tint to opaque white." };
            resetTint.AddToClassList("sprite-editor-brush-tint-reset");
            tintRow.Add(resetTint);
            scroll.Add(tintRow);
            var modes = new System.Collections.Generic.List<string>();
            foreach (BlendMode mode in Enum.GetValues(typeof(BlendMode)))
                if (mode != BlendMode.Overwrite && mode != BlendMode.None) modes.Add(mode.ToString());
            var blend = SpriteEditorUI.ConfigureField(new DropdownField(modes, modes.IndexOf(paintSettings.dynamics.blend.ToString())) { label = "Blend" });
            blend.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.blend = (BlendMode)Enum.Parse(typeof(BlendMode), evt.newValue)));
            brushSettingsBindings.Track(blend, () => paintSettings.dynamics.blend.ToString());
            brushSettingsBindings.Add(() => blend.SetEnabled(paintSettings.tool != PaintToolMode.Eraser));
            scroll.Add(blend);
            var application = SpriteEditorUI.ConfigureField(new DropdownField("Apply Blend",
                new System.Collections.Generic.List<string> { "Per Stroke", "Per Stamp" }, (int)paintSettings.dynamics.blendApplication)
            {
                tooltip = "Per Stroke blends the accumulated stroke with the layer once. Per Stamp blends each stamp with the evolving layer, including earlier stamps. Opacity controls the whole result; Flow controls each stamp. Dense spacing costs more in Per Stamp mode."
            });
            application.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => paintSettings.dynamics.blendApplication =
                evt.newValue == "Per Stamp" ? BrushBlendApplication.Stamp : BrushBlendApplication.Stroke));
            brushSettingsBindings.Track(application, () => paintSettings.dynamics.blendApplication == BrushBlendApplication.Stamp ? "Per Stamp" : "Per Stroke");
            brushSettingsBindings.Add(() => application.SetEnabled(paintSettings.tool != PaintToolMode.Eraser));
            scroll.Add(application);
            scroll.Add(new HelpBox("Opacity limits a whole stroke. Flow controls each stamp and builds up inside the stroke. Both are in the preview header.", HelpBoxMessageType.Info));
            BuildBrushStrokePreview(brushDrawer);
            brushSettingsBindings.Refresh(true);
        }

        private VisualElement CreateBrushSectionHeader(string title, Action reset, string tooltip)
        {
            var header = new VisualElement();
            header.AddToClassList("sprite-editor-brush-section-header");
            var label = new Label(title);
            label.AddToClassList("sprite-editor-brush-section-title");
            header.Add(label);
            var button = new Button(() =>
            {
                ApplyPaintToolChange(reset);
                brushSettingsBindings?.Refresh(true);
            }) { text = "↺", tooltip = tooltip };
            button.AddToClassList("sprite-editor-brush-section-reset");
            header.Add(button);
            return header;
        }

        private Slider AddBrushPercent(VisualElement root, string label, Func<float> read, Action<float> write, string tooltip, float max = 100f)
        {
            var field = SpriteEditorUI.ConfigureField(new Slider(label, 0f, max)
                { value = read() * 100f, showInputField = true, tooltip = tooltip });
            field.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => write(evt.newValue * .01f)));
            brushSettingsBindings.Track(field, () => read() * 100f);
            root.Add(field);
            return field;
        }

        private void AddBrushHeaderPercent(VisualElement row, string label, Func<float> read, Action<float> write, string tooltip)
        {
            var field = CompactField(new FloatField(label) { value = read() * 100f, tooltip = tooltip }, 94f);
            field.AddToClassList("sprite-editor-brush-strength");
            toolkitHeaderBindings.Track(field, () => read() * 100f);
            field.RegisterValueChangedCallback(evt => ApplyPaintToolChange(() => write(
                Mathf.Clamp01((float.IsNaN(evt.newValue) ? 100f : evt.newValue) * .01f))));
            row.Add(field);
        }
    }
}
