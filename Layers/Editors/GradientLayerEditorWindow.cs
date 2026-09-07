using System;
using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public sealed class GradientLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(GradientLayer);

        public static void Open(GradientLayer layer, TextureCompositor compositor)
        {
            GradientLayerEditorWindow window = GetWindow<GradientLayerEditorWindow>(true, "Gradient Settings");
            window.Initialize(layer, compositor);
            window.Show();
        }

        protected override void DrawSettings(Layer source)
        {
            GradientLayer layer = (GradientLayer)source;
            SEGUI.DrawTextureTransform(ref layer.transform);

            layer.gradientType = (GradientLayer.GradientType)EditorGUILayout.EnumPopup("Gradient Type", layer.gradientType);
            layer.gradient = EditorGUILayout.GradientField("Gradient", layer.gradient);

            if (layer.gradientType == GradientLayer.GradientType.Radial ||
                layer.gradientType == GradientLayer.GradientType.Diamond ||
                layer.gradientType == GradientLayer.GradientType.Square)
            {
                layer.center = EditorGUILayout.Vector2Field("Center", layer.center);
                layer.radius = Mathf.Max(0f, EditorGUILayout.FloatField("Radius", layer.radius));
            }
            else if (layer.gradientType == GradientLayer.GradientType.Circular)
            {
                layer.center = EditorGUILayout.Vector2Field("Center", layer.center);
                layer.circularRepetitions = Mathf.Max(
                    float.Epsilon,
                    EditorGUILayout.FloatField("Repetitions", layer.circularRepetitions));
                layer.circularWrapMode = (GradientLayer.WrapMode)EditorGUILayout.EnumPopup(
                    "Wrap Mode",
                    layer.circularWrapMode);
            }
        }
    }
}
