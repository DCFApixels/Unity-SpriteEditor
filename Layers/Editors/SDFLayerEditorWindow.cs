using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SDFLayerEditorWindow : EditorWindow
{
    private SDFLayer layer;
    private TextureCompositor compositor;
    private int selectedTargetIndex;
    private Texture2D previewTexture;
    private const int previewSize = 256;

    public static void Open(SDFLayer layer, TextureCompositor compositor)
    {
        var window = GetWindow<SDFLayerEditorWindow>(true, "SDF Settings");
        window.layer = layer;
        window.compositor = compositor;
        window.selectedTargetIndex = GetPopupIndexFromLayerIndex(compositor, layer, layer.targetLayerIndex);
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
            if (i == currentLayerIndex) continue;
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
                int realIndex = 0;
                int skipCount = 0;
                for (int i = 0; i < compositor.layers.Count; i++)
                {
                    if (i == currentLayerIndex) continue;
                    if (skipCount == selected)
                    {
                        realIndex = i;
                        break;
                    }
                    skipCount++;
                }
                layer.targetLayerIndex = realIndex;
            }
        }

        layer.useAccumulation = EditorGUILayout.Toggle("Use Accumulation", layer.useAccumulation);
        layer.metric = (SDFLayer.DistanceMetric)EditorGUILayout.EnumPopup("Distance Metric", layer.metric);
        layer.sourceChannel = (SDFLayer.SourceChannel)EditorGUILayout.EnumPopup("Source Channel", layer.sourceChannel);
        layer.threshold = (byte)EditorGUILayout.IntSlider("Threshold", layer.threshold, 0, 255);

        // Добавлено поле в OnGUI:
        layer.maxDistanceNormalization = EditorGUILayout.FloatField("Max Distance (0 = auto)", layer.maxDistanceNormalization);
        if (layer.maxDistanceNormalization < 0) layer.maxDistanceNormalization = 0;

        layer.distancePosition = (SDFLayer.DistancePosition)EditorGUILayout.EnumPopup("Position", layer.distancePosition);
        layer.inverted = EditorGUILayout.Toggle("Inverted", layer.inverted);

        if (EditorGUI.EndChangeCheck())
        {
            UpdatePreview();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview (SDF)", EditorStyles.boldLabel);
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

        int layerIndex = compositor.layers.IndexOf(layer);
        RenderTexture rt = layer.GetRenderTexture(compositor, layerIndex, previewSize, previewSize);
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

    private static int GetPopupIndexFromLayerIndex(TextureCompositor compositor, Layer currentLayer, int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= compositor.layers.Count)
            return 0;

        int currentIdx = compositor.layers.IndexOf(currentLayer);
        int popupIndex = 0;
        for (int i = 0; i < compositor.layers.Count; i++)
        {
            if (i == currentIdx) continue;
            if (i == targetIndex) return popupIndex;
            popupIndex++;
        }
        return 0;
    }
}