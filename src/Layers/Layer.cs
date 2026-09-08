using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
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
        public List<Material> modifiers = new List<Material>();
        public TextureTransform transform = TextureTransform.Default;

        public string Id => id;
        internal virtual bool RequiresInput => false;
        internal virtual bool IsGroup => false;

        internal void EnsureId(HashSet<string> usedIds)
        {
            if (string.IsNullOrEmpty(id) || usedIds.Contains(id))
                id = Guid.NewGuid().ToString("N");
            usedIds.Add(id);
            modifiers ??= new List<Material>();
        }

        internal abstract RenderTexture Render(in LayerRenderContext context);

        internal void CopyRasterizedIdentityFrom(Layer source)
        {
            id = source.id;
            layerName = source.layerName;
            enabled = source.enabled;
            opacity = source.opacity;
            blendMode = source.blendMode;
            modifiers = source.modifiers == null ? new List<Material>() : new List<Material>(source.modifiers);
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
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            current.filterMode = FilterMode.Bilinear;
            current.wrapMode = TextureWrapMode.Clamp;

            try
            {
                Material transformMaterial = SpriteEditorMaterials.Transform;
                if (!context.applyTransform || transform.IsIdentity() || transformMaterial == null)
                {
                    Graphics.Blit(source, current);
                }
                else
                {
                    transformMaterial.SetVector("_Pivot", new Vector4(transform.pivot.x, transform.pivot.y, 0f, 0f));
                    Vector2 scaledPosition = transform.position / context.scaleMultiplier;
                    transformMaterial.SetVector("_Position", new Vector4(scaledPosition.x, scaledPosition.y, 0f, 0f));
                    transformMaterial.SetVector("_Scale", new Vector4(transform.scale.x, transform.scale.y, 0f, 0f));
                    transformMaterial.SetFloat("_Rotation", transform.rotation * Mathf.Deg2Rad);
                    transformMaterial.SetInt("_TilingMode", (int)transform.tiling);
                    transformMaterial.SetVector("_OutputSize", new Vector4(context.width, context.height, 0f, 0f));
                    Graphics.Blit(source, current, transformMaterial);
                }

                if (!context.applyModifiers || modifiers == null)
                    return current;

                for (int i = 0; i < modifiers.Count; i++)
                {
                    Material modifier = modifiers[i];
                    if (modifier == null)
                        continue;

                    RenderTexture next = RenderTexture.GetTemporary(
                        context.width,
                        context.height,
                        0,
                        RenderTextureFormat.ARGB32,
                        RenderTextureReadWrite.Default);
                    next.filterMode = FilterMode.Bilinear;
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
