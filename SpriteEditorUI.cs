using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    internal static class SpriteEditorUI
    {
        internal sealed class ValueBindings
        {
            private readonly List<Action<bool>> updates = new List<Action<bool>>();

            public void Clear() => updates.Clear();

            public void Add(Action update) => updates.Add(_ => update());

            public void Track<T>(BaseField<T> field, Func<T> read)
            {
                void Refresh(bool force)
                {
                    T value = read();
                    if (!EqualityComparer<T>.Default.Equals(field.value, value) &&
                        (force || !IsInteracting(field)))
                        field.SetValueWithoutNotify(value);
                }

                bool queued = false;
                void QueueRefresh()
                {
                    if (queued || field.panel == null)
                        return;
                    queued = true;
                    field.schedule.Execute(() =>
                    {
                        queued = false;
                        Refresh(false);
                    });
                }

                field.RegisterCallback<FocusOutEvent>(_ => QueueRefresh(), TrickleDown.TrickleDown);
                field.RegisterCallback<PointerUpEvent>(_ => QueueRefresh(), TrickleDown.TrickleDown);
                field.RegisterCallback<PointerCaptureOutEvent>(_ => QueueRefresh(), TrickleDown.TrickleDown);
                updates.Add(Refresh);
                Refresh(true);
            }

            public void Refresh(bool force = false)
            {
                for (int i = 0; i < updates.Count; i++)
                    updates[i](force);
            }
        }

        internal static bool IsInteracting(VisualElement element)
        {
            VisualElement focused = element.focusController?.focusedElement as VisualElement;
            if (focused != null && (focused == element || element.Contains(focused)))
                return true;

            return HasPointerCaptureWithin(element);
        }

        internal static bool HasPointerCaptureWithin(VisualElement element)
        {
            if (element.panel == null)
                return false;
            for (int pointer = 0; pointer < PointerId.maxPointers; pointer++)
            {
                VisualElement captured = PointerCaptureHelper.GetCapturingElement(element.panel, pointer) as VisualElement;
                if (captured != null && (captured == element || element.Contains(captured)))
                    return true;
            }
            return false;
        }

        public const float StandardLabelWidth = 132f;
        public static readonly Color PanelDark = new Color(0.105f, 0.105f, 0.105f, 1f);
        public static readonly Color PanelLight = new Color(0.65f, 0.65f, 0.65f, 1f);
        public static readonly Color ToolbarDark = new Color(0.16f, 0.16f, 0.16f, 1f);
        public static readonly Color ToolbarLight = new Color(0.78f, 0.78f, 0.78f, 1f);
        public static readonly Color SelectedDark = new Color(0.18f, 0.38f, 0.62f, 1f);
        public static readonly Color SelectedLight = new Color(0.38f, 0.62f, 0.88f, 1f);
        public static readonly Color RowDark = new Color(0.18f, 0.18f, 0.18f, 1f);
        public static readonly Color RowLight = new Color(0.82f, 0.82f, 0.82f, 1f);

        public static Color PanelColor => EditorGUIUtility.isProSkin ? PanelDark : PanelLight;
        public static Color ToolbarColor => EditorGUIUtility.isProSkin ? ToolbarDark : ToolbarLight;
        public static Color SelectedColor => EditorGUIUtility.isProSkin ? SelectedDark : SelectedLight;
        public static Color RowColor => EditorGUIUtility.isProSkin ? RowDark : RowLight;

        public static VisualElement CreateToolbar()
        {
            VisualElement result = CreateRow();
            result.style.minHeight = 24f;
            result.style.paddingLeft = 4f;
            result.style.paddingRight = 4f;
            result.style.backgroundColor = ToolbarColor;
            result.style.borderBottomWidth = 1f;
            result.style.borderBottomColor = new Color(0f, 0f, 0f, 0.3f);
            return result;
        }

        public static VisualElement CreateRow()
        {
            return new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };
        }

        public static VisualElement CreateCard(string title = null)
        {
            VisualElement card = new VisualElement();
            card.style.marginTop = 4f;
            card.style.marginBottom = 4f;
            card.style.paddingLeft = 6f;
            card.style.paddingRight = 6f;
            card.style.paddingTop = 5f;
            card.style.paddingBottom = 6f;
            card.style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.16f, 0.16f, 1f)
                : new Color(0.86f, 0.86f, 0.86f, 1f);
            card.style.borderTopLeftRadius = 3f;
            card.style.borderTopRightRadius = 3f;
            card.style.borderBottomLeftRadius = 3f;
            card.style.borderBottomRightRadius = 3f;

            if (!string.IsNullOrEmpty(title))
            {
                Label heading = new Label(title);
                heading.style.unityFontStyleAndWeight = FontStyle.Bold;
                heading.style.marginBottom = 4f;
                card.Add(heading);
            }

            return card;
        }

        public static Label CreateHeading(string text)
        {
            Label result = new Label(text);
            result.style.unityFontStyleAndWeight = FontStyle.Bold;
            result.style.marginTop = 4f;
            result.style.marginBottom = 3f;
            return result;
        }

        public static Button CreateButton(string text, Action clicked, float width = 0f)
        {
            Button result = new Button(clicked) { text = text };
            if (width > 0f)
                result.style.width = width;
            result.style.height = 20f;
            return result;
        }

        public static Button CreateToolbarButton(string text, Action clicked, float width = 0f)
        {
            Button result = CreateButton(text, clicked, width);
            result.style.marginTop = 1f;
            result.style.marginBottom = 1f;
            return result;
        }

        public static T ConfigureField<T>(T field, float labelWidth = StandardLabelWidth)
            where T : VisualElement
        {
            field.style.flexShrink = 0f;
            field.style.marginTop = 1f;
            field.style.marginBottom = 1f;

            Label label = field.Q<Label>(className: BaseField<string>.labelUssClassName);
            if (label != null)
            {
                label.style.minWidth = labelWidth;
                label.style.width = labelWidth;
            }
            return field;
        }

        public static HelpBox AddHelpBox(VisualElement parent, string message, HelpBoxMessageType type)
        {
            HelpBox result = new HelpBox(message, type);
            result.style.marginTop = 4f;
            result.style.marginBottom = 4f;
            parent.Add(result);
            return result;
        }

        public static void AddTextureTransform(
            VisualElement parent,
            Func<TextureTransform> read,
            Action<TextureTransform> write,
            Action<string, Action> applyChange,
            ValueBindings bindings)
        {
            VisualElement card = CreateCard();
            VisualElement header = CreateRow();
            Label title = new Label("Transform");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.flexGrow = 1f;
            header.Add(title);
            card.Add(header);

            Vector2Field pivot = ConfigureField(new Vector2Field("Pivot"));
            pivot.tooltip = "Normalized pivot inside the output canvas.";
            Vector2Field position = ConfigureField(new Vector2Field("Position (px)"));
            position.tooltip = "Offset in output pixels. Positive X moves right; positive Y moves up.";
            Vector2Field scale = ConfigureField(new Vector2Field("Scale"));
            scale.tooltip = "Visual scale. One means 100 percent; negative values flip the image.";
            FloatField rotation = ConfigureField(new FloatField("Rotation"));
            rotation.tooltip = "Clockwise visual rotation in degrees.";

            bindings.Track(pivot, () => read().pivot);
            bindings.Track(position, () => read().position);
            bindings.Track(scale, () => read().scale);
            bindings.Track(rotation, () => read().rotation);

            Button reset = CreateButton("Reset", () =>
            {
                applyChange("Reset Layer Transform", () =>
                {
                    TextureTransform value = read();
                    value.Reset();
                    write(value);
                });
                bindings.Refresh(true);
            }, 54f);
            header.Add(reset);

            pivot.RegisterValueChangedCallback(evt =>
            {
                applyChange("Change Layer Transform", () =>
                {
                    TextureTransform value = read();
                    value.pivot = evt.newValue;
                    write(value);
                });
            });
            position.RegisterValueChangedCallback(evt =>
            {
                applyChange("Change Layer Transform", () =>
                {
                    TextureTransform value = read();
                    value.position = evt.newValue;
                    write(value);
                });
            });
            scale.RegisterValueChangedCallback(evt =>
            {
                applyChange("Change Layer Transform", () =>
                {
                    TextureTransform value = read();
                    value.scale = evt.newValue;
                    write(value);
                });
            });
            rotation.RegisterValueChangedCallback(evt =>
            {
                applyChange("Change Layer Transform", () =>
                {
                    TextureTransform value = read();
                    value.rotation = evt.newValue;
                    write(value);
                });
            });

            card.Add(pivot);
            card.Add(position);
            card.Add(scale);
            card.Add(rotation);
            parent.Add(card);
        }
    }
}
