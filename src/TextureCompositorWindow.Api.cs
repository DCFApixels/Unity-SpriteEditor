using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        internal static bool IsDocumentBusyForApi(TextureCompositor document)
        {
            foreach (TextureCompositorWindow window in Resources.FindObjectsOfTypeAll<TextureCompositorWindow>())
                if (window.compositor == document && (window.paintingLayer != null ||
                    window.previewTransformManipulator != null && window.previewTransformManipulator.IsDragging))
                    return true;
            return false;
        }
    }
}
