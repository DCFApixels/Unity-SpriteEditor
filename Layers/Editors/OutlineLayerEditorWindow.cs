using System;
using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public sealed class OutlineLayerEditorWindow : LayerEditorWindowBase
    {
        protected override Type EditedLayerType => typeof(OutlineLayer);

        public static void Open(OutlineLayer layer, TextureCompositor compositor)
        {
            OutlineLayerEditorWindow window = GetWindow<OutlineLayerEditorWindow>(true, "Outline Settings");
            window.Initialize(layer, compositor);
            window.Show();
        }

        protected override void DrawSettings(Layer source)
        {
            OutlineLayer layer = (OutlineLayer)source;
            DrawEffectTarget(layer);
            SEGUI.DrawTextureTransform(ref layer.transform);
            layer.metric = (DistanceMetric)EditorGUILayout.EnumPopup("Distance Algorithm", layer.metric);
            layer.outlineColor = EditorGUILayout.ColorField("Color", layer.outlineColor);
            layer.outlineWidth = Mathf.Max(0f, EditorGUILayout.FloatField("Width (px)", layer.outlineWidth));
            layer.outlineSoftness = Mathf.Max(0f, EditorGUILayout.FloatField("Softness (px)", layer.outlineSoftness));
            layer.outlinePosition = (OutlineLayer.OutlinePosition)EditorGUILayout.EnumPopup(
                "Position",
                layer.outlinePosition);
        }
    }
}
