using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using static DCFApixels.SpriteEditor.AgentJson;

namespace DCFApixels.SpriteEditor
{
    public static partial class SpriteEditorApi
    {
        public static string Inspect(string assetPath) => Respond(() =>
        {
            string path = AssetPath(assetPath, ".asset");
            TextureCompositor document = Load(path);
            Require(!TextureCompositorWindow.IsDocumentBusyForApi(document), "Finish the current paint/transform gesture first.", "document_busy");
            JObject result = Success();
            result["document"] = Snapshot(document, path);
            return result;
        });

        public static string Describe() => Respond(() =>
        {
            JObject result = Success();
            result["operations"] = new JArray("add", "set", "transform", "target", "move", "stroke", "compact");
            result["colorRanges"] = new JArray(System.Enum.GetNames(typeof(LayerColorRange)));
            result["blendRanges"] = new JArray(System.Enum.GetNames(typeof(LayerBlendRange)));
            result["swizzleChannels"] = new JArray(LayerSwizzle.Labels);
            result["clippingMask"] = "Boolean setting on every layer type. Clips to the first non-clipping sibling below; missing/hidden bases hide the chain. Participating groups are isolated; base alpha and opacity are preserved.";
            result["groupCompositing"] = new JArray(System.Enum.GetNames(typeof(GroupCompositing)));
            result["layerTypes"] = new JArray("file", "drawing", "group", "color", "gradient", "outline", "sdf", "normalMap", "gaussianBlur");
            result["normalMapDefaults"] = NormalMapSnapshot(new NormalMapLayer());
            result["gaussianBlurDefaults"] = GaussianBlurSnapshot(new GaussianBlurLayer());
            result["blendModes"] = new JArray(System.Enum.GetNames(typeof(BlendMode)));
            result["tilingModes"] = new JArray(System.Enum.GetNames(typeof(TransformTilingMode)));
            result["filterModes"] = new JArray(System.Enum.GetNames(typeof(LayerFilterMode)));
            result["distanceMetrics"] = new JArray(System.Enum.GetNames(typeof(DistanceMetric)));
            result["repeatModes"] = new JArray(System.Enum.GetNames(typeof(PaintRepeatMode)));
            result["repeatElements"] = new JArray(System.Enum.GetNames(typeof(PaintRepeatElementMode)));
            result["repeatBoundaries"] = new JArray(System.Enum.GetNames(typeof(PaintRepeatBoundaryMode)));
            result["pencilShapes"] = new JArray(System.Enum.GetNames(typeof(PencilShape)));
            result["coordinates"] = "Layer index 0 is topmost. Transform position uses canvas pixels, +X right, +Y up; rotation is counterclockwise degrees. Pivot is bottom-left UV. canvasPixels stroke points use top-left origin; layerUv uses bottom-left UV.";
            result["limits"] = new JObject { ["requestBytes"] = 4194304, ["operations"] = 256, ["canvasPixels"] = MaxCanvasPixels,
                ["layers"] = 1024, ["drawingPixels"] = 67108864, ["strokePoints"] = 4096, ["strokeStamps"] = 100000, ["strokeCoveragePixels"] = 250000000 };
            result["editing"] = "Inspect before editing; expectedRevision is mandatory on existing documents. Use @aliases within a batch. New documents require save=true. dryRun validates without drawing or saving. Save failure may leave partial asset I/O: inspect before retrying.";
            result["reference"] = "Documentation~/AgentAPI.md";
            return result;
        });

        private static string Revision(TextureCompositor document)
        {
            using var hash = SHA256.Create();
            var text = new StringBuilder(EditorJsonUtility.ToJson(document));
            string path = AssetDatabase.GetAssetPath(document);
            if (!string.IsNullOrEmpty(path)) text.Append(AssetDatabase.GetAssetDependencyHash(path));
            foreach (Layer layer in Enumerate(document.layers))
            {
                if (layer is DrawingLayer drawing && drawing.StoredTexture != null)
                {
                    Require(drawing.StoredTexture.isReadable, "Drawing texture is not readable.", "invalid_document");
                    text.Append(Convert.ToBase64String(hash.ComputeHash(drawing.StoredTexture.GetRawTextureData())));
                }
                if (layer.modifiers != null)
                    foreach (UnityEngine.Object modifier in layer.modifiers)
                        if (modifier != null) text.Append(EditorJsonUtility.ToJson(modifier));
            }
            return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
        }

        private static JObject Snapshot(TextureCompositor document, string path)
        {
            var layers = new JArray();
            Collect(document.layers, null);
            return new JObject
            {
                ["assetPath"] = path, ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["revision"] = Revision(document), ["width"] = document.width, ["height"] = document.height,
                ["dirty"] = EditorUtility.IsDirty(document), ["hasOutputTexture"] = document.OutputTexture != null,
                ["hasOutputSprite"] = document.OutputSprite != null, ["layers"] = layers
            };

            void Collect(List<Layer> source, string parent)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    Layer layer = source[i];
                    if (layer == null) continue;
                    JObject settings = new JObject { ["name"] = layer.layerName, ["enabled"] = layer.enabled };
                    var entry = new JObject { ["id"] = layer.Id, ["type"] = TypeName(layer), ["parent"] = parent, ["index"] = i, ["settings"] = settings };
                    settings["opacity"] = layer.opacity;
                    settings["clippingMask"] = layer.clippingMask;
                    entry["clippingBaseId"] = document.GetClippingBase(layer)?.Id;
                    if (layer is GroupLayer clippingGroup)
                        entry["isolatedByClipping"] = document.IsGroupIsolatedByClipping(clippingGroup);
                    settings["blend"] = layer.blendMode.ToString();
                    settings["colorRange"] = layer.colorRange.ToString();
                    settings["blendRange"] = layer.blendRange.ToString();
                    settings["swizzle"] = new JArray(LayerSwizzle.Labels[(int)layer.swizzle[0]],
                        LayerSwizzle.Labels[(int)layer.swizzle[1]], LayerSwizzle.Labels[(int)layer.swizzle[2]],
                        LayerSwizzle.Labels[(int)layer.swizzle[3]]);
                    if (layer is GroupLayer folder) settings["compositing"] = folder.compositing.ToString();
                    if (layer is DrawingLayer stored) entry["storageFormat"] = stored.StoredTexture != null ? stored.StoredTexture.format.ToString() : "Unallocated";
                    if (!layer.IsGroup)
                    {
                        settings["opacity"] = layer.opacity;
                        settings["blend"] = layer.blendMode.ToString();
                        settings["filter"] = layer.filterMode.ToString();
                        entry["transform"] = new JObject
                        {
                            ["position"] = Json(layer.transform.position), ["scale"] = Json(layer.transform.scale),
                            ["pivot"] = Json(layer.transform.pivot), ["rotation"] = layer.transform.rotation, ["tiling"] = layer.transform.tiling.ToString()
                        };
                        entry["modifierCount"] = layer.modifiers?.Count ?? 0;
                    }
                    if (layer is FileLayer file)
                    {
                        settings["source"] = file.sourceTexture != null ? AssetDatabase.GetAssetPath(file.sourceTexture) : "";
                        if (file.sourceTexture != null)
                            entry["sourceSize"] = new JArray(file.sourceTexture.width, file.sourceTexture.height);
                    }
                    if (layer is ColorFillLayer fill) settings["color"] = Json(fill.color);
                    if (layer is DrawingLayer drawing) settings["brush"] = BrushSnapshot(drawing);
                    if (layer is TargetedLayerEffect targeted)
                    {
                        entry["input"] = targeted.inputMode.ToString();
                        entry["target"] = targeted.TargetLayerId;
                        entry["inputValid"] = document.HasUsableEffectInput(targeted, source, i);
                    }
                    if (layer is OutlineLayer outline)
                    {
                        settings["color"] = Json(outline.outlineColor);
                        settings["metric"] = outline.metric.ToString();
                        settings["outlineWidth"] = outline.outlineWidth;
                        settings["outlineSoftness"] = outline.outlineSoftness;
                        settings["outlinePosition"] = outline.outlinePosition.ToString();
                    }
                    if (layer is SDFLayer sdf)
                    {
                        settings["metric"] = sdf.metric.ToString();
                        settings["sourceChannel"] = sdf.sourceChannel.ToString();
                        settings["threshold"] = sdf.threshold;
                        settings["distancePosition"] = sdf.distancePosition.ToString();
                        settings["inverted"] = sdf.inverted;
                        settings["maxDistance"] = sdf.maxDistanceNormalization;
                        entry["gradientKeys"] = GradientSnapshot(sdf.gradient);
                    }
                    if (layer is NormalMapLayer normal) settings["normalMap"] = NormalMapSnapshot(normal);
                    if (layer is GaussianBlurLayer gaussian) settings["gaussianBlur"] = GaussianBlurSnapshot(gaussian);
                    if (layer is GradientLayer gradient) entry["gradientKeys"] = GradientSnapshot(gradient.gradient);
                    layers.Add(entry);
                    if (layer is GroupLayer group) Collect(group.layers, layer.Id);
                }
            }
        }

        private static JObject GradientSnapshot(Gradient gradient)
        {
            var colors = new JArray();
            var alphas = new JArray();
            if (gradient != null)
            {
                foreach (var key in gradient.colorKeys) colors.Add(new JObject { ["time"] = key.time, ["color"] = Json(key.color) });
                foreach (var key in gradient.alphaKeys) alphas.Add(new JObject { ["time"] = key.time, ["alpha"] = key.alpha });
            }
            return new JObject { ["colors"] = colors, ["alphas"] = alphas };
        }

        private static JObject BrushSnapshot(DrawingLayer layer) => new JObject
        {
            ["color"] = Json(layer.brushColor), ["size"] = layer.brushSize, ["hardness"] = layer.brushHardness,
            ["spacing"] = layer.brushSpacing, ["mirrorX"] = layer.mirrorAcrossVerticalAxis, ["mirrorY"] = layer.mirrorAcrossHorizontalAxis,
            ["center"] = Json(layer.patternCenter), ["repeat"] = layer.repeatMode.ToString(), ["repeatCount"] = layer.repeatCount,
            ["radialStartAngle"] = layer.radialStartAngle,
            ["mirrorAngle"] = layer.mirrorAngle,
            ["repeatSecondaryCount"] = layer.repeatSecondaryCount, ["elements"] = layer.repeatElementMode.ToString(), ["boundary"] = layer.repeatBoundaryMode.ToString()
        };

        private static string TypeName(Layer layer) => layer switch
        {
            FileLayer _ => "file", DrawingLayer _ => "drawing", GroupLayer _ => "group", ColorFillLayer _ => "color",
            GradientLayer _ => "gradient", OutlineLayer _ => "outline", SDFLayer _ => "sdf", NormalMapLayer _ => "normalMap",
            GaussianBlurLayer _ => "gaussianBlur", _ => layer.GetType().Name
        };
    }
}
