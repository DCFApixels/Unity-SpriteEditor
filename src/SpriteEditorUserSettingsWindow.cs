using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    internal sealed class SpriteEditorUserSettingsWindow : EditorWindow
    {
        private ColorField checkerLight;
        private ColorField checkerDark;
        private ColorField invalidPixels;
        private ColorField postFxBackground;
        private SliderInt checkerSize;

        internal static void Open()
        {
            var window = GetWindow<SpriteEditorUserSettingsWindow>(true, "Sprite Editor Settings");
            window.minSize = new Vector2(340f, 250f);
            window.Show();
        }

        private void OnEnable() => SpriteEditorUserSettings.Changed += RefreshValues;
        private void OnDisable() => SpriteEditorUserSettings.Changed -= RefreshValues;
        private void OnFocus() => RefreshValues();

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            SpriteEditorUI.ApplyWindowStyles(root);
            root.AddToClassList("sprite-editor-user-settings");
            var scroll = new ScrollView();
            root.Add(scroll);
            AddHeading(scroll, "Transparency Checkerboard");
            checkerLight = AddColor(scroll, "Light Squares", value => SpriteEditorUserSettings.CheckerLight = value);
            checkerDark = AddColor(scroll, "Dark Squares", value => SpriteEditorUserSettings.CheckerDark = value);
            checkerSize = new SliderInt("Cell Size", SpriteEditorUserSettings.MinimumCheckerSize, SpriteEditorUserSettings.MaximumCheckerSize)
            {
                showInputField = true,
                tooltip = "Size of one checkerboard square in UI pixels, independent of canvas zoom. Default: 16."
            };
            checkerSize.AddToClassList("sprite-editor-user-settings-color");
            checkerSize.RegisterValueChangedCallback(evt =>
            {
                SpriteEditorUserSettings.CheckerSize = evt.newValue;
                checkerSize.SetValueWithoutNotify(SpriteEditorUserSettings.CheckerSize);
            });
            scroll.Add(checkerSize);
            AddHeading(scroll, "Debug Preview");
            invalidPixels = AddColor(scroll, "Invalid Pixels", value => SpriteEditorUserSettings.InvalidPixels = value);
            invalidPixels.tooltip = "Display color for the accumulated numeric-error mask when Debug is enabled. Does not change image pixels or exports.";
            AddHeading(scroll, "Post FX Preview");
            postFxBackground = AddColor(scroll, "Background", value => SpriteEditorUserSettings.PostFxBackground = value);
            postFxBackground.tooltip = "Opaque fill behind the composition before Post FX. Shared with the Post FX panel. Original alpha is still used for depth; document pixels and exports are unchanged.";
            var note = new Label("Saved for your user account. Applies to all Sprite Editor windows; documents and exports are unaffected.");
            note.AddToClassList("sprite-editor-user-settings-note");
            scroll.Add(note);
            var reset = new Button(SpriteEditorUserSettings.Reset) { text = "Reset Preview Appearance" };
            reset.AddToClassList("sprite-editor-user-settings-reset");
            scroll.Add(reset);
            RefreshValues();
        }

        private static void AddHeading(VisualElement parent, string text)
        {
            var label = new Label(text);
            label.AddToClassList("sprite-editor-user-settings-heading");
            parent.Add(label);
        }

        private static ColorField AddColor(VisualElement parent, string label, System.Action<Color> write)
        {
            var field = new ColorField(label) { hdr = false, showAlpha = false };
            field.AddToClassList("sprite-editor-user-settings-color");
            field.RegisterValueChangedCallback(evt => write(evt.newValue));
            parent.Add(field);
            return field;
        }

        private void RefreshValues()
        {
            checkerLight?.SetValueWithoutNotify(SpriteEditorUserSettings.CheckerLight);
            checkerDark?.SetValueWithoutNotify(SpriteEditorUserSettings.CheckerDark);
            invalidPixels?.SetValueWithoutNotify(SpriteEditorUserSettings.InvalidPixels);
            postFxBackground?.SetValueWithoutNotify(SpriteEditorUserSettings.PostFxBackground);
            checkerSize?.SetValueWithoutNotify(SpriteEditorUserSettings.CheckerSize);
        }
    }
}
