using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public class FileLayerEditorWindow : EditorWindow
    {
        private FileLayer layer;
        private TextureCompositor compositor;
        private Texture2D previewTexture;
        private const int previewSize = 256;

        public static void Open(FileLayer layer, TextureCompositor compositor)
        {
            var window = GetWindow<FileLayerEditorWindow>(true, "File Layer");
            window.compositor = compositor;
            window.layer = layer;
            window.Show();
        }

        private void OnEnable()
        {
            if (layer != null)
            {
                UpdatePreview();
            }
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


            // Transform UI
            // Use SEGUI helper if available in project (used by other editors)
            SEGUI.DrawTextureTransform(ref layer.transform);

            // Texture selection (duplicated here and in compositor list)
            layer.sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", layer.sourceTexture, typeof(Texture2D), false);

            if (EditorGUI.EndChangeCheck())
            {
                UpdatePreview();
                Repaint();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            Rect previewRect = EditorGUILayout.GetControlRect(false, previewSize);
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
                DestroyImmediate(previewTexture);
            }
            // Генерируем превью, используя метод GetRenderTexture с временными параметрами
            float scale = 1f;
            if (compositor != null && previewSize > 0)
            {
                scale = (float)compositor.width / (float)previewSize;
            }

            RenderTexture rt = layer.GetRenderTexture(compositor, compositor.layers.IndexOf(layer), previewSize, previewSize, scale);
            if (rt != null)
            {
                previewTexture = new Texture2D(previewSize, previewSize, TextureFormat.RGBA32, false);
                RenderTexture.active = rt;
                previewTexture.ReadPixels(new Rect(0, 0, previewSize, previewSize), 0, 0);
                previewTexture.Apply();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
            }
            Repaint();
        }
    }
}