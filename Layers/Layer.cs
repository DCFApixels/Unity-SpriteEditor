using System.Collections.Generic;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [System.Serializable]
    public abstract class Layer
    {
        public string layerName = "New Layer";
        public bool enabled = true;
        public float opacity = 1;
        public BlendMode blendMode = BlendMode.Overwrite;
        public List<Material> modifiers = new List<Material>();

        // Параметры трансформации вынесены в отдельную структуру
        public TextureTransform transform = TextureTransform.Default;

        private Material transformMaterial;
        protected Material GetTransformMaterial()
        {
            if (transformMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/TextureCompositor/Transform");
                if (shader != null)
                    transformMaterial = new Material(shader);
                else
                    Debug.LogError("Transform shader not found!");
            }
            return transformMaterial;
        }
        protected RenderTexture ApplyTransform(Texture source, int width, int height)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            if (transform.IsIdentity())
            {
                Graphics.Blit(source, rt);
            }
            else
            {
                Material mat = GetTransformMaterial();
                if (mat != null)
                {
                    mat.SetMatrix("_Transform", transform.ToMatrix());
                    Graphics.Blit(source, rt, mat);
                }
                else
                {
                    Graphics.Blit(source, rt); // fallback
                }
            }
            return rt;
        }



        public abstract RenderTexture GetRenderTexture(TextureCompositor compositor, int layerIndex, int width, int height, float scaleMultiplier = 1f);
        public virtual Texture2D GetPreviewTexture(int size)
        {
            return null;
        }
    }
}