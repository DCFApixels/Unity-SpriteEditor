using System;
using UnityEditor;
using UnityEngine;

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

        protected override void DrawSettings(Layer source)
        {
            FileLayer layer = (FileLayer)source;
            SEGUI.DrawTextureTransform(ref layer.transform);
            layer.sourceTexture = (Texture2D)EditorGUILayout.ObjectField(
                "Source Texture",
                layer.sourceTexture,
                typeof(Texture2D),
                false);
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

        protected override void DrawSettings(Layer source)
        {
            ColorFillLayer layer = (ColorFillLayer)source;
            SEGUI.DrawTextureTransform(ref layer.transform);
            layer.color = EditorGUILayout.ColorField("Color", layer.color);
        }
    }
}
