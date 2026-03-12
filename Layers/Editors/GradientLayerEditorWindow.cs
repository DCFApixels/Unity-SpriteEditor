using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public class GradientLayerEditorWindow : EditorWindow
    {
        private GradientLayer layer;
        private Texture2D previewTexture;
        private GradientLayer.GradientType lastType;
        private Gradient lastGradient;
        private Vector2 lastCenter;
        private float lastRadius;
        private bool needUpdatePreview = true;

        private const int PREVIEW_SIZE = 128;


        public static void Open(GradientLayer layer)
        {
            var window = GetWindow<GradientLayerEditorWindow>(true, "Gradient Settings");
            window.layer = layer;
            window.Show();
        }

        private void OnEnable()
        {
            if (layer != null)
            {
                SaveCurrentState();
                needUpdatePreview = true;
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

        private void SaveCurrentState()
        {
            if (layer == null) return;
            lastType = layer.gradientType;
            lastGradient = layer.gradient; // сохраняем ссылку, но содержимое может измениться
            lastCenter = layer.center;
            lastRadius = layer.radius;
        }

        private bool HasChanges()
        {
            if (layer == null) return false;
            if (layer.gradientType != lastType) return true;
            if (layer.center != lastCenter) return true;
            if (!Mathf.Approximately(layer.radius, lastRadius)) return true;
            // Для градиента сравнивать сложнее, будем считать по GUI.changed
            return false;
        }

        private void UpdatePreview()
        {
            if (layer == null) return;

            // Создаём временную текстуру градиента
            Texture2D newTex = GeneratePreviewTexture(PREVIEW_SIZE, PREVIEW_SIZE);

            if (previewTexture != null)
                DestroyImmediate(previewTexture);
            previewTexture = newTex;

            SaveCurrentState();
        }

        private Texture2D GeneratePreviewTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;
                    float t = layer.GetGradientCoord(u, v);
                    pixels[y * width + x] = layer.gradient.Evaluate(t);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
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
                if (GUILayout.Button("Close"))
                    Close();
                return;
            }

            EditorGUI.BeginChangeCheck();

            SEGUI.DrawTextureTransform(ref layer.transform);

            layer.gradientType = (GradientLayer.GradientType)EditorGUILayout.EnumPopup("Gradient Type", layer.gradientType);
            layer.gradient = EditorGUILayout.GradientField("Gradient", layer.gradient);

            EditorGUILayout.Space();

            if (layer.gradientType == GradientLayer.GradientType.Radial ||
                layer.gradientType == GradientLayer.GradientType.Diamond ||
                layer.gradientType == GradientLayer.GradientType.Square)
            {
                layer.center = EditorGUILayout.Vector2Field("Center", layer.center);
                layer.radius = EditorGUILayout.FloatField("Radius", layer.radius);
            }
            else if (layer.gradientType == GradientLayer.GradientType.Circular)
            {
                layer.center = EditorGUILayout.Vector2Field("Center", layer.center);
                layer.circularRepetitions = EditorGUILayout.FloatField("Repetitions", layer.circularRepetitions);
                if (layer.circularRepetitions <= 0) layer.circularRepetitions = float.Epsilon;
                layer.circularWrapMode = (GradientLayer.WrapMode)EditorGUILayout.EnumPopup("Wrap Mode", layer.circularWrapMode);
            }

            EditorGUILayout.Space();

            // Preview
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

            if (EditorGUI.EndChangeCheck())
            {
                needUpdatePreview = true;
                Repaint();
            }

            if (needUpdatePreview)
            {
                UpdatePreview();
                needUpdatePreview = false;
            }
        }
    }
}