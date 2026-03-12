using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class OutlineLayerEditorWindow : EditorWindow
{
    private OutlineLayer layer;
    private TextureCompositor compositor;
    private int selectedTargetIndex;
    private Texture2D previewTexture;
    private const int previewSize = 256;

    public static void Open(OutlineLayer layer, TextureCompositor compositor)
    {
        var window = GetWindow<OutlineLayerEditorWindow>(true, "Outline Settings");
        window.layer = layer;
        window.compositor = compositor;
        window.selectedTargetIndex = layer.targetLayerIndex >= 0 ? layer.targetLayerIndex : 0;
        window.Show();
    }

    private void OnEnable()
    {
        if (layer != null && compositor != null)
            UpdatePreview();
    }

    private bool _isInit = false;
    private void OnGUI()
    {
        if (_isInit == false)
        {
            UpdatePreview();
            _isInit = true;
        }
        if (layer == null || compositor == null)
        {
            Close();
            return;
        }

        EditorGUI.BeginChangeCheck();


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        layer.pivot = EditorGUILayout.Vector2Field("Pivot", layer.pivot);
        layer.position = EditorGUILayout.Vector2Field("Position", layer.position);
        layer.scale = EditorGUILayout.Vector2Field("Scale", layer.scale);
        layer.rotation = EditorGUILayout.FloatField("Rotation", layer.rotation);
        if (GUILayout.Button("Reset Transform"))
        {
            layer.pivot = new Vector2(0.5f, 0.5f);
            layer.position = Vector2.zero;
            layer.scale = Vector2.one;
            layer.rotation = 0f;
            GUI.changed = true;
        }
        EditorGUI.indentLevel--;


        // Выбор целевого слоя
        List<string> layerNames = new List<string>();
        int currentLayerIndex = compositor.layers.IndexOf(layer);
        for (int i = 0; i < compositor.layers.Count; i++)
        {
            if (i == currentLayerIndex) continue; // нельзя выбрать себя
            layerNames.Add($"{i}: {compositor.layers[i].layerName}");
        }
        if (layerNames.Count == 0)
        {
            EditorGUILayout.LabelField("No other layers available.");
        }
        else
        {
            int selected = EditorGUILayout.Popup("Target Layer", selectedTargetIndex, layerNames.ToArray());
            if (selected != selectedTargetIndex)
            {
                selectedTargetIndex = selected;
                // Найти реальный индекс слоя (пропуская себя)
                int realIndex = 0;
                for (int i = 0; i < compositor.layers.Count; i++)
                {
                    if (i == currentLayerIndex) continue;
                    if (realIndex == selected)
                    {
                        layer.targetLayerIndex = i;
                        break;
                    }
                    realIndex++;
                }
            }
        }

        layer.useAccumulation = EditorGUILayout.Toggle("Use Accumulation", layer.useAccumulation);
        layer.outlineColor = EditorGUILayout.ColorField("Color", layer.outlineColor);
        layer.outlineWidth = EditorGUILayout.FloatField("Width", layer.outlineWidth);
        layer.outlineSoftness = EditorGUILayout.FloatField("Softness", layer.outlineSoftness);
        layer.outlinePosition = (OutlineLayer.OutlinePosition)EditorGUILayout.EnumPopup("Position", layer.outlinePosition);

        if (EditorGUI.EndChangeCheck())
        {
            UpdatePreview();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        if (previewTexture != null)
        {
            Rect previewRect = EditorGUILayout.GetControlRect(false, previewSize);
            EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit);
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
        RenderTexture rt = layer.GetRenderTexture(compositor, compositor.layers.IndexOf(layer), previewSize, previewSize);
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

    private void OnDestroy()
    {
        if (previewTexture != null)
            DestroyImmediate(previewTexture);
    }
}