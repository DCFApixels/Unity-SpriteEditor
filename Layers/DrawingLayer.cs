using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    [Serializable]
    public sealed class DrawingLayer : Layer
    {
        private const int MinimumRepeatCount = 2;
        private const int MaximumRepeatCount = 64;

        [SerializeField] private Texture2D pixels;

        public PaintToolMode tool = PaintToolMode.Brush;
        public Color brushColor = Color.white;
        public float brushSize = 32f;
        [Range(0f, 1f)] public float brushHardness = 0.8f;

        public bool mirrorAcrossVerticalAxis;
        public bool mirrorAcrossHorizontalAxis;
        public Vector2 patternCenter = new Vector2(0.5f, 0.5f);

        public PaintRepeatMode repeatMode;
        public PaintRepeatElementMode repeatElementMode;
        public PaintRepeatBoundaryMode repeatBoundaryMode;
        public int repeatCount = 8;
        public int repeatSecondaryCount = 4;

        [NonSerialized] private RenderTexture paintSurface;
        [NonSerialized] private List<Vector2> symmetryPoints;
        [NonSerialized] private List<PaintStamp> patternStamps;

        internal Texture2D StoredTexture => pixels;

        public override Texture2D GetPreviewTexture(int size)
        {
            return pixels;
        }

        internal override RenderTexture Render(in LayerRenderContext context)
        {
            RenderTexture surface = EnsurePaintSurface(context.compositor.width, context.compositor.height);
            if (surface == null)
                return null;

            RenderTexture straight = RenderTexture.GetTemporary(
                context.width,
                context.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            straight.filterMode = FilterMode.Bilinear;
            straight.wrapMode = TextureWrapMode.Clamp;

            try
            {
                Material conversion = SpriteEditorMaterials.AlphaConversion;
                if (conversion == null)
                {
                    Graphics.Blit(surface, straight);
                }
                else
                {
                    conversion.SetFloat("_Mode", 1f);
                    Graphics.Blit(surface, straight, conversion);
                }

                return ApplyTransformAndModifiers(straight, context);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(straight);
            }
        }

        internal void NormalizeSettings()
        {
            brushSize = Mathf.Max(1f, brushSize);
            brushHardness = Mathf.Clamp01(brushHardness);
            patternCenter.x = Mathf.Clamp01(patternCenter.x);
            patternCenter.y = Mathf.Clamp01(patternCenter.y);
            repeatCount = Mathf.Clamp(repeatCount, MinimumRepeatCount, MaximumRepeatCount);
            repeatSecondaryCount = Mathf.Clamp(repeatSecondaryCount, MinimumRepeatCount, MaximumRepeatCount);
        }

        internal void InitializeCanvas(int width, int height)
        {
            if (pixels != null)
                return;
            EnsurePaintSurface(width, height);
            SyncSurfaceToTexture();
        }

        internal void PrepareStroke(int width, int height, string undoName)
        {
            EnsurePaintSurface(width, height);
            if (pixels == null)
                SyncSurfaceToTexture();
            if (pixels != null)
                Undo.RegisterCompleteObjectUndo(pixels, undoName);
        }

        internal void PaintPoint(Vector2 sourceUv, int outputWidth, int outputHeight)
        {
            PaintSegment(sourceUv, sourceUv, outputWidth, outputHeight, true);
        }

        internal void PaintSegment(
            Vector2 fromSourceUv,
            Vector2 toSourceUv,
            int outputWidth,
            int outputHeight,
            bool includeStart)
        {
            RenderTexture surface = EnsurePaintSurface(outputWidth, outputHeight);
            if (surface == null)
                return;

            NormalizeSettings();
            outputWidth = Mathf.Max(1, outputWidth);
            outputHeight = Mathf.Max(1, outputHeight);
            Vector2 pixelDelta = new Vector2(
                (toSourceUv.x - fromSourceUv.x) * outputWidth,
                (toSourceUv.y - fromSourceUv.y) * outputHeight);
            float distance = pixelDelta.magnitude;
            float spacing = Mathf.Max(1f, brushSize * 0.16f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / spacing));
            int firstStep = includeStart ? 0 : 1;

            for (int step = firstStep; step <= steps; step++)
            {
                float t = steps <= 0 ? 1f : (float)step / steps;
                Vector2 point = Vector2.Lerp(fromSourceUv, toSourceUv, t);
                BuildPatternStamps(point, outputWidth, outputHeight);
                PaintBrushRenderer.Draw(
                    surface,
                    patternStamps,
                    brushSize,
                    brushHardness,
                    brushColor,
                    tool == PaintToolMode.Eraser,
                    outputWidth,
                    outputHeight,
                    patternCenter);
            }
        }

        internal void ClearSurface(int width, int height)
        {
            RenderTexture surface = EnsurePaintSurface(width, height);
            if (surface == null)
                return;

            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = surface;
                GL.Clear(true, true, Color.clear);
            }
            finally
            {
                RenderTexture.active = previous;
            }
            SyncSurfaceToTexture();
        }

        internal void SyncSurfaceToTexture()
        {
            if (paintSurface == null)
                return;

            if (pixels == null || pixels.width != paintSurface.width || pixels.height != paintSurface.height)
            {
                if (pixels != null && !AssetDatabase.Contains(pixels))
                    UnityEngine.Object.DestroyImmediate(pixels);
                pixels = new Texture2D(paintSurface.width, paintSurface.height, TextureFormat.RGBA32, false)
                {
                    name = GetTextureName(),
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            RenderTexture straight = RenderTexture.GetTemporary(
                paintSurface.width,
                paintSurface.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Material conversion = SpriteEditorMaterials.AlphaConversion;
                if (conversion == null)
                {
                    Graphics.Blit(paintSurface, straight);
                }
                else
                {
                    conversion.SetFloat("_Mode", 1f);
                    Graphics.Blit(paintSurface, straight, conversion);
                }

                RenderTexture.active = straight;
                pixels.ReadPixels(new Rect(0f, 0f, straight.width, straight.height), 0, 0, false);
                pixels.Apply(false, false);
                pixels.name = GetTextureName();
                if (AssetDatabase.Contains(pixels))
                    EditorUtility.SetDirty(pixels);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(straight);
            }
        }

        internal void CloneStoredTexture()
        {
            SyncSurfaceToTexture();
            if (pixels == null)
                return;

            pixels = UnityEngine.Object.Instantiate(pixels);
            pixels.name = GetTextureName();
            pixels.hideFlags = HideFlags.HideAndDontSave;
            ReleasePaintSurface();
        }

        internal bool MakeTexturePersistent(TextureCompositor owner)
        {
            if (owner == null || !AssetDatabase.Contains(owner))
                return false;
            if (pixels == null)
            {
                EnsurePaintSurface(owner.width, owner.height);
                SyncSurfaceToTexture();
            }
            if (pixels == null || AssetDatabase.Contains(pixels))
                return false;

            pixels.name = GetTextureName();
            pixels.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(pixels, owner);
            EditorUtility.SetDirty(pixels);
            EditorUtility.SetDirty(owner);
            return true;
        }

        internal void DestroyStoredTextureWithUndo()
        {
            ReleasePaintSurface();
            if (pixels == null)
                return;

            if (AssetDatabase.Contains(pixels))
                Undo.DestroyObjectImmediate(pixels);
            else
                UnityEngine.Object.DestroyImmediate(pixels);
            pixels = null;
        }

        internal void InvalidatePaintSurface()
        {
            ReleasePaintSurface();
        }

        internal override void ReleaseTransientResources()
        {
            ReleasePaintSurface();
            if (pixels != null && !AssetDatabase.Contains(pixels) &&
                (pixels.hideFlags & HideFlags.DontSave) != 0)
            {
                UnityEngine.Object.DestroyImmediate(pixels);
                pixels = null;
            }
        }

        private RenderTexture EnsurePaintSurface(int fallbackWidth, int fallbackHeight)
        {
            int width = pixels != null ? pixels.width : Mathf.Max(1, fallbackWidth);
            int height = pixels != null ? pixels.height : Mathf.Max(1, fallbackHeight);
            if (paintSurface != null && paintSurface.width == width && paintSurface.height == height)
                return paintSurface;

            ReleasePaintSurface();
            paintSurface = new RenderTexture(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default)
            {
                name = GetTextureName() + " (Paint Surface)",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            paintSurface.Create();

            if (pixels != null)
            {
                Material conversion = SpriteEditorMaterials.AlphaConversion;
                if (conversion == null)
                {
                    Graphics.Blit(pixels, paintSurface);
                }
                else
                {
                    conversion.SetFloat("_Mode", 0f);
                    Graphics.Blit(pixels, paintSurface, conversion);
                }
            }
            else
            {
                RenderTexture previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = paintSurface;
                    GL.Clear(true, true, Color.clear);
                }
                finally
                {
                    RenderTexture.active = previous;
                }
            }
            return paintSurface;
        }

        private void BuildPatternStamps(Vector2 sourceUv, int outputWidth, int outputHeight)
        {
            symmetryPoints ??= new List<Vector2>(4);
            patternStamps ??= new List<PaintStamp>(MaximumRepeatCount * 4);
            symmetryPoints.Clear();
            patternStamps.Clear();

            AddSymmetryPoint(sourceUv);
            if (mirrorAcrossVerticalAxis)
                AddSymmetryPoint(new Vector2(patternCenter.x * 2f - sourceUv.x, sourceUv.y));
            if (mirrorAcrossHorizontalAxis)
                AddSymmetryPoint(new Vector2(sourceUv.x, patternCenter.y * 2f - sourceUv.y));
            if (mirrorAcrossVerticalAxis && mirrorAcrossHorizontalAxis)
            {
                AddSymmetryPoint(new Vector2(
                    patternCenter.x * 2f - sourceUv.x,
                    patternCenter.y * 2f - sourceUv.y));
            }

            for (int i = 0; i < symmetryPoints.Count; i++)
                AddRepeatedStamps(symmetryPoints[i], outputWidth, outputHeight);
        }

        private void AddSymmetryPoint(Vector2 point)
        {
            for (int i = 0; i < symmetryPoints.Count; i++)
            {
                if ((symmetryPoints[i] - point).sqrMagnitude < 0.00000001f)
                    return;
            }
            symmetryPoints.Add(point);
        }

        private void AddRepeatedStamps(Vector2 point, int outputWidth, int outputHeight)
        {
            int primaryCount = Mathf.Clamp(repeatCount, MinimumRepeatCount, MaximumRepeatCount);
            int secondaryCount = Mathf.Clamp(repeatSecondaryCount, MinimumRepeatCount, MaximumRepeatCount);
            bool clip = repeatBoundaryMode == PaintRepeatBoundaryMode.Clip;
            bool alternate = repeatElementMode == PaintRepeatElementMode.AlternateMirror;

            switch (repeatMode)
            {
                case PaintRepeatMode.Horizontal:
                {
                    float localX = Mathf.Repeat(point.x * primaryCount, 1f);
                    for (int x = 0; x < primaryCount; x++)
                    {
                        float repeatedX = (x + (alternate && (x & 1) != 0 ? 1f - localX : localX)) /
                                          primaryCount;
                        Vector4 clipRect = new Vector4(
                            (float)x / primaryCount,
                            0f,
                            (float)(x + 1) / primaryCount,
                            1f);
                        AddStamp(new Vector2(repeatedX, point.y), clip ? 1 : 0, clipRect, 0f, 0f);
                    }
                    break;
                }

                case PaintRepeatMode.Vertical:
                {
                    float localY = Mathf.Repeat(point.y * primaryCount, 1f);
                    for (int y = 0; y < primaryCount; y++)
                    {
                        float repeatedY = (y + (alternate && (y & 1) != 0 ? 1f - localY : localY)) /
                                          primaryCount;
                        Vector4 clipRect = new Vector4(
                            0f,
                            (float)y / primaryCount,
                            1f,
                            (float)(y + 1) / primaryCount);
                        AddStamp(new Vector2(point.x, repeatedY), clip ? 1 : 0, clipRect, 0f, 0f);
                    }
                    break;
                }

                case PaintRepeatMode.Grid:
                {
                    float localX = Mathf.Repeat(point.x * primaryCount, 1f);
                    float localY = Mathf.Repeat(point.y * secondaryCount, 1f);
                    for (int y = 0; y < secondaryCount; y++)
                    {
                        for (int x = 0; x < primaryCount; x++)
                        {
                            bool mirrored = alternate && ((x + y) & 1) != 0;
                            float repeatedX = (x + (mirrored ? 1f - localX : localX)) / primaryCount;
                            float repeatedY = (y + localY) / secondaryCount;
                            Vector4 clipRect = new Vector4(
                                (float)x / primaryCount,
                                (float)y / secondaryCount,
                                (float)(x + 1) / primaryCount,
                                (float)(y + 1) / secondaryCount);
                            AddStamp(new Vector2(repeatedX, repeatedY), clip ? 1 : 0, clipRect, 0f, 0f);
                        }
                    }
                    break;
                }

                case PaintRepeatMode.Radial:
                {
                    Vector2 canvasSize = new Vector2(Mathf.Max(1, outputWidth), Mathf.Max(1, outputHeight));
                    Vector2 deltaPixels = Vector2.Scale(point - patternCenter, canvasSize);
                    float radius = deltaPixels.magnitude;
                    float angle = Mathf.Atan2(deltaPixels.y, deltaPixels.x);
                    float sectorWidth = Mathf.PI * 2f / primaryCount;
                    const float startAngle = -Mathf.PI;
                    float localAngle = Mathf.Repeat(angle - startAngle, sectorWidth);
                    for (int sector = 0; sector < primaryCount; sector++)
                    {
                        bool mirrored = alternate && (sector & 1) != 0;
                        float repeatedAngle = startAngle + sector * sectorWidth +
                                              (mirrored ? sectorWidth - localAngle : localAngle);
                        Vector2 repeatedPixels = new Vector2(
                            Mathf.Cos(repeatedAngle) * radius,
                            Mathf.Sin(repeatedAngle) * radius);
                        Vector2 repeatedUv = patternCenter + new Vector2(
                            repeatedPixels.x / canvasSize.x,
                            repeatedPixels.y / canvasSize.y);
                        float clipCenter = startAngle + (sector + 0.5f) * sectorWidth;
                        AddStamp(
                            repeatedUv,
                            clip ? 2 : 0,
                            new Vector4(0f, 0f, 1f, 1f),
                            clipCenter,
                            sectorWidth * 0.5f);
                    }
                    break;
                }

                default:
                    AddStamp(point, 0, new Vector4(0f, 0f, 1f, 1f), 0f, 0f);
                    break;
            }
        }

        private void AddStamp(
            Vector2 center,
            int clipMode,
            Vector4 clipRect,
            float clipAngleCenter,
            float clipAngleHalfWidth)
        {
            for (int i = 0; i < patternStamps.Count; i++)
            {
                PaintStamp existing = patternStamps[i];
                if ((existing.center - center).sqrMagnitude >= 0.00000001f || existing.clipMode != clipMode)
                    continue;
                if (clipMode == 0 ||
                    (clipMode == 1 && (existing.clipRect - clipRect).sqrMagnitude < 0.00000001f) ||
                    (clipMode == 2 && Mathf.Abs(Mathf.DeltaAngle(
                        existing.clipAngleCenter * Mathf.Rad2Deg,
                        clipAngleCenter * Mathf.Rad2Deg)) < 0.001f))
                {
                    return;
                }
            }

            patternStamps.Add(new PaintStamp
            {
                center = center,
                clipMode = clipMode,
                clipRect = clipRect,
                clipAngleCenter = clipAngleCenter,
                clipAngleHalfWidth = clipAngleHalfWidth
            });
        }

        private void ReleasePaintSurface()
        {
            if (paintSurface == null)
                return;
            paintSurface.Release();
            UnityEngine.Object.DestroyImmediate(paintSurface);
            paintSurface = null;
        }

        private string GetTextureName()
        {
            return (string.IsNullOrWhiteSpace(layerName) ? "Drawing Layer" : layerName) + " Pixels";
        }

        internal struct PaintStamp
        {
            public Vector2 center;
            public int clipMode;
            public Vector4 clipRect;
            public float clipAngleCenter;
            public float clipAngleHalfWidth;
        }

        private static class PaintBrushRenderer
        {
            private static readonly int ColorId = Shader.PropertyToID("_Color");
            private static readonly int HardnessId = Shader.PropertyToID("_Hardness");
            private static readonly int CanvasSizeId = Shader.PropertyToID("_CanvasSize");
            private static readonly int ClipModeId = Shader.PropertyToID("_ClipMode");
            private static readonly int ClipRectId = Shader.PropertyToID("_ClipRect");
            private static readonly int PatternCenterId = Shader.PropertyToID("_PatternCenter");
            private static readonly int ClipAngleCenterId = Shader.PropertyToID("_ClipAngleCenter");
            private static readonly int ClipAngleHalfWidthId = Shader.PropertyToID("_ClipAngleHalfWidth");
            private static readonly int SourceBlendId = Shader.PropertyToID("_SrcBlend");
            private static readonly int DestinationBlendId = Shader.PropertyToID("_DstBlend");

            public static void Draw(
                RenderTexture target,
                List<PaintStamp> stamps,
                float sizePixels,
                float hardness,
                Color color,
                bool erase,
                int outputWidth,
                int outputHeight,
                Vector2 patternCenter)
            {
                if (target == null || stamps == null || stamps.Count == 0)
                    return;

                Material material = SpriteEditorMaterials.PaintBrush;
                if (material == null)
                    return;

                float radiusX = sizePixels / Mathf.Max(1f, outputWidth) * 0.5f;
                float radiusY = sizePixels / Mathf.Max(1f, outputHeight) * 0.5f;
                material.SetColor(ColorId, color);
                material.SetFloat(HardnessId, Mathf.Clamp01(hardness));
                material.SetVector(CanvasSizeId, new Vector4(outputWidth, outputHeight, 0f, 0f));
                material.SetVector(PatternCenterId, new Vector4(patternCenter.x, patternCenter.y, 0f, 0f));
                material.SetFloat(
                    SourceBlendId,
                    erase
                        ? (float)UnityEngine.Rendering.BlendMode.Zero
                        : (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat(
                    DestinationBlendId,
                    (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

                RenderTexture previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = target;
                    GL.PushMatrix();
                    try
                    {
                        GL.LoadOrtho();
                        for (int i = 0; i < stamps.Count; i++)
                        {
                            PaintStamp stamp = stamps[i];
                            Rect brushRect = new Rect(
                                stamp.center.x - radiusX,
                                stamp.center.y - radiusY,
                                radiusX * 2f,
                                radiusY * 2f);
                            if (brushRect.xMax <= 0f || brushRect.xMin >= 1f ||
                                brushRect.yMax <= 0f || brushRect.yMin >= 1f)
                            {
                                continue;
                            }

                            material.SetFloat(ClipModeId, stamp.clipMode);
                            material.SetVector(ClipRectId, stamp.clipRect);
                            material.SetFloat(ClipAngleCenterId, stamp.clipAngleCenter);
                            material.SetFloat(ClipAngleHalfWidthId, stamp.clipAngleHalfWidth);
                            if (!material.SetPass(0))
                                continue;

                            GL.Begin(GL.QUADS);
                            GL.MultiTexCoord2(0, 0f, 0f);
                            GL.Vertex3(brushRect.xMin, brushRect.yMin, 0f);
                            GL.MultiTexCoord2(0, 0f, 1f);
                            GL.Vertex3(brushRect.xMin, brushRect.yMax, 0f);
                            GL.MultiTexCoord2(0, 1f, 1f);
                            GL.Vertex3(brushRect.xMax, brushRect.yMax, 0f);
                            GL.MultiTexCoord2(0, 1f, 0f);
                            GL.Vertex3(brushRect.xMax, brushRect.yMin, 0f);
                            GL.End();
                        }
                    }
                    finally
                    {
                        GL.PopMatrix();
                    }
                }
                finally
                {
                    RenderTexture.active = previous;
                }
            }
        }
    }
}
