using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [CreateAssetMenu(fileName = "TextureCompositor", menuName = "Sprite Editor/Texture Compositor")]
    public sealed partial class TextureCompositor : ScriptableObject
    {
        private const int MinimumOutputSize = 1;
        private const int MaximumOutputSize = 16384;
        private const int AlphaUnionMode = 100;

        public int width = 512;
        public int height = 512;
        [SerializeReference] public List<Layer> layers = new List<Layer>();
        [SerializeField, HideInInspector] private int nextGroupNumber = 1;
        [SerializeField, HideInInspector] private List<ShaderFX> embeddedShaderFX = new List<ShaderFX>();

        internal static event Action<TextureCompositor> Changed;

        internal static void NotifyShaderFXChanged(ShaderFX effect)
        {
            // Only loaded documents need repainting; never load or rebake saved assets here.
            foreach (TextureCompositor document in Resources.FindObjectsOfTypeAll<TextureCompositor>())
                if (ContainsShaderFX(document.layers, effect))
                    Changed?.Invoke(document);
        }

        private static bool ContainsShaderFX(List<Layer> source, ShaderFX effect)
        {
            if (source == null)
                return false;
            foreach (Layer layer in source)
            {
                if (layer?.modifiers != null && layer.modifiers.Contains(effect))
                    return true;
                if (layer is GroupLayer group && ContainsShaderFX(group.layers, effect))
                    return true;
            }
            return false;
        }

        private void OnEnable()
        {
            NormalizeModel();
        }

        private void OnValidate()
        {
            NormalizeModel();
        }

        private void OnDisable()
        {
            ReleaseLayerResources(layers);
        }

        private void OnDestroy()
        {
            foreach (ShaderFX effect in embeddedShaderFX)
                if (effect != null && effect.EmbeddedOwner == this && !AssetDatabase.Contains(effect))
                    DestroyImmediate(effect);
        }

        internal ShaderFX AddEmbeddedShaderFX(Layer layer)
        {
            ShaderFX effect = ShaderFX.CreateEmbedded(this);
            Undo.RecordObject(this, "Add Shader FX");
            embeddedShaderFX.Add(effect);
            layer.modifiers.Add(effect);
            return effect;
        }

        internal void EmbedShaderFX(Layer layer, int index)
        {
            if (!(layer.modifiers[index] is ShaderFX source))
                return;
            ShaderFX copy = source.CloneForDocument(this);
            Undo.RegisterCreatedObjectUndo(copy, "Embed Shader FX");
            Undo.RecordObject(this, "Embed Shader FX");
            embeddedShaderFX.Add(copy);
            layer.modifiers[index] = copy;
        }

        internal void PersistEmbeddedShaderFX()
        {
            foreach (ShaderFX effect in embeddedShaderFX)
                if (effect != null)
                    effect.PersistEmbedded(this);
        }

        internal void CloneEmbeddedShaderFX()
        {
            embeddedShaderFX = new List<ShaderFX>();
            Dictionary<ShaderFX, ShaderFX> copies = new Dictionary<ShaderFX, ShaderFX>();
            CloneIn(layers);
            void CloneIn(List<Layer> source)
            {
                if (source == null)
                    return;
                foreach (Layer layer in source)
                {
                    if (layer == null)
                        continue;
                    if (layer.modifiers != null)
                        for (int i = 0; i < layer.modifiers.Count; i++)
                            if (layer.modifiers[i] is ShaderFX effect && effect.EmbeddedOwner != null)
                            {
                                if (!copies.TryGetValue(effect, out ShaderFX copy))
                                {
                                    copy = effect.CloneForDocument(this);
                                    copies.Add(effect, copy);
                                    embeddedShaderFX.Add(copy);
                                }
                                layer.modifiers[i] = copy;
                            }
                    if (layer is GroupLayer group)
                        CloneIn(group.layers);
                }
            }
        }

        public Texture2D Compose()
        {
            NormalizeModel();
            return ComposeAtSize(width, height, 1f);
        }

        internal Texture2D ComposePreview(int maxSize)
        {
            GetPreviewDimensions(maxSize, out int previewWidth, out int previewHeight, out float scaleMultiplier);
            return ComposeAtSize(previewWidth, previewHeight, scaleMultiplier);
        }

        internal RenderTexture RenderLayerPreview(Layer layer, int maxSize)
        {
            if (layer == null || !TryFindLayer(layer, out List<Layer> container, out int index))
                return null;

            GetPreviewDimensions(maxSize, out int previewWidth, out int previewHeight, out float scaleMultiplier);
            return RenderStandalone(
                container,
                index,
                previewWidth,
                previewHeight,
                scaleMultiplier,
                new HashSet<Layer>());
        }

        internal Layer FindLayer(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            return FindLayerRecursive(layers, id);
        }

        internal Texture2D RasterizeLayer(Layer layer, bool applyTransform)
        {
            if (layer == null || !TryFindLayer(layer, out List<Layer> container, out int index))
                throw new InvalidOperationException("The layer no longer belongs to this composition.");

            RenderTexture rendered = null;
            try
            {
                if (layer is GroupLayer group)
                {
                    rendered = GetClearRenderTexture(width, height);
                    CompositeLayers(group.layers, ref rendered, width, height, 1f, new HashSet<Layer>());
                }
                else
                {
                    rendered = RenderStandalone(container, index, width, height, 1f, new HashSet<Layer>(),
                        applyTransform: applyTransform, applyModifiers: false, includeDisabled: true);
                    if (rendered == null)
                        rendered = GetClearRenderTexture(width, height);
                }
                return CopyToTexture2D(rendered);
            }
            finally
            {
                if (rendered != null)
                    RenderTexture.ReleaseTemporary(rendered);
            }
        }

        internal bool TryFindLayer(Layer target, out List<Layer> container, out int index)
        {
            return TryFindLayerRecursive(layers, target, out container, out index);
        }

        internal bool TryFindParentGroup(List<Layer> childList, out GroupLayer parent, out List<Layer> parentContainer, out int parentIndex)
        {
            return TryFindParentGroupRecursive(layers, childList, out parent, out parentContainer, out parentIndex);
        }

        internal void GetEffectTargetOptions(
            TargetedLayerEffect consumer,
            List<string> targetIds,
            List<string> labels)
        {
            if (targetIds == null)
                throw new ArgumentNullException(nameof(targetIds));
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));

            targetIds.Clear();
            labels.Clear();
            if (consumer == null)
                return;

            CollectEffectTargetOptions(layers, consumer, 0, targetIds, labels);
        }

        internal bool IsUsableEffectTarget(TargetedLayerEffect consumer, string targetId)
        {
            Layer target = FindLayer(targetId);
            return target != null && !LayerDependsOn(target, consumer, new HashSet<Layer>());
        }

        internal bool HasUsableEffectInput(
            TargetedLayerEffect effect,
            List<Layer> container,
            int index)
        {
            if (effect == null)
                return false;
            if (effect.inputMode == EffectInputMode.Specific)
                return IsUsableEffectTarget(effect, effect.TargetLayerId);
            return container != null && index + 1 < container.Count && container[index + 1] != null;
        }

        internal void NormalizeModel()
        {
            width = Mathf.Clamp(width, MinimumOutputSize, MaximumOutputSize);
            height = Mathf.Clamp(height, MinimumOutputSize, MaximumOutputSize);
            layers ??= new List<Layer>();
            HashSet<string> usedIds = new HashSet<string>();
            NormalizeLayers(layers, usedIds);
            SynchronizeNextAutomaticNumbers();
        }

        internal void MarkChanged()
        {
            NormalizeModel();
            if (AssetDatabase.Contains(this))
            {
                PersistEmbeddedShaderFX();
                PersistDrawingLayerTextures();
                EditorUtility.SetDirty(this);
            }
            Changed?.Invoke(this);
        }

        internal void SyncDrawingLayerTextures()
        {
            VisitDrawingLayers(layers, drawing => drawing.SyncSurfaceToTexture());
        }

        internal void CloneDrawingLayerTextures()
        {
            VisitDrawingLayers(layers, drawing => drawing.CloneStoredTexture());
        }

        internal void PersistDrawingLayerTextures(bool reimport = true)
        {
            if (!AssetDatabase.Contains(this))
                return;

            bool addedTexture = false;
            VisitDrawingLayers(layers, drawing => addedTexture |= drawing.MakeTexturePersistent(this));
            if (!addedTexture || !reimport)
                return;

            string assetPath = AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        internal void InvalidateDrawingLayerSurfaces()
        {
            VisitDrawingLayers(layers, drawing => drawing.InvalidatePaintSurface());
        }

        internal void DestroyLayerAssets(Layer layer, bool undoTransient = false)
        {
            if (layer is DrawingLayer drawing)
                drawing.DestroyStoredTextureWithUndo(undoTransient);
            if (!(layer is GroupLayer group) || group.layers == null)
                return;
            for (int i = 0; i < group.layers.Count; i++)
                DestroyLayerAssets(group.layers[i], undoTransient);
        }

        internal static Texture2D CopyToTexture2D(RenderTexture source)
        {
            if (source == null)
                return null;

            Texture2D texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                texture.Apply(false, false);
                return texture;
            }
            catch
            {
                DestroyImmediate(texture);
                throw;
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private Texture2D ComposeAtSize(int outputWidth, int outputHeight, float scaleMultiplier)
        {
            RenderTexture composite = RenderComposite(outputWidth, outputHeight, scaleMultiplier);
            try
            {
                return CopyToTexture2D(composite);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(composite);
            }
        }

        private RenderTexture RenderComposite(int outputWidth, int outputHeight, float scaleMultiplier)
        {
            RenderTexture accumulator = GetClearRenderTexture(outputWidth, outputHeight);
            CompositeLayers(
                layers,
                ref accumulator,
                outputWidth,
                outputHeight,
                scaleMultiplier,
                new HashSet<Layer>());
            return accumulator;
        }

        private void CompositeLayers(
            List<Layer> sourceLayers,
            ref RenderTexture accumulator,
            int outputWidth,
            int outputHeight,
            float scaleMultiplier,
            HashSet<Layer> renderStack)
        {
            if (sourceLayers == null)
                return;

            // Data and UI are ordered top-to-bottom. Rendering therefore walks backwards.
            for (int i = sourceLayers.Count - 1; i >= 0; i--)
            {
                Layer layer = sourceLayers[i];
                if (layer == null || !layer.enabled)
                    continue;

                if (layer is GroupLayer group)
                {
                    // A group is deliberately not composited into an intermediate color target.
                    CompositeLayers(
                        group.layers,
                        ref accumulator,
                        outputWidth,
                        outputHeight,
                        scaleMultiplier,
                        renderStack);
                    continue;
                }

                RenderTexture rendered = RenderStandalone(
                    sourceLayers,
                    i,
                    outputWidth,
                    outputHeight,
                    scaleMultiplier,
                    renderStack);
                if (rendered == null)
                    continue;

                try
                {
                    BlendInto(ref accumulator, rendered, layer.blendMode, layer.opacity);
                }
                finally
                {
                    RenderTexture.ReleaseTemporary(rendered);
                }
            }
        }

        private RenderTexture RenderStandalone(
            List<Layer> container,
            int index,
            int outputWidth,
            int outputHeight,
            float scaleMultiplier,
            HashSet<Layer> renderStack,
            bool applyTransform = true,
            bool applyModifiers = true,
            bool includeDisabled = false)
        {
            if (container == null || index < 0 || index >= container.Count)
                return null;

            Layer layer = container[index];
            if (layer == null || (!includeDisabled && !layer.enabled))
                return null;
            if (layer is GroupLayer group)
                return RenderGroupAlpha(group, outputWidth, outputHeight, scaleMultiplier, renderStack);

            renderStack ??= new HashSet<Layer>();
            if (!renderStack.Add(layer))
                return null;

            RenderTexture input = null;
            try
            {
                if (layer is TargetedLayerEffect effect)
                {
                    input = RenderEffectInput(
                        effect,
                        container,
                        index,
                        outputWidth,
                        outputHeight,
                        scaleMultiplier,
                        renderStack);
                }

                LayerRenderContext context = new LayerRenderContext(
                    this,
                    input,
                    outputWidth,
                    outputHeight,
                    scaleMultiplier,
                    applyTransform,
                    applyModifiers);
                return layer.Render(context);
            }
            finally
            {
                if (input != null)
                    RenderTexture.ReleaseTemporary(input);
                renderStack.Remove(layer);
            }
        }

        private RenderTexture RenderEffectInput(
            TargetedLayerEffect effect,
            List<Layer> container,
            int index,
            int outputWidth,
            int outputHeight,
            float scaleMultiplier,
            HashSet<Layer> renderStack)
        {
            if (effect.inputMode != EffectInputMode.Specific)
            {
                return RenderPreviousInput(
                    container,
                    index,
                    outputWidth,
                    outputHeight,
                    scaleMultiplier,
                    renderStack);
            }

            Layer target = FindLayer(effect.TargetLayerId);
            if (target == null ||
                !target.enabled ||
                !IsUsableEffectTarget(effect, effect.TargetLayerId))
                return null;

            if (target is GroupLayer group)
                return RenderGroupAlpha(group, outputWidth, outputHeight, scaleMultiplier, renderStack);
            if (!TryFindLayer(target, out List<Layer> targetContainer, out int targetIndex))
                return null;

            return RenderStandalone(
                targetContainer,
                targetIndex,
                outputWidth,
                outputHeight,
                scaleMultiplier,
                renderStack);
        }

        private RenderTexture RenderPreviousInput(
            List<Layer> container,
            int currentIndex,
            int outputWidth,
            int outputHeight,
            float scaleMultiplier,
            HashSet<Layer> renderStack)
        {
            int previousIndex = currentIndex + 1;
            if (previousIndex >= container.Count)
                return null;

            Layer previous = container[previousIndex];
            if (previous is GroupLayer group)
                return RenderGroupAlpha(group, outputWidth, outputHeight, scaleMultiplier, renderStack);
            return RenderStandalone(
                container,
                previousIndex,
                outputWidth,
                outputHeight,
                scaleMultiplier,
                renderStack);
        }

        private RenderTexture RenderGroupAlpha(
            GroupLayer group,
            int outputWidth,
            int outputHeight,
            float scaleMultiplier,
            HashSet<Layer> renderStack)
        {
            if (group == null || !group.enabled)
                return null;

            RenderTexture mask = GetClearRenderTexture(outputWidth, outputHeight);
            bool hasContent = AccumulateGroupAlpha(
                group.layers,
                ref mask,
                outputWidth,
                outputHeight,
                scaleMultiplier,
                renderStack);
            if (hasContent)
                return mask;

            RenderTexture.ReleaseTemporary(mask);
            return null;
        }

        private bool AccumulateGroupAlpha(
            List<Layer> sourceLayers,
            ref RenderTexture mask,
            int outputWidth,
            int outputHeight,
            float scaleMultiplier,
            HashSet<Layer> renderStack)
        {
            if (sourceLayers == null)
                return false;

            bool hasContent = false;
            for (int i = sourceLayers.Count - 1; i >= 0; i--)
            {
                Layer layer = sourceLayers[i];
                if (layer == null || !layer.enabled)
                    continue;

                RenderTexture rendered;
                float opacity;
                if (layer is GroupLayer nestedGroup)
                {
                    rendered = RenderGroupAlpha(
                        nestedGroup,
                        outputWidth,
                        outputHeight,
                        scaleMultiplier,
                        renderStack);
                    opacity = 1f;
                }
                else
                {
                    rendered = RenderStandalone(
                        sourceLayers,
                        i,
                        outputWidth,
                        outputHeight,
                        scaleMultiplier,
                        renderStack);
                    opacity = layer.opacity;
                }

                if (rendered == null)
                    continue;

                hasContent = true;
                try
                {
                    BlendInto(ref mask, rendered, (BlendMode)AlphaUnionMode, opacity);
                }
                finally
                {
                    RenderTexture.ReleaseTemporary(rendered);
                }
            }

            return hasContent;
        }

        private static void BlendInto(ref RenderTexture accumulator, RenderTexture layer, BlendMode mode, float opacity)
        {
            Material material = SpriteEditorMaterials.Blend;
            RenderTexture result = RenderTexture.GetTemporary(
                accumulator.width,
                accumulator.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            result.filterMode = FilterMode.Bilinear;
            result.wrapMode = TextureWrapMode.Clamp;

            if (material == null)
            {
                Graphics.Blit(layer, result);
            }
            else
            {
                material.SetTexture("_Blend", layer);
                material.SetFloat("_Mode", (int)mode);
                material.SetFloat("_Opacity", Mathf.Clamp01(opacity));
                Graphics.Blit(accumulator, result, material);
            }

            RenderTexture.ReleaseTemporary(accumulator);
            accumulator = result;
        }

        private static RenderTexture GetClearRenderTexture(int outputWidth, int outputHeight)
        {
            RenderTexture result = RenderTexture.GetTemporary(
                outputWidth,
                outputHeight,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            result.filterMode = FilterMode.Bilinear;
            result.wrapMode = TextureWrapMode.Clamp;

            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = result;
                GL.Clear(true, true, Color.clear);
            }
            finally
            {
                RenderTexture.active = previous;
            }
            return result;
        }

        private void GetPreviewDimensions(int maxSize, out int previewWidth, out int previewHeight, out float scaleMultiplier)
        {
            maxSize = Mathf.Max(1, maxSize);
            float scale = Mathf.Min(1f, (float)maxSize / Mathf.Max(width, height));
            previewWidth = Mathf.Max(1, Mathf.RoundToInt(width * scale));
            previewHeight = Mathf.Max(1, Mathf.RoundToInt(height * scale));
            scaleMultiplier = Mathf.Max((float)width / previewWidth, (float)height / previewHeight);
        }

        private void CollectEffectTargetOptions(
            List<Layer> sourceLayers,
            TargetedLayerEffect consumer,
            int depth,
            List<string> targetIds,
            List<string> labels)
        {
            if (sourceLayers == null)
                return;

            for (int i = 0; i < sourceLayers.Count; i++)
            {
                Layer candidate = sourceLayers[i];
                if (candidate == null)
                    continue;

                bool isGroup = candidate is GroupLayer;
                if (!LayerDependsOn(candidate, consumer, new HashSet<Layer>()))
                {
                    string candidateName = string.IsNullOrWhiteSpace(candidate.layerName)
                        ? candidate.GetType().Name
                        : candidate.layerName;
                    string indentation = depth > 0 ? new string(' ', depth * 4) + "↳ " : string.Empty;
                    targetIds.Add(candidate.Id);
                    labels.Add(indentation + candidateName + (isGroup ? "  [Group]" : string.Empty));
                }

                if (candidate is GroupLayer group)
                {
                    CollectEffectTargetOptions(
                        group.layers,
                        consumer,
                        depth + 1,
                        targetIds,
                        labels);
                }
            }
        }

        private bool LayerDependsOn(Layer candidate, Layer soughtLayer, HashSet<Layer> visited)
        {
            if (candidate == null)
                return false;
            if (ReferenceEquals(candidate, soughtLayer))
                return true;
            if (!visited.Add(candidate))
                return false;

            if (candidate is GroupLayer group)
            {
                if (group.layers == null)
                    return false;
                for (int i = 0; i < group.layers.Count; i++)
                {
                    if (LayerDependsOn(group.layers[i], soughtLayer, visited))
                        return true;
                }
                return false;
            }

            if (!(candidate is TargetedLayerEffect effect))
                return false;

            Layer input = null;
            if (effect.inputMode == EffectInputMode.Specific)
            {
                input = FindLayer(effect.TargetLayerId);
            }
            else if (TryFindLayer(effect, out List<Layer> container, out int index) &&
                     index + 1 < container.Count)
            {
                input = container[index + 1];
            }

            return LayerDependsOn(input, soughtLayer, visited);
        }

        private static void NormalizeLayers(List<Layer> sourceLayers, HashSet<string> usedIds)
        {
            for (int i = 0; i < sourceLayers.Count; i++)
            {
                Layer layer = sourceLayers[i];
                if (layer == null)
                    continue;

                layer.EnsureId(usedIds);
                layer.opacity = Mathf.Clamp01(layer.opacity);
                if (layer is DrawingLayer drawing)
                    drawing.NormalizeSettings();
                if (layer is GroupLayer group)
                {
                    group.layers ??= new List<Layer>();
                    NormalizeLayers(group.layers, usedIds);
                }
            }
        }

        private static int SynchronizeCounter(int nextNumber, int highestNumber)
        {
            int numberAfterExisting = highestNumber < int.MaxValue ? highestNumber + 1 : int.MaxValue;
            return Mathf.Max(1, Mathf.Max(nextNumber, numberAfterExisting));
        }

        private static Layer FindLayerRecursive(List<Layer> sourceLayers, string id)
        {
            if (sourceLayers == null)
                return null;

            for (int i = 0; i < sourceLayers.Count; i++)
            {
                Layer layer = sourceLayers[i];
                if (layer == null)
                    continue;
                if (layer.Id == id)
                    return layer;
                if (layer is GroupLayer group)
                {
                    Layer found = FindLayerRecursive(group.layers, id);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        private static bool TryFindLayerRecursive(
            List<Layer> sourceLayers,
            Layer target,
            out List<Layer> container,
            out int index)
        {
            if (sourceLayers != null)
            {
                for (int i = 0; i < sourceLayers.Count; i++)
                {
                    Layer layer = sourceLayers[i];
                    if (ReferenceEquals(layer, target))
                    {
                        container = sourceLayers;
                        index = i;
                        return true;
                    }

                    if (layer is GroupLayer group &&
                        TryFindLayerRecursive(group.layers, target, out container, out index))
                        return true;
                }
            }

            container = null;
            index = -1;
            return false;
        }

        private static bool TryFindParentGroupRecursive(
            List<Layer> sourceLayers,
            List<Layer> childList,
            out GroupLayer parent,
            out List<Layer> parentContainer,
            out int parentIndex)
        {
            if (sourceLayers != null)
            {
                for (int i = 0; i < sourceLayers.Count; i++)
                {
                    if (!(sourceLayers[i] is GroupLayer group))
                        continue;
                    if (ReferenceEquals(group.layers, childList))
                    {
                        parent = group;
                        parentContainer = sourceLayers;
                        parentIndex = i;
                        return true;
                    }
                    if (TryFindParentGroupRecursive(group.layers, childList, out parent, out parentContainer, out parentIndex))
                        return true;
                }
            }

            parent = null;
            parentContainer = null;
            parentIndex = -1;
            return false;
        }

        private static void ReleaseLayerResources(List<Layer> sourceLayers)
        {
            if (sourceLayers == null)
                return;
            for (int i = 0; i < sourceLayers.Count; i++)
            {
                Layer layer = sourceLayers[i];
                if (layer == null)
                    continue;
                layer.ReleaseTransientResources();
                if (layer is GroupLayer group)
                    ReleaseLayerResources(group.layers);
            }
        }

        private static void VisitDrawingLayers(List<Layer> sourceLayers, Action<DrawingLayer> visitor)
        {
            if (sourceLayers == null || visitor == null)
                return;
            for (int i = 0; i < sourceLayers.Count; i++)
            {
                Layer layer = sourceLayers[i];
                if (layer is DrawingLayer drawing)
                    visitor(drawing);
                if (layer is GroupLayer group)
                    VisitDrawingLayers(group.layers, visitor);
            }
        }
    }
}
