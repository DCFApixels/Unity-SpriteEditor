using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public enum LayerColorRange { Standard, HDR }
    public enum LayerBlendRange { Standard, HDR }
    internal readonly struct LayerRenderContext
    {
        public readonly TextureCompositor compositor;
        public readonly RenderTexture input;
        public readonly int width;
        public readonly int height;
        public readonly float scaleMultiplier;
        public readonly bool applyTransform;
        public readonly bool applyModifiers;

        public LayerRenderContext(
            TextureCompositor compositor,
            RenderTexture input,
            int width,
            int height,
            float scaleMultiplier,
            bool applyTransform = true,
            bool applyModifiers = true)
        {
            this.compositor = compositor;
            this.input = input;
            this.width = width;
            this.height = height;
            this.scaleMultiplier = Mathf.Max(0.0001f, scaleMultiplier);
            this.applyTransform = applyTransform;
            this.applyModifiers = applyModifiers;
        }
    }

    [Serializable]
    public abstract class Layer
    {
        [SerializeField] private string id;

        public string layerName = "New Layer";
        public bool enabled = true;
        [Range(0f, 1f)] public float opacity = 1f;
        public BlendMode blendMode = BlendMode.Normal;
        public LayerColorRange colorRange;
        public LayerBlendRange blendRange;
        public LayerSwizzle swizzle;
        // Keep the serialized field name and object-reference layout for existing Material FX.
        public List<UnityEngine.Object> modifiers = new List<UnityEngine.Object>();
        public TextureTransform transform = TextureTransform.Default;
        public LayerFilterMode filterMode = LayerFilterMode.Source;

        public string Id => id;
        internal virtual bool RequiresInput => false;
        internal virtual bool IsGroup => false;

        internal void AssignNewId() => id = Guid.NewGuid().ToString("N");

        internal void EnsureId(HashSet<string> usedIds)
        {
            if (string.IsNullOrEmpty(id) || usedIds.Contains(id))
                id = Guid.NewGuid().ToString("N");
            usedIds.Add(id);
            modifiers ??= new List<UnityEngine.Object>();
        }

        internal abstract RenderTexture Render(in LayerRenderContext context);

        internal Texture SamplingSource => this is FileLayer file ? file.sourceTexture :
            this is DrawingLayer drawing ? drawing.StoredTexture : null;

        internal FilterMode ResolveFilterMode(Texture fallback = null)
        {
            switch (filterMode)
            {
                case LayerFilterMode.Point: return FilterMode.Point;
                case LayerFilterMode.Bilinear: return FilterMode.Bilinear;
                case LayerFilterMode.Trilinear: return FilterMode.Trilinear;
                default:
                    Texture source = SamplingSource;
                    if (source == null)
                        source = fallback;
                    return source != null ? source.filterMode : FilterMode.Bilinear;
            }
        }

        internal bool TryGetOriginalAspectTransform(TextureCompositor owner, out TextureTransform fitted)
        {
            fitted = transform;
            if (owner == null || IsGroup)
                return false;
            Vector2 canvasSize = new Vector2(owner.width, owner.height);
            Vector2 sourceSize = canvasSize;
            Texture source = this is FileLayer file ? file.sourceTexture :
                this is DrawingLayer drawing ? drawing.StoredTexture : null;
            if (this is FileLayer && source == null)
                return false;
            if (source != null)
                sourceSize = new Vector2(source.width, source.height);
            return transform.TryFitOriginalAspect(canvasSize, sourceSize, out fitted);
        }

        internal void CopyRasterizedIdentityFrom(Layer source)
        {
            id = source.id;
            layerName = source.layerName;
            enabled = source.enabled;
            opacity = source.opacity;
            blendMode = source.blendMode;
            colorRange = source.colorRange;
            blendRange = source.blendRange;
            swizzle = source.swizzle;
            filterMode = source.filterMode;
            modifiers = source.modifiers == null ? new List<UnityEngine.Object>() : new List<UnityEngine.Object>(source.modifiers);
        }

        public virtual Texture2D GetPreviewTexture(int size)
        {
            return null;
        }

        internal virtual void ReleaseTransientResources()
        {
        }

        internal RenderTexture ApplyTransformAndModifiers(Texture source, in LayerRenderContext context)
        {
            if (source == null)
                return null;

            RenderTexture current = RenderTexture.GetTemporary(
                context.width,
                context.height,
                0,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            FilterMode resolvedFilter = ResolveFilterMode(source);
            current.filterMode = resolvedFilter;
            current.wrapMode = TextureWrapMode.Clamp;

            try
            {
                Material transformMaterial = SpriteEditorMaterials.Transform;
                if (transformMaterial != null)
                    transformMaterial.SetFloat("_DecodeSource", source is Texture2D t &&
                        UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(t.graphicsFormat) ? 1f : 0f);
                if (transformMaterial == null)
                {
                    Graphics.Blit(source, current);
                }
                else
                {
                    // Even identity transforms must use the independent filter/wrap settings.
                    TextureTransform applied = context.applyTransform ? transform : TextureTransform.Default;
                    Texture samplingSource = SamplingSource;
                    if (samplingSource == null)
                        samplingSource = source;
                    TextureWrapMode wrapU = TextureWrapMode.Clamp;
                    TextureWrapMode wrapV = TextureWrapMode.Clamp;
                    switch (applied.tiling)
                    {
                        case TransformTilingMode.Source:
                            wrapU = samplingSource.wrapModeU;
                            wrapV = samplingSource.wrapModeV;
                            break;
                        case TransformTilingMode.Repeat:
                            wrapU = wrapV = TextureWrapMode.Repeat;
                            break;
                        case TransformTilingMode.Mirror:
                            wrapU = wrapV = TextureWrapMode.Mirror;
                            break;
                    }
                    transformMaterial.SetVector("_Pivot", new Vector4(applied.pivot.x, applied.pivot.y, 0f, 0f));
                    Vector2 scaledPosition = applied.position / context.scaleMultiplier;
                    transformMaterial.SetVector("_Position", new Vector4(scaledPosition.x, scaledPosition.y, 0f, 0f));
                    transformMaterial.SetVector("_Scale", new Vector4(applied.scale.x, applied.scale.y, 0f, 0f));
                    transformMaterial.SetFloat("_Rotation", applied.rotation * Mathf.Deg2Rad);
                    transformMaterial.SetInt("_ClipOutside", applied.tiling == TransformTilingMode.Clip ? 1 : 0);
                    transformMaterial.SetInt("_WrapModeU", (int)wrapU);
                    transformMaterial.SetInt("_WrapModeV", (int)wrapV);
                    transformMaterial.SetInt("_FilterMode", (int)resolvedFilter);
                    transformMaterial.SetInt("_SourceMipCount", source is Texture2D texture ? texture.mipmapCount : 1);
                    transformMaterial.SetVector("_OutputSize", new Vector4(context.width, context.height, 0f, 0f));
                    Graphics.Blit(source, current, transformMaterial);
                }

                current = context.compositor.FinishStage(current);
                if (!context.applyModifiers || modifiers == null)
                    return current;

                for (int i = 0; i < modifiers.Count; i++)
                {
                    Material modifier = modifiers[i] is ShaderFX shaderFX
                        ? shaderFX.GetMaterial(context)
                        : modifiers[i] as Material;
                    if (modifier == null)
                        continue;

                    RenderTexture next = RenderTexture.GetTemporary(
                        context.width,
                        context.height,
                        0,
                        RenderTextureFormat.ARGBFloat,
                        RenderTextureReadWrite.Linear);
                    next.filterMode = resolvedFilter;
                    next.wrapMode = TextureWrapMode.Clamp;
                    try
                    {
                        Graphics.Blit(current, next, modifier);
                    }
                    catch
                    {
                        RenderTexture.ReleaseTemporary(next);
                        throw;
                    }
                    RenderTexture.ReleaseTemporary(current);
                    current = next;
                    current = context.compositor.FinishStage(current);
                }

                return current;
            }
            catch
            {
                RenderTexture.ReleaseTemporary(current);
                throw;
            }
        }
    }

    [Serializable]
    public abstract class TargetedLayerEffect : Layer
    {
        [SerializeField] private string targetLayerId;

        public EffectInputMode inputMode = EffectInputMode.Previous;
        public string TargetLayerId
        {
            get => targetLayerId;
            set => targetLayerId = value;
        }

        internal sealed override bool RequiresInput => true;
    }
}
