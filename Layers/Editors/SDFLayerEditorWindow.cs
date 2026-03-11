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
        // Инициализируем выбранный индекс в соответствии с текущим targetLayerIndex
        window.selectedTargetIndex = GetPopupIndexFromLayerIndex(compositor, layer, layer.targetLayerIndex);
        window.Show();
    }

    private void OnEnable()
    {
        if (layer != null && compositor != null)
            UpdatePreview();
    }

    private void OnGUI()
    {
        if (layer == null || compositor == null)
        {
            Close();
            return;
        }

        EditorGUI.BeginChangeCheck();

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
                // Преобразуем индекс в списке (без учёта себя) в реальный индекс слоя
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

        if (EditorGUI.EndChangeCheck())
        {
            UpdatePreview();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview (SDF - Red channel)", EditorStyles.boldLabel);
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

        // Получаем RenderTexture от слоя
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

    // Вспомогательный метод для инициализации selectedTargetIndex по реальному индексу слоя
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
        return 0; // fallback
    }
}