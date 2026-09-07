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
            FileLayer layer = (FileLayer)source;
            SpriteEditorUI.AddTextureTransform(
                root,
                () => layer.transform,
                value => layer.transform = value,
                ApplyLayerChange);

            ObjectField texture = SpriteEditorUI.ConfigureField(new ObjectField("Source Texture")) as ObjectField;
            texture.objectType = typeof(Texture2D);
            texture.allowSceneObjects = false;
            texture.SetValueWithoutNotify(layer.sourceTexture);
            texture.RegisterValueChangedCallback(evt =>
                ApplyLayerChange("Change Source Texture", () => layer.sourceTexture = evt.newValue as Texture2D));
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
            ColorFillLayer layer = (ColorFillLayer)source;
            SpriteEditorUI.AddTextureTransform(
                root,
                () => layer.transform,
                value => layer.transform = value,
                ApplyLayerChange);

            ColorField color = SpriteEditorUI.ConfigureField(new ColorField("Color"));
            color.SetValueWithoutNotify(layer.color);
            color.RegisterValueChangedCallback(evt =>
                ApplyLayerChange("Change Fill Color", () => layer.color = evt.newValue));
            root.Add(color);
        }
    }
}
