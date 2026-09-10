using System;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed class ShaderProcessorLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(ShaderProcessorLayer);
        protected override string PreviewTitle => "Preview (processed lower layers)";
        public static void Open(ShaderProcessorLayer layer, TextureCompositor compositor) =>
            OpenPropertiesWindow<ShaderProcessorLayerEditorWindow>(layer, compositor);
        protected override void BuildSettings(VisualElement root, Layer layer)
        {
            SpriteEditorUI.AddTextureTransform(root, layer, Compositor, ApplyLayerChange, SettingsBindings);
            root.Add(new LayerShaderFXView(layer, Compositor, ApplyLayerChange));
        }
    }
}
