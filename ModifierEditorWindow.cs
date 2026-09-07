using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public sealed class ModifierEditorWindow : EditorWindow
    {
        [SerializeField] private TextureCompositor compositor;
        [SerializeField] private string layerId;

        private Layer layer;
        private ReorderableList modifiersList;

        public static void Open(Layer layer, TextureCompositor compositor)
        {
            ModifierEditorWindow window = GetWindow<ModifierEditorWindow>(true, "Layer Modifiers");
            window.compositor = compositor;
            window.layer = layer;
            window.layerId = layer?.Id;
            window.SetupList();
            window.Show();
        }

        private void OnEnable()
        {
            TextureCompositor.Changed += OnCompositorChanged;
        }

        private void OnDisable()
        {
            TextureCompositor.Changed -= OnCompositorChanged;
        }

        private void OnGUI()
        {
            if (!ResolveLayer())
            {
                EditorGUILayout.HelpBox("The edited layer no longer exists in this compositor.", MessageType.Info);
                if (GUILayout.Button("Close"))
                    Close();
                return;
            }

            if (modifiersList == null || !ReferenceEquals(modifiersList.list, layer.modifiers))
                SetupList();

            EditorGUILayout.HelpBox(
                "Materials are applied in list order after the layer transform.",
                MessageType.Info);
            Undo.RecordObject(compositor, "Edit Layer Modifiers");
            EditorGUI.BeginChangeCheck();
            modifiersList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
                compositor.MarkChanged();
        }

        private bool ResolveLayer()
        {
            if (compositor == null || string.IsNullOrEmpty(layerId))
                return false;

            Layer resolved = compositor.FindLayer(layerId);
            if (resolved == null)
                return false;
            if (!ReferenceEquals(resolved, layer))
            {
                layer = resolved;
                SetupList();
            }
            return true;
        }

        private void SetupList()
        {
            if (layer == null)
            {
                modifiersList = null;
                return;
            }

            layer.modifiers ??= new System.Collections.Generic.List<Material>();
            modifiersList = new ReorderableList(layer.modifiers, typeof(Material), true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Modifiers (Materials)"),
                drawElementCallback = DrawModifier,
                onAddCallback = AddModifier,
                onRemoveCallback = RemoveModifier,
                onReorderCallback = _ => compositor.MarkChanged()
            };
        }

        private void DrawModifier(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= layer.modifiers.Count)
                return;
            rect.height = EditorGUIUtility.singleLineHeight;
            layer.modifiers[index] = (Material)EditorGUI.ObjectField(
                rect,
                layer.modifiers[index],
                typeof(Material),
                false);
        }

        private void AddModifier(ReorderableList list)
        {
            Undo.RecordObject(compositor, "Add Layer Modifier");
            layer.modifiers.Add(null);
            list.index = layer.modifiers.Count - 1;
            compositor.MarkChanged();
        }

        private void RemoveModifier(ReorderableList list)
        {
            if (list.index < 0 || list.index >= layer.modifiers.Count)
                return;
            Undo.RecordObject(compositor, "Remove Layer Modifier");
            layer.modifiers.RemoveAt(list.index);
            list.index = Mathf.Min(list.index, layer.modifiers.Count - 1);
            compositor.MarkChanged();
        }

        private void OnCompositorChanged(TextureCompositor changedCompositor)
        {
            if (changedCompositor == compositor)
                Repaint();
        }
    }
}
