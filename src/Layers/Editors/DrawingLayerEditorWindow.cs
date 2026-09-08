using System;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed class DrawingLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(DrawingLayer);

        public static void Open(DrawingLayer layer, TextureCompositor compositor)
        {
            DrawingLayerEditorWindow window = GetWindow<DrawingLayerEditorWindow>(true, "Drawing Layer");
            window.Initialize(layer, compositor);
            window.Show();
        }

        protected override void BuildSettings(VisualElement root, Layer source)
        {
            BuildFields(root, (DrawingLayer)source, ApplyLayerChange, SettingsBindings);
        }

        internal static void BuildFields(
            VisualElement root, DrawingLayer layer,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings)
        {
            SpriteEditorUI.AddTextureTransform(
                root,
                () => layer.transform,
                value => layer.transform = value,
                applyChange,
                bindings);
            SpriteEditorUI.AddHelpBox(root,
                "Select this layer in Sprite Editor to paint. Brush, symmetry, and repeat settings are in the Preview header.",
                HelpBoxMessageType.Info);
        }
    }
}
