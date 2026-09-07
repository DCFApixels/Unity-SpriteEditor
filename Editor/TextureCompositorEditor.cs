using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    [CustomEditor(typeof(TextureCompositor))]
    public sealed class TextureCompositorEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            root.style.paddingTop = 4f;

            Button open = new Button(() => TextureCompositorWindow.Open((TextureCompositor)target))
            {
                text = "Open in Sprite Editor",
                tooltip = "Open this saved composition in the Sprite Editor window."
            };
            open.style.height = 28f;
            open.style.marginBottom = 6f;
            root.Add(open);

            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                PropertyField field = new PropertyField(property.Copy());
                if (property.propertyPath == "m_Script")
                    field.SetEnabled(false);
                root.Add(field);
            }

            root.Bind(serializedObject);
            root.TrackSerializedObjectValue(serializedObject, _ =>
            {
                foreach (Object inspectedTarget in targets)
                {
                    if (inspectedTarget is TextureCompositor compositor)
                        compositor.MarkChanged();
                }
            });
            return root;
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
