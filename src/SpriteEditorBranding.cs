using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    internal static class SpriteEditorBranding
    {
        private static Texture2D icon;

        internal static Texture2D Icon
        {
            get
            {
                if (icon == null)
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                        AssetDatabase.GUIDToAssetPath("fd19ec41578c43f8b0439d8ae8b4d49e"));
                return icon;
            }
        }

        internal static GUIContent WindowTitle(string title) => new GUIContent(title, Icon);
    }
}
