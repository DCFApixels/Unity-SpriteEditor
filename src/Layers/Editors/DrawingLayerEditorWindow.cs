using System;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed class DrawingLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(DrawingLayer);

        public static void Open(DrawingLayer layer, TextureCompositor compositor)
        {
            OpenPropertiesWindow<DrawingLayerEditorWindow>(layer, compositor);
        }

        protected override void BuildSettings(VisualElement root, Layer source)
        {
            BuildFields(root, (DrawingLayer)source, Compositor, ApplyLayerChange, SettingsBindings);
        }

        internal static void BuildFields(
            VisualElement root, DrawingLayer layer, TextureCompositor compositor,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings)
        {
            SpriteEditorUI.AddTextureTransform(
                root,
                layer,
                compositor,
                applyChange,
                bindings);
            SpriteEditorUI.AddHelpBox(root,
                "Select this layer in Sprite Editor to paint. Brush, symmetry, and repeat settings are in the Preview header.",
                HelpBoxMessageType.Info);
        }
    }
}
