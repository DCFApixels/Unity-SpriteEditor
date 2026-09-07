using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public enum BlendMode
    {
        Normal = 0,
        Multiply = 1,
        Overwrite = 2,
        None = 3,
        Add = 4,
        Subtract = 5,
        Divide = 6,
        Screen = 7,
        Overlay = 8,
        Darken = 9,
        Lighten = 10,
        [InspectorName("Color Dodge")] Dodge = 11,
        [InspectorName("Color Burn")] Burn = 12,
        [InspectorName("Linear Dodge (Add)")] LinearDodge = 13,
        [InspectorName("Linear Burn")] LinearBurn = 14,
        [InspectorName("Linear Light")] LinearLight = 15,
        [InspectorName("Linear Light Add/Sub")] LinearLightAddSub = 16,
        [InspectorName("Vivid Light")] VividLight = 17,
        [InspectorName("Pin Light")] PinLight = 18,
        [InspectorName("Hard Mix")] HardMix = 19,
        [InspectorName("Hard Light")] HardLight = 20,
        [InspectorName("Soft Light")] SoftLight = 21,
        Difference = 22,
        Exclusion = 23,
        Negation = 24
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

    public enum PaintToolMode
    {
        Brush = 0,
        Eraser = 1
    }

    public enum PaintRepeatMode
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        Grid = 3,
        Radial = 4
    }

    public enum PaintRepeatElementMode
    {
        Copy = 0,
        AlternateMirror = 1
    }

    public enum PaintRepeatBoundaryMode
    {
        Continue = 0,
        Clip = 1
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

    [InitializeOnLoad]
    internal static class SpriteEditorMaterials
    {
        private static Material blendMaterial;
        private static Material transformMaterial;
        private static Material paintBrushMaterial;
        private static Material alphaConversionMaterial;

        static SpriteEditorMaterials()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
        }

        public static Material Blend => GetOrCreate(ref blendMaterial, "Hidden/TextureCompositor/Blend");
        public static Material Transform => GetOrCreate(ref transformMaterial, "Hidden/TextureCompositor/Transform");
        public static Material PaintBrush => GetOrCreate(ref paintBrushMaterial, "Hidden/TextureCompositor/PaintBrush");
        public static Material AlphaConversion => GetOrCreate(
            ref alphaConversionMaterial,
            "Hidden/TextureCompositor/AlphaConversion");

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
            if (paintBrushMaterial != null)
                UnityEngine.Object.DestroyImmediate(paintBrushMaterial);
            if (alphaConversionMaterial != null)
                UnityEngine.Object.DestroyImmediate(alphaConversionMaterial);
            blendMaterial = null;
            transformMaterial = null;
            paintBrushMaterial = null;
            alphaConversionMaterial = null;
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
        [NonSerialized] private Image previewImage;
        [NonSerialized] private Label previewPlaceholder;
        [NonSerialized] private bool applyingChange;

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
            if (rootVisualElement != null && rootVisualElement.panel != null)
                RebuildInterface();
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

        public void CreateGUI()
        {
            RebuildInterface();
        }

        protected abstract void BuildSettings(VisualElement root, Layer layer);

        protected void ApplyLayerChange(string undoName, Action change)
        {
            if (compositor == null || change == null)
                return;

            Undo.RecordObject(compositor, undoName);
            applyingChange = true;
            try
            {
                change();
                compositor.NormalizeModel();
                compositor.MarkChanged();
            }
            finally
            {
                applyingChange = false;
            }
            RequestPreview();
        }

        protected void AddEffectTarget(VisualElement root, TargetedLayerEffect effect)
        {
            EnumField input = SpriteEditorUI.ConfigureField(new EnumField("Input", effect.inputMode));
            input.RegisterValueChangedCallback(evt =>
            {
                ApplyLayerChange("Change Effect Input", () => effect.inputMode = (EffectInputMode)evt.newValue);
                InvalidateEffectTargetOptions();
                RebuildInterface();
            });
            root.Add(input);

            if (effect.inputMode == EffectInputMode.Previous)
            {
                SpriteEditorUI.AddHelpBox(
                    root,
                    "Uses the item directly below this effect. A group is read as the combined alpha of all visible descendants.",
                    HelpBoxMessageType.Info);
                return;
            }

            EnsureEffectTargetOptions(effect);
            int selectedIndex = FindEffectTargetIndex(effect.TargetLayerId);
            PopupField<string> target = SpriteEditorUI.ConfigureField(
                new PopupField<string>("Target", new List<string>(effectTargetLabels), selectedIndex));
            target.RegisterValueChangedCallback(evt =>
            {
                int nextIndex = Array.IndexOf(effectTargetLabels, evt.newValue);
                if (nextIndex < 0 || nextIndex >= effectTargetIds.Length)
                    return;
                ApplyLayerChange("Change Effect Target", () => effect.TargetLayerId = effectTargetIds[nextIndex]);
                InvalidateEffectTargetOptions();
                RebuildInterface();
            });
            root.Add(target);

            if (string.IsNullOrEmpty(effect.TargetLayerId))
            {
                SpriteEditorUI.AddHelpBox(
                    root,
                    "Select a source layer or group for this effect.",
                    HelpBoxMessageType.Warning);
            }
            else if (!compositor.IsUsableEffectTarget(effect, effect.TargetLayerId))
            {
                SpriteEditorUI.AddHelpBox(
                    root,
                    "The selected target is missing or would create a cyclic effect dependency.",
                    HelpBoxMessageType.Error);
            }
            else if (compositor.FindLayer(effect.TargetLayerId) is GroupLayer)
            {
                SpriteEditorUI.AddHelpBox(
                    root,
                    "The selected group is read as the combined alpha of all visible descendant layers.",
                    HelpBoxMessageType.Info);
            }
        }

        protected void RequestPreview(bool immediate = false)
        {
            previewRequested = true;
            previewAt = EditorApplication.timeSinceStartup + (immediate ? 0d : PreviewDelay);
        }

        protected void RebuildInterface()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            root.style.paddingLeft = 8f;
            root.style.paddingRight = 8f;
            root.style.paddingTop = 8f;
            root.style.paddingBottom = 8f;

            if (!ResolveLayer())
            {
                SpriteEditorUI.AddHelpBox(
                    root,
                    "The edited layer no longer exists in this compositor.",
                    HelpBoxMessageType.Info);
                root.Add(SpriteEditorUI.CreateButton("Close", Close));
                return;
            }

            ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            BuildSettings(scroll, currentLayer);
            scroll.Add(SpriteEditorUI.CreateHeading(PreviewTitle));

            VisualElement preview = new VisualElement();
            preview.style.height = PreviewMaxSize;
            preview.style.minHeight = 96f;
            preview.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            preview.style.borderTopWidth = 1f;
            preview.style.borderRightWidth = 1f;
            preview.style.borderBottomWidth = 1f;
            preview.style.borderLeftWidth = 1f;
            preview.style.borderTopColor = new Color(0f, 0f, 0f, 0.4f);
            preview.style.borderRightColor = new Color(0f, 0f, 0f, 0.4f);
            preview.style.borderBottomColor = new Color(0f, 0f, 0f, 0.4f);
            preview.style.borderLeftColor = new Color(0f, 0f, 0f, 0.4f);

            previewImage = new Image
            {
                image = previewTexture,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            previewImage.style.position = Position.Absolute;
            previewImage.style.left = 0f;
            previewImage.style.right = 0f;
            previewImage.style.top = 0f;
            previewImage.style.bottom = 0f;
            preview.Add(previewImage);

            previewPlaceholder = new Label(previewTexture == null ? "Rendering preview…" : string.Empty);
            previewPlaceholder.style.unityTextAlign = TextAnchor.MiddleCenter;
            previewPlaceholder.style.position = Position.Absolute;
            previewPlaceholder.style.left = 0f;
            previewPlaceholder.style.right = 0f;
            previewPlaceholder.style.top = 0f;
            previewPlaceholder.style.bottom = 0f;
            previewPlaceholder.pickingMode = PickingMode.Ignore;
            preview.Add(previewPlaceholder);
            scroll.Add(preview);

            Button close = SpriteEditorUI.CreateButton("Close", Close);
            close.style.marginTop = 8f;
            scroll.Add(close);
            root.Add(scroll);
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
            if (changedCompositor != compositor || applyingChange)
                return;
            currentLayer = null;
            InvalidateEffectTargetOptions();
            RebuildInterface();
            RequestPreview();
        }

        private void UpdatePreview()
        {
            ReleasePreview();
            if (!ResolveLayer())
                return;

            RenderTexture rendered = compositor.RenderLayerPreview(currentLayer, PreviewMaxSize);
            if (rendered != null)
            {
                try
                {
                    previewTexture = TextureCompositor.CopyToTexture2D(rendered);
                    previewTexture.hideFlags = HideFlags.HideAndDontSave;
                }
                finally
                {
                    RenderTexture.ReleaseTemporary(rendered);
                }
            }

            if (previewImage != null)
                previewImage.image = previewTexture;
            if (previewPlaceholder != null)
                previewPlaceholder.text = previewTexture == null ? "Preview unavailable" : string.Empty;
        }

        private void ReleasePreview()
        {
            if (previewImage != null)
                previewImage.image = null;
            if (previewTexture == null)
                return;
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }
}
