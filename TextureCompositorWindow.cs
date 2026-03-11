using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class TextureCompositorWindow : EditorWindow
{
    private TextureCompositor compositor;
    private ReorderableList layersList;
    private Texture2D previewTexture;
    private Vector2 scrollPos;

    [MenuItem("Window/Texture Compositor")]
    public static void ShowWindow()
    {
        GetWindow<TextureCompositorWindow>("Texture Compositor");
    }

    private void OnEnable()
    {
        // По умолчанию создаём временный композитор, можно также загружать из ассета
        if (compositor == null)
        {
            compositor = ScriptableObject.CreateInstance<TextureCompositor>();
        }
        SetupLayerList();
    }

    private void SetupLayerList()
    {
        layersList = new ReorderableList(compositor.layers, typeof(Layer), true, true, true, true);
        layersList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Layers");
        layersList.drawElementCallback = DrawLayerElement;
        layersList.onAddCallback = OnAddLayer;
        layersList.onRemoveCallback = OnRemoveLayer;
    }

    private void DrawLayerElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        Layer layer = compositor.layers[index];
        rect.y += 2;
        float lineHeight = EditorGUIUtility.singleLineHeight;


        // Превью
        Rect previewRect = new Rect(rect.x, rect.y, lineHeight, lineHeight);
        Texture previewTex = layer.GetPreviewTexture((int)lineHeight);
        if (previewTex != null)
            GUI.DrawTexture(previewRect, previewTex, ScaleMode.ScaleToFit, true);
        else
            EditorGUI.DrawRect(previewRect, Color.gray);
        rect.xMin += lineHeight + 5;

        // Включение/выключение
        Rect enabledRect = new Rect(rect.x, rect.y, 20, lineHeight);
        layer.enabled = EditorGUI.Toggle(enabledRect, layer.enabled);

        // Имя
        Rect nameRect = new Rect(rect.x + 25, rect.y, 120, lineHeight);
        layer.layerName = EditorGUI.TextField(nameRect, layer.layerName);

        // opacity
        float dw = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 10f;
        Rect opacityRect = new Rect(rect.x + 150, rect.y, 60, lineHeight);
        layer.opacity = Mathf.Clamp01(EditorGUI.FloatField(opacityRect, ">", layer.opacity));
        rect.xMin += 60;

        // Режим смешивания
        Rect blendRect = new Rect(rect.x + 150, rect.y, 100, lineHeight);
        layer.blendMode = (BlendMode)EditorGUI.EnumPopup(blendRect, layer.blendMode);

        // Параметры в зависимости от типа слоя
        if (layer is FileLayer fileLayer)
        {
            Rect texRect = new Rect(rect.x + 260, rect.y, 150, lineHeight);
            fileLayer.sourceTexture = (Texture2D)EditorGUI.ObjectField(texRect, fileLayer.sourceTexture, typeof(Texture2D), false);
        }
        else if (layer is GradientLayer gradientLayer)
        {
            Rect typeRect = new Rect(rect.x + 260, rect.y, 80, lineHeight);
            EditorGUI.LabelField(typeRect, gradientLayer.ToString());

            if (GUI.Button(new Rect(rect.x + 345, rect.y, 50, lineHeight), "Edit"))
            {
                GradientLayerEditorWindow.Open(gradientLayer);
            }
        }
        else if(layer is OutlineLayer outlineLayer)
        {
            Rect typeRect = new Rect(rect.x + 260, rect.y, 80, lineHeight);
            EditorGUI.LabelField(typeRect, outlineLayer.ToString());

            if (GUI.Button(new Rect(rect.x + 345, rect.y, 50, lineHeight), "Edit"))
            {
                OutlineLayerEditorWindow.Open(outlineLayer, compositor);
            }
        }
        else if (layer is SDFLayer sdfLayer)
        {
            Rect typeRect = new Rect(rect.x + 260, rect.y, 80, lineHeight);
            EditorGUI.LabelField(typeRect, sdfLayer.ToString());

            if (GUI.Button(new Rect(rect.x + 345, rect.y, 50, lineHeight), "Edit"))
            {
                SDFLayerEditorWindow.Open(sdfLayer, compositor);
            }
        }

        // Кнопка модификаторов
        if (GUI.Button(new Rect(rect.x + 410, rect.y, 80, lineHeight), "Modifiers"))
        {
            ModifierEditorWindow.Open(layer);
        }
    }

    private void OnAddLayer(ReorderableList list)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("File Layer"), false, () => AddLayer(new FileLayer()));
        menu.AddItem(new GUIContent("Gradient Layer"), false, () => AddLayer(new GradientLayer()));
        menu.AddItem(new GUIContent("Outline Layer"), false, () => AddLayer(new OutlineLayer()));
        menu.AddItem(new GUIContent("SDF Layer"), false, () => AddLayer(new SDFLayer()));
        menu.ShowAsContext();
    }

    private void AddLayer(Layer layer)
    {
        layer.layerName = "Layer " + (compositor.layers.Count + 1);
        compositor.layers.Add(layer);
        Repaint();
    }

    private void OnRemoveLayer(ReorderableList list)
    {
        compositor.layers.RemoveAt(list.index);
        Repaint();
    }

    private void OnGUI()
    {
        if (compositor == null)
        {
            compositor = ScriptableObject.CreateInstance<TextureCompositor>();
            SetupLayerList();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Ассет композитора (опционально)
        EditorGUILayout.BeginHorizontal();
        compositor = (TextureCompositor)EditorGUILayout.ObjectField("Compositor Asset", compositor, typeof(TextureCompositor), false);
        if (GUILayout.Button("New", GUILayout.Width(50)))
        {
            compositor = ScriptableObject.CreateInstance<TextureCompositor>();
            SetupLayerList();
        }
        EditorGUILayout.EndHorizontal();

        // Размер вывода
        EditorGUILayout.LabelField("Output Size", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        compositor.width = EditorGUILayout.IntField("Width", compositor.width);
        compositor.height = EditorGUILayout.IntField("Height", compositor.height);
        EditorGUILayout.EndHorizontal();

        // Список слоёв
        layersList.DoLayoutList();

        // Предпросмотр
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh Preview"))
        {
            UpdatePreview();
        }

        if (previewTexture != null)
        {
            Rect previewRect = EditorGUILayout.GetControlRect(false, 256);
            EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit);
            //GUI.Label(previewRect, previewTexture, EditorStyles.centeredGreyMiniLabel);
        }

        // Экспорт
        if (GUILayout.Button("Export to PNG"))
        {
            ExportTexture();
        }

        EditorGUILayout.EndScrollView();
    }

    private void UpdatePreview()
    {
        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
        }
        previewTexture = compositor.Compose();
        Repaint();
    }

    private void ExportTexture()
    {
        string path = EditorUtility.SaveFilePanel("Save Texture", Application.dataPath, "texture", "png");
        if (!string.IsNullOrEmpty(path))
        {
            Texture2D tex = compositor.Compose();
            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            DestroyImmediate(tex);
        }
    }
}