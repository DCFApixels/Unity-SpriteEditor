using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [CustomEditor(typeof(TextureCompositor))]
    public sealed class TextureCompositorEditor : Editor
    {
        private static readonly GUIContent OpenButtonContent = new GUIContent(
            "Open in Sprite Editor",
            "Open this saved composition in the Sprite Editor window.");

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button(OpenButtonContent, GUILayout.Height(28f)))
                TextureCompositorWindow.Open((TextureCompositor)target);

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (!EditorGUI.EndChangeCheck())
                return;

            foreach (Object inspectedTarget in targets)
            {
                if (inspectedTarget is TextureCompositor compositor)
                    compositor.MarkChanged();
            }
        }

        [OnOpenAsset]
#if UNITY_6000_2_OR_NEWER
        public static bool OpenTextureCompositor(EntityId instanceId, int line)
#else
        public static bool OpenTextureCompositor(int instanceId, int line)
#endif
        {
#if UNITY_6000_2_OR_NEWER
            TextureCompositor compositor =
                EditorUtility.EntityIdToObject(instanceId) as TextureCompositor;
#else
            TextureCompositor compositor =
                EditorUtility.InstanceIDToObject(instanceId) as TextureCompositor;
#endif
            if (compositor == null)
                return false;

            TextureCompositorWindow.Open(compositor);
            return true;
        }
    }
}
