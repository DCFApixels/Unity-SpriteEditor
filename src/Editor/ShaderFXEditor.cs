using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    [CustomEditor(typeof(ShaderFX))]
    public sealed class ShaderFXEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return BuildView((ShaderFX)target, serializedObject);
        }

        internal static VisualElement CreateInlineView(ShaderFX effect)
        {
            VisualElement host = new VisualElement();
            SerializedObject data = null;
            host.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                if (evt.target != host || data != null || effect == null)
                    return;
                data = new SerializedObject(effect);
                host.Add(BuildView(effect, data));
            });
            host.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                if (evt.target != host)
                    return;
                host.Unbind();
                host.Clear();
                data?.Dispose();
                data = null;
            });
            return host;
        }

        private static VisualElement BuildView(ShaderFX effect, SerializedObject serializedObject)
        {
            VisualElement root = new VisualElement { focusable = true };
            SpriteEditorUI.ApplyWindowStyles(root);
            root.AddToClassList("sprite-editor-shader-fx");
            root.Add(new HelpBox(
                "Implement float4 ApplyFX(float2 uv, float4 color). SampleInput(uv) reads the incoming layer. " +
                "Use #include with Assets/Packages paths, or paths relative to the containing asset (Assets before the first save). " +
                "UnityCG.cginc is already included.", HelpBoxMessageType.Info));

            Label heading = new Label("HLSL Code");
            heading.AddToClassList("sprite-editor-shader-fx-heading");
            root.Add(heading);
            ShaderFXCodeField code = new ShaderFXCodeField(effect);
            root.Add(code);

            HelpBox status = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            root.Add(status);
            Button apply = new Button { text = "Apply", tooltip = "Compile the code and parameter declarations; re-read included libraries. Embedded FX save with the document." };
            root.Add(apply);

            TextField diagnostics = new TextField
            {
                name = "shaderFXDiagnostics",
                multiline = true,
                isReadOnly = true,
                verticalScrollerVisibility = ScrollerVisibility.Auto
            };
            diagnostics.AddToClassList("sprite-editor-shader-fx-diagnostics");
            root.Add(diagnostics);
            apply.clicked += () =>
            {
                if (SpriteEditorApi.IsShaderFXContentLocked(effect)) return;
                root.Focus();
                serializedObject.ApplyModifiedProperties();
                effect.Apply();
                serializedObject.Update();
                RefreshStatus();
            };
            root.Add(new HelpBox(
                "Parameter values are live and shared by all layers using this asset. Adding, renaming or changing " +
                "a parameter type requires Apply. Do not redeclare generated parameter uniforms in the code.", HelpBoxMessageType.Info));
            root.Add(new PropertyField(serializedObject.FindProperty("parameters"), "Parameters"));
            Foldout reference = new Foldout { text = "Shader inputs", value = false };
            reference.Add(new HelpBox(
                "uv: normalized coordinates; color: straight RGBA at uv.\n" +
                "SampleInput(uv), _MainTex and _MainTex_TexelSize: incoming texture.\n" +
                "_InputSize: render width, height, 1/width, 1/height.\n" +
                "_CanvasSize: full-resolution canvas size in the same format.\n" +
                "_PreviewScale: full-size pixels per preview pixel (1 for export).\n" +
                "Texture parameters: sampler2D with <name>_TexelSize; sample with tex2D(name, uv).\n" +
                "Return straight RGBA. The layer's blend mode and opacity are applied afterwards.", HelpBoxMessageType.Info));
            root.Add(reference);

            void RefreshStatus()
            {
                if (effect == null)
                    return;
                code.SyncFromModel();
                status.messageType = effect.LastApplyFailed ? HelpBoxMessageType.Error : HelpBoxMessageType.Info;
                status.text = effect.LastApplyFailed
                    ? (effect.HasAppliedShader ? "Apply failed. The last successfully applied effect is still in use." : "Apply failed. This FX is skipped until it compiles successfully.")
                    : effect.HasPendingChanges ? "Unapplied code or parameter declarations. Click Apply when ready."
                    : "Applied. Values update without recompiling. Click Apply again after editing an included library.";
                diagnostics.SetValueWithoutNotify(effect.Diagnostics);
                apply.SetEnabled(effect != null);
            }

            root.TrackSerializedObjectValue(serializedObject, _ => RefreshStatus());
            root.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
            {
                if (evt.changedProperty != null && evt.changedProperty.propertyPath.StartsWith("parameters", System.StringComparison.Ordinal))
                    effect.NotifyValuesChanged();
            });
            root.Bind(serializedObject);
            void RefreshLock() => root.SetEnabled(!SpriteEditorApi.IsShaderFXContentLocked(effect));
            root.RegisterCallback<AttachToPanelEvent>(_ => { SpriteEditorApi.LiveEditLocksChanged -= RefreshLock; SpriteEditorApi.LiveEditLocksChanged += RefreshLock; RefreshLock(); });
            root.RegisterCallback<DetachFromPanelEvent>(_ => SpriteEditorApi.LiveEditLocksChanged -= RefreshLock);
            RefreshLock();
            RefreshStatus();
            return root;
        }
    }

    [CustomPropertyDrawer(typeof(ShaderFXParameter))]
    public sealed class ShaderFXParameterDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new VisualElement();
            SpriteEditorUI.ApplyWindowStyles(root);
            root.AddToClassList("sprite-editor-shader-fx-parameter");
            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ShaderFXParameter.name)), "Name"));
            SerializedProperty type = property.FindPropertyRelative(nameof(ShaderFXParameter.type));
            root.Add(new PropertyField(type, "Type"));
            string[] valueNames =
            {
                nameof(ShaderFXParameter.floatValue), nameof(ShaderFXParameter.colorValue),
                nameof(ShaderFXParameter.vectorValue), nameof(ShaderFXParameter.textureValue)
            };
            VisualElement[] fields = new VisualElement[valueNames.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                SerializedProperty value = property.FindPropertyRelative(valueNames[i]);
                if (valueNames[i] == nameof(ShaderFXParameter.colorValue))
                {
                    ColorField color = SpriteEditorColorInputs.Bind(new ColorField("Value"), value,
                        () => ((ShaderFX)value.serializedObject.targetObject).NotifyValuesChanged());
                    color.AddToClassList(BaseField<UnityEngine.Color>.alignedFieldUssClassName);
                    fields[i] = color;
                }
                else
                    fields[i] = new PropertyField(value, "Value");
                root.Add(fields[i]);
            }
            void RefreshType(SerializedProperty current)
            {
                for (int i = 0; i < fields.Length; i++)
                    fields[i].EnableInClassList("sprite-editor-shader-fx-hidden", i != current.enumValueIndex);
            }
            root.TrackPropertyValue(type, RefreshType);
            RefreshType(type);
            return root;
        }
    }
}
