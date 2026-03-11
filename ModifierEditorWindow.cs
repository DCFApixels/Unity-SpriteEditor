using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class ModifierEditorWindow : EditorWindow
{
    private Layer layer;
    private ReorderableList modifiersList;

    public static void Open(Layer layer)
    {
        var window = GetWindow<ModifierEditorWindow>(true, "Modifiers");
        window.layer = layer;
        window.SetupList();
        window.Show();
    }

    private void SetupList()
    {
        modifiersList = new ReorderableList(layer.modifiers, typeof(Material), true, true, true, true);
        modifiersList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Modifiers (Materials)");
        modifiersList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // Поле для выбора материала
            layer.modifiers[index] = (Material)EditorGUI.ObjectField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                layer.modifiers[index], typeof(Material), false);
        };
        // При добавлении нового элемента просто добавляем null
        modifiersList.onAddCallback = (ReorderableList list) =>
        {
            layer.modifiers.Add(null);
        };
        // Можно также добавить обработчик удаления, но по умолчанию он работает
    }

    private void OnGUI()
    {
        if (layer == null)
        {
            Close();
            return;
        }

        modifiersList.DoLayoutList();
    }
}