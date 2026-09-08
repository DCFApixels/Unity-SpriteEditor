using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositor
    {
        [InitializeOnLoadMethod]
        private static void RegisterModelUndoRefresh()
        {
            Undo.undoRedoPerformed -= RefreshModelsAfterUndo;
            Undo.undoRedoPerformed += RefreshModelsAfterUndo;
        }

        private static void RefreshModelsAfterUndo()
        {
            foreach (TextureCompositor document in Resources.FindObjectsOfTypeAll<TextureCompositor>())
            {
                document.InvalidateDrawingLayerSurfaces();
                Changed?.Invoke(document);
            }
        }
    }
}
