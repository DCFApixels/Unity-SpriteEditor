using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public enum BlendMode
    {
        Normal = 0,
        Multiply = 1,
        Overwrite = 2
    }

    public enum DistanceMetric
    {
        EuclideanExact = 0,
        EuclideanApproximate = 1,
        Manhattan = 2,
        Chebyshev = 3
    }

    public enum EffectInputMode
    {
        Previous = 0,
        Specific = 1
    }

    [Serializable]
    public struct TextureTransform
    {
        public static readonly TextureTransform Default = new TextureTransform
        {
            pivot = new Vector2(0.5f, 0.5f),
            position = Vector2.zero,
            scale = Vector2.one,
            rotation = 0f
        };

        public Vector2 pivot;
        public Vector2 position;
        public Vector2 scale;
        public float rotation;

        public bool IsIdentity()
        {
            return position == Vector2.zero && scale == Vector2.one && Mathf.Approximately(rotation, 0f);
        }

        public void Reset()
        {
            this = Default;
        }
    }

    public static class GradientUtility
    {
        private static readonly GradientAlphaKey[] OpaqueAlphaKeys =
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };

        public static readonly Gradient WhiteToBlack = Create(new[]
        {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.black, 1f)
        });

        public static Gradient Create(Gradient gradient)
        {
            Gradient result = new Gradient();
            if (gradient != null)
                result.SetKeys(gradient.colorKeys, gradient.alphaKeys);
            return result;
        }

        public static Gradient Create(GradientColorKey[] colorKeys)
        {
            return Create(colorKeys, OpaqueAlphaKeys);
        }

        public static Gradient Create(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys)
        {
            Gradient result = new Gradient();
            result.SetKeys(colorKeys, alphaKeys);
            return result;
        }

        public static bool IsTwoColorGradient(Gradient gradient, out Color left, out Color right)
        {
            left = default;
            right = default;
            if (gradient == null)
                return false;

            GradientColorKey[] colors = gradient.colorKeys;
            GradientAlphaKey[] alphas = gradient.alphaKeys;
            if (colors.Length == 0 || alphas.Length == 0 || colors.Length > 2 || alphas.Length > 2)
                return false;

            if (colors.Length == 1)
            {
                left = right = colors[0].color;
            }
            else if (Mathf.Approximately(colors[0].time, 0f) && Mathf.Approximately(colors[1].time, 1f))
            {
                left = colors[0].color;
                right = colors[1].color;
            }
            else
            {
                return false;
            }

            if (alphas.Length == 1)
            {
                left.a = right.a = alphas[0].alpha;
            }
            else if (Mathf.Approximately(alphas[0].time, 0f) && Mathf.Approximately(alphas[1].time, 1f))
            {
                left.a = alphas[0].alpha;
                right.a = alphas[1].alpha;
            }
            else
            {
                return false;
            }

            return true;
        }

        public static int ComputeHash(Gradient gradient)
        {
            if (gradient == null)
                return 0;

            unchecked
            {
                int hash = 17;
                GradientColorKey[] colors = gradient.colorKeys;
                GradientAlphaKey[] alphas = gradient.alphaKeys;
                hash = hash * 31 + colors.Length;
                for (int i = 0; i < colors.Length; i++)
                {
                    hash = hash * 31 + colors[i].color.GetHashCode();
                    hash = hash * 31 + colors[i].time.GetHashCode();
                }

                hash = hash * 31 + alphas.Length;
                for (int i = 0; i < alphas.Length; i++)
                {
                    hash = hash * 31 + alphas[i].alpha.GetHashCode();
                    hash = hash * 31 + alphas[i].time.GetHashCode();
                }

                return hash;
            }
        }
    }

    public static class SEGUI
    {
        private static readonly GUIContent PivotLabel = new GUIContent("Pivot", "Normalized pivot inside the output canvas.");
        private static readonly GUIContent PositionLabel = new GUIContent("Position (px)", "Offset in output pixels. Positive X moves right; positive Y moves up.");
        private static readonly GUIContent ScaleLabel = new GUIContent("Scale", "Visual scale. One means 100 percent; negative values flip the image.");
        private static readonly GUIContent RotationLabel = new GUIContent("Rotation", "Clockwise visual rotation in degrees.");

        public static void DrawTextureTransform(ref TextureTransform transform)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
                    if (GUILayout.Button("Reset", GUILayout.Width(54f)))
                    {
                        transform.Reset();
                        GUI.changed = true;
                    }
                }

                int previousIndent = EditorGUI.indentLevel;
                float previousLabelWidth = EditorGUIUtility.labelWidth;
                try
                {
                    EditorGUI.indentLevel++;
                    EditorGUIUtility.labelWidth = 92f;
                    transform.pivot = EditorGUILayout.Vector2Field(PivotLabel, transform.pivot);
                    transform.position = EditorGUILayout.Vector2Field(PositionLabel, transform.position);
                    transform.scale = EditorGUILayout.Vector2Field(ScaleLabel, transform.scale);
                    transform.rotation = EditorGUILayout.FloatField(RotationLabel, transform.rotation);
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndent;
                    EditorGUIUtility.labelWidth = previousLabelWidth;
                }
            }
        }
    }

    [InitializeOnLoad]
    internal static class SpriteEditorMaterials
    {
        private static Material blendMaterial;
        private static Material transformMaterial;

        static SpriteEditorMaterials()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
        }

        public static Material Blend => GetOrCreate(ref blendMaterial, "Hidden/TextureCompositor/Blend");
        public static Material Transform => GetOrCreate(ref transformMaterial, "Hidden/TextureCompositor/Transform");

        private static Material GetOrCreate(ref Material material, string shaderName)
        {
            if (material != null)
                return material;

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"SpriteEditor shader '{shaderName}' was not found.");
                return null;
            }

            material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return material;
        }

        private static void Dispose()
        {
            if (blendMaterial != null)
                UnityEngine.Object.DestroyImmediate(blendMaterial);
            if (transformMaterial != null)
                UnityEngine.Object.DestroyImmediate(transformMaterial);
            blendMaterial = null;
            transformMaterial = null;
        }
    }

    public abstract class LayerEditorWindowBase : EditorWindow
    {
        private const int PreviewMaxSize = 256;
        private const double PreviewDelay = 0.12d;

        [SerializeField] private TextureCompositor compositor;
        [SerializeField] private string layerId;

        [NonSerialized] private Layer currentLayer;
        [NonSerialized] private Texture2D previewTexture;
        [NonSerialized] private bool previewRequested;
        [NonSerialized] private double previewAt;
        [NonSerialized] private string[] effectTargetIds;
        [NonSerialized] private string[] effectTargetLabels;
        [NonSerialized] private string effectTargetOptionsForLayerId;
        [NonSerialized] private string effectTargetOptionsForTargetId;

        protected Layer CurrentLayer => currentLayer;
        protected TextureCompositor Compositor => compositor;
        protected virtual string PreviewTitle => "Preview";
        protected abstract Type EditedLayerType { get; }

        protected void Initialize(Layer layer, TextureCompositor owner)
        {
            currentLayer = layer;
            compositor = owner;
            layerId = layer?.Id;
            minSize = new Vector2(320f, 430f);
            InvalidateEffectTargetOptions();
            RequestPreview(true);
        }

        protected virtual void OnEnable()
        {
            TextureCompositor.Changed += OnCompositorChanged;
            RequestPreview(true);
        }

        protected virtual void OnDisable()
        {
            TextureCompositor.Changed -= OnCompositorChanged;
            ReleasePreview();
        }

        protected virtual void Update()
        {
            if (!previewRequested || EditorApplication.timeSinceStartup < previewAt)
                return;

            previewRequested = false;
            UpdatePreview();
        }

        protected virtual void OnGUI()
        {
            if (!ResolveLayer())
            {
                EditorGUILayout.HelpBox("The edited layer no longer exists in this compositor.", MessageType.Info);
                if (GUILayout.Button("Close"))
                    Close();
                return;
            }

            Undo.RecordObject(compositor, "Edit Sprite Layer");
            EditorGUI.BeginChangeCheck();
            DrawSettings(currentLayer);
            if (EditorGUI.EndChangeCheck())
            {
                compositor.MarkChanged();
                RequestPreview();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(PreviewTitle, EditorStyles.boldLabel);
            DrawPreview();

            if (GUILayout.Button("Close"))
                Close();
        }

        protected abstract void DrawSettings(Layer layer);

        protected void DrawEffectTarget(TargetedLayerEffect effect)
        {
            effect.inputMode = (EffectInputMode)EditorGUILayout.EnumPopup("Input", effect.inputMode);
            if (effect.inputMode == EffectInputMode.Previous)
            {
                EditorGUILayout.HelpBox(
                    "Uses the item directly below this effect. A group is read as the combined alpha of all visible descendants.",
                    MessageType.Info);
                return;
            }

            EnsureEffectTargetOptions(effect);
            int selectedIndex = FindEffectTargetIndex(effect.TargetLayerId);
            int nextIndex = EditorGUILayout.Popup("Target", selectedIndex, effectTargetLabels);
            if (nextIndex != selectedIndex)
            {
                effect.TargetLayerId = effectTargetIds[nextIndex];
                InvalidateEffectTargetOptions();
                GUI.changed = true;
            }

            if (string.IsNullOrEmpty(effect.TargetLayerId))
            {
                EditorGUILayout.HelpBox("Select a source layer or group for this effect.", MessageType.Warning);
                return;
            }

            if (!compositor.IsUsableEffectTarget(effect, effect.TargetLayerId))
            {
                EditorGUILayout.HelpBox(
                    "The selected target is missing or would create a cyclic effect dependency.",
                    MessageType.Error);
                return;
            }

            if (compositor.FindLayer(effect.TargetLayerId) is GroupLayer)
            {
                EditorGUILayout.HelpBox(
                    "The selected group is read as the combined alpha of all visible descendant layers.",
                    MessageType.Info);
            }
        }

        protected void RequestPreview(bool immediate = false)
        {
            previewRequested = true;
            previewAt = EditorApplication.timeSinceStartup + (immediate ? 0d : PreviewDelay);
            Repaint();
        }

        private bool ResolveLayer()
        {
            if (compositor == null || string.IsNullOrEmpty(layerId))
                return false;

            if (currentLayer == null || currentLayer.Id != layerId)
                currentLayer = compositor.FindLayer(layerId);

            return currentLayer != null && EditedLayerType.IsInstanceOfType(currentLayer);
        }

        private void EnsureEffectTargetOptions(TargetedLayerEffect effect)
        {
            if (effectTargetIds != null &&
                effectTargetOptionsForLayerId == effect.Id &&
                effectTargetOptionsForTargetId == effect.TargetLayerId)
            {
                return;
            }

            List<string> candidateIds = new List<string>();
            List<string> candidateLabels = new List<string>();
            compositor.GetEffectTargetOptions(effect, candidateIds, candidateLabels);

            bool hasCurrentTarget = false;
            for (int i = 0; i < candidateIds.Count; i++)
            {
                if (candidateIds[i] == effect.TargetLayerId)
                {
                    hasCurrentTarget = true;
                    break;
                }
            }

            bool includeUnavailableTarget = !string.IsNullOrEmpty(effect.TargetLayerId) && !hasCurrentTarget;
            int firstCandidateIndex = includeUnavailableTarget ? 2 : 1;
            effectTargetIds = new string[candidateIds.Count + firstCandidateIndex];
            effectTargetLabels = new string[candidateLabels.Count + firstCandidateIndex];
            effectTargetIds[0] = string.Empty;
            effectTargetLabels[0] = "<Select layer or group>";

            if (includeUnavailableTarget)
            {
                effectTargetIds[1] = effect.TargetLayerId;
                effectTargetLabels[1] = compositor.FindLayer(effect.TargetLayerId) == null
                    ? "<Missing target>"
                    : "<Unavailable target: cyclic dependency>";
            }

            for (int i = 0; i < candidateIds.Count; i++)
            {
                effectTargetIds[firstCandidateIndex + i] = candidateIds[i];
                effectTargetLabels[firstCandidateIndex + i] = candidateLabels[i];
            }

            effectTargetOptionsForLayerId = effect.Id;
            effectTargetOptionsForTargetId = effect.TargetLayerId;
        }

        private int FindEffectTargetIndex(string targetId)
        {
            for (int i = 0; i < effectTargetIds.Length; i++)
            {
                if (effectTargetIds[i] == targetId)
                    return i;
            }
            return 0;
        }

        private void InvalidateEffectTargetOptions()
        {
            effectTargetIds = null;
            effectTargetLabels = null;
            effectTargetOptionsForLayerId = null;
            effectTargetOptionsForTargetId = null;
        }

        private void OnCompositorChanged(TextureCompositor changedCompositor)
        {
            if (changedCompositor != compositor)
                return;
            currentLayer = null;
            InvalidateEffectTargetOptions();
            RequestPreview();
        }

        private void UpdatePreview()
        {
            ReleasePreview();
            if (!ResolveLayer())
                return;

            RenderTexture rendered = compositor.RenderLayerPreview(currentLayer, PreviewMaxSize);
            if (rendered == null)
            {
                Repaint();
                return;
            }

            try
            {
                previewTexture = TextureCompositor.CopyToTexture2D(rendered);
                previewTexture.hideFlags = HideFlags.HideAndDontSave;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rendered);
            }
            Repaint();
        }

        private void DrawPreview()
        {
            float aspect = previewTexture != null && previewTexture.height > 0
                ? (float)previewTexture.width / previewTexture.height
                : 1f;
            float availableWidth = Mathf.Max(64f, EditorGUIUtility.currentViewWidth - 36f);
            float drawWidth = Mathf.Min(PreviewMaxSize, availableWidth);
            float drawHeight = Mathf.Clamp(drawWidth / Mathf.Max(0.01f, aspect), 64f, PreviewMaxSize);
            Rect rect = EditorGUILayout.GetControlRect(false, drawHeight);
            if (previewTexture != null)
                EditorGUI.DrawPreviewTexture(rect, previewTexture, null, ScaleMode.ScaleToFit);
            else
                EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f, 1f));
        }

        private void ReleasePreview()
        {
            if (previewTexture == null)
                return;
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }
}
