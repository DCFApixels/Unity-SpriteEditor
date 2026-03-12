using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public class FileLayerEditorWindow : EditorWindow
    {
        private FileLayer layer;
        private Texture2D previewTexture;
        private const int PREVIEW_SIZE = 256;

        public static void Open(FileLayer layer)
        {
            var window = GetWindow<FileLayerEditorWindow>(true, "File Layer");
            window.layer = layer;
            window.Show();
        }

        private void OnEnable()
        {
            if (layer != null)
                UpdatePreview();
        }

        private void OnDisable()
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
                previewTexture = null;
            }
        }

        private bool _isInit = false;
        private void OnGUI()
        {
            if (_isInit == false)
            {
                UpdatePreview();
                _isInit = true;
            }
            if (layer == null)
            {
                EditorGUILayout.LabelField("Layer is null");
                if (GUILayout.Button("Close")) Close();
                return;
            }

            EditorGUI.BeginChangeCheck();

            // Texture selection (duplicated here and in compositor list)
            layer.sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", layer.sourceTexture, typeof(Texture2D), false);

            // Transform UI
            // Use SEGUI helper if available in project (used by other editors)
            SEGUI.DrawTextureTransform(ref layer.transform);

            if (EditorGUI.EndChangeCheck())
            {
                UpdatePreview();
                Repaint();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            Rect previewRect = EditorGUILayout.GetControlRect(false, PREVIEW_SIZE);
            if (previewTexture != null)
            {
                EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, Color.gray);
            }

            if (GUILayout.Button("Close"))
            {
                Close();
            }
        }

        private void UpdatePreview()
        {
            if (previewTexture != null)
            {
                if (previewTexture != layer.sourceTexture)
                {
                    DestroyImmediate(previewTexture);
                }
                previewTexture = null;
            }

            if (layer == null) return;

            if (layer.sourceTexture != null)
            {
                // For file layer preview just show source texture (scaled by editor)
                previewTexture = layer.sourceTexture;
            }
        }
    }
}