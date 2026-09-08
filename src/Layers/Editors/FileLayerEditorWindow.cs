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
            OpenPropertiesWindow<FileLayerEditorWindow>(layer, compositor);
        }

        protected override void BuildSettings(VisualElement root, Layer source)
        {
            BuildFields(root, (FileLayer)source, Compositor, ApplyLayerChange, SettingsBindings);
        }

        internal static void BuildFields(
            VisualElement root, FileLayer layer, TextureCompositor compositor,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings)
        {
            SpriteEditorUI.AddTextureTransform(
                root,
                layer,
                compositor,
                applyChange,
                bindings);

            ObjectField texture = SpriteEditorUI.ConfigureField(new ObjectField("Source Texture")) as ObjectField;
            texture.objectType = typeof(Texture2D);
            texture.allowSceneObjects = false;
            texture.SetValueWithoutNotify(layer.sourceTexture);
            bindings.Track(texture, () => (UnityEngine.Object)layer.sourceTexture);
            texture.RegisterValueChangedCallback(evt =>
                applyChange("Change Source Texture", () => layer.AssignSourceTexture(evt.newValue as Texture2D, compositor)));
            root.Add(texture);
        }
    }

    public sealed class ColorFillLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(ColorFillLayer);

        public static void Open(ColorFillLayer layer, TextureCompositor compositor)
        {
            OpenPropertiesWindow<ColorFillLayerEditorWindow>(layer, compositor);
        }

        protected override void BuildSettings(VisualElement root, Layer source)
        {
            BuildFields(root, (ColorFillLayer)source, Compositor, ApplyLayerChange, SettingsBindings);
        }

        internal static void BuildFields(
            VisualElement root, ColorFillLayer layer, TextureCompositor compositor,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings)
        {
            SpriteEditorUI.AddTextureTransform(
                root,
                layer,
                compositor,
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
