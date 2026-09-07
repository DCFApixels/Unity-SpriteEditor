using System;
using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public sealed class SDFLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(SDFLayer);
        protected override string PreviewTitle => "Preview (SDF)";

        public static void Open(SDFLayer layer, TextureCompositor compositor)
        {
            SDFLayerEditorWindow window = GetWindow<SDFLayerEditorWindow>(true, "SDF Settings");
            window.Initialize(layer, compositor);
            window.Show();
        }

        protected override void DrawSettings(Layer source)
        {
            SDFLayer layer = (SDFLayer)source;
            DrawEffectTarget(layer);
            SEGUI.DrawTextureTransform(ref layer.transform);
            layer.metric = (DistanceMetric)EditorGUILayout.EnumPopup("Distance Algorithm", layer.metric);
            layer.sourceChannel = (SDFLayer.SourceChannel)EditorGUILayout.EnumPopup("Source Channel", layer.sourceChannel);
            layer.threshold = (byte)EditorGUILayout.IntSlider("Threshold", layer.threshold, 0, 255);
            layer.maxDistanceNormalization = Mathf.Max(
                0f,
                EditorGUILayout.FloatField("Max Distance (px, 0 = auto)", layer.maxDistanceNormalization));
            layer.distancePosition = (SDFLayer.DistancePosition)EditorGUILayout.EnumPopup(
                "Position",
                layer.distancePosition);
            layer.inverted = EditorGUILayout.Toggle("Inverted", layer.inverted);
            layer.gradient = EditorGUILayout.GradientField("Gradient", layer.gradient);
        }
    }
}
