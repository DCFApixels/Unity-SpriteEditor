using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed class FileLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(FileLayer);

        public static void Open(FileLayer layer, TextureCompositor compositor)
        {
            FileLayerEditorWindow window = GetWindow<FileLayerEditorWindow>(true, "File Layer");
            window.Initialize(layer, compositor);
            window.Show();
        }

        protected override void BuildSettings(VisualElement root, Layer source)
        {
            BuildFields(root, (FileLayer)source, ApplyLayerChange, SettingsBindings);
        }

        internal static void BuildFields(
            VisualElement root, FileLayer layer,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings)
        {
            SpriteEditorUI.AddTextureTransform(
                root,
                () => layer.transform,
                value => layer.transform = value,
                applyChange,
                bindings);

            ObjectField texture = SpriteEditorUI.ConfigureField(new ObjectField("Source Texture")) as ObjectField;
            texture.objectType = typeof(Texture2D);
            texture.allowSceneObjects = false;
            texture.SetValueWithoutNotify(layer.sourceTexture);
            bindings.Track(texture, () => (UnityEngine.Object)layer.sourceTexture);
            texture.RegisterValueChangedCallback(evt =>
                applyChange("Change Source Texture", () => layer.sourceTexture = evt.newValue as Texture2D));
            root.Add(texture);
        }
    }

    public sealed class ColorFillLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(ColorFillLayer);

        public static void Open(ColorFillLayer layer, TextureCompositor compositor)
        {
            ColorFillLayerEditorWindow window = GetWindow<ColorFillLayerEditorWindow>(true, "Color Fill Layer");
            window.Initialize(layer, compositor);
            window.Show();
        }

        protected override void BuildSettings(VisualElement root, Layer source)
        {
            BuildFields(root, (ColorFillLayer)source, ApplyLayerChange, SettingsBindings);
        }

        internal static void BuildFields(
            VisualElement root, ColorFillLayer layer,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings)
        {
            SpriteEditorUI.AddTextureTransform(
                root,
                () => layer.transform,
                value => layer.transform = value,
                applyChange,
                bindings);

            ColorField color = SpriteEditorUI.ConfigureField(new ColorField("Color"));
            color.SetValueWithoutNotify(layer.color);
            bindings.Track(color, () => layer.color);
            color.RegisterValueChangedCallback(evt =>
                applyChange("Change Fill Color", () => layer.color = evt.newValue));
            root.Add(color);
        }
    }
}
