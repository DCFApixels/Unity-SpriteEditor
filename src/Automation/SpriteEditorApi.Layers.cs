using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using static DCFApixels.SpriteEditor.AgentJson;

namespace DCFApixels.SpriteEditor
{
    public static partial class SpriteEditorApi
    {
        private static Layer ApplyOperation(TextureCompositor document, JObject operation, Dictionary<string, Layer> aliases, bool execute)
        {
            string op = Text(operation, "op");
            if (op == "add")
            {
                Keys(operation, "op", "type", "as", "parent", "index", "settings", "transform");
                string type = Text(operation, "type");
                Layer added = type switch
                {
                    "file" => new FileLayer(), "drawing" => new DrawingLayer(), "group" => new GroupLayer(),
                    "color" => new ColorFillLayer(), "gradient" => new GradientLayer(),
                    "noise" => new NoiseLayer(),
                    "outline" => new OutlineLayer(), "sdf" => new SDFLayer(),
                    "normalMap" => new NormalMapLayer(),
                    "gaussianBlur" => new GaussianBlurLayer(),
                    "shaderProcessor" => new ShaderProcessorLayer(),
                    _ => throw new SpriteEditorApiException("invalid_request", "Unknown layer type: " + type)
                };
                string alias = Text(operation, "as");
                if (alias != null)
                {
                    Require(alias.Length > 0 && alias.Length <= 64 && !alias.StartsWith("@"), "Alias must be 1..64 characters without a leading @.");
                    Require(!aliases.ContainsKey(alias), "Duplicate alias: " + alias);
                }
                List<Layer> container = Container(document, Text(operation, "parent"), aliases);
                int index = Int(operation, "index", 0, 0, container.Count);
                added.AssignNewId();
                added.layerName = document.AllocateLayerName(added);
                container.Insert(index, added);
                if (alias != null) aliases.Add(alias, added);
                if (operation["settings"] != null) SetLayer(document, added, Obj(operation["settings"], "settings"));
                if (operation["transform"] != null) SetTransform(document, added, Obj(operation["transform"], "transform"));
                if (execute && added is DrawingLayer drawing)
                {
                    drawing.InitializeCanvas(document.width, document.height);
                    drawing.MakeTexturePersistent(document);
                    Undo.RegisterCreatedObjectUndo(drawing.StoredTexture, UndoName);
                }
                return added;
            }

            Layer layer = Resolve(document, Text(operation, "layer"), aliases);
            switch (op)
            {
                case "compact":
                    Keys(operation, "op", "layer");
                    Require(layer is DrawingLayer, "compact requires a Drawing layer.");
                    if (execute) ((DrawingLayer)layer).ConvertTo8Bit();
                    else layer.colorRange = LayerColorRange.Standard;
                    break;
                case "set":
                    Keys(operation, "op", "layer", "settings");
                    SetLayer(document, layer, Obj(operation["settings"], "settings"));
                    break;
                case "transform":
                    Keys(operation, "op", "layer", "transform");
                    SetTransform(document, layer, Obj(operation["transform"], "transform"));
                    break;
                case "target":
                    Keys(operation, "op", "layer", "input", "target");
                    Require(layer is TargetedLayerEffect, "target requires an effect layer.");
                    var effect = (TargetedLayerEffect)layer;
                    effect.inputMode = Enum(operation, "input", EffectInputMode.Specific);
                    Require(effect.inputMode != EffectInputMode.Previous || operation["target"] == null, "Previous input does not take a target.");
                    effect.TargetLayerId = effect.inputMode == EffectInputMode.Specific
                        ? Resolve(document, Text(operation, "target"), aliases).Id : null;
                    break;
                case "stroke":
                    Keys(operation, "op", "layer", "points", "space", "erase", "brush", "pencil");
                    Require(layer is DrawingLayer, "stroke requires a Drawing layer.");
                    Paint(document, (DrawingLayer)layer, operation, execute);
                    break;
                case "move":
                    Keys(operation, "op", "layer", "parent", "index");
                    List<Layer> destination = Container(document, Text(operation, "parent"), aliases);
                    if (layer is GroupLayer movingGroup)
                        Require(!ContainsContainer(movingGroup, destination), "A group cannot be moved into itself or its descendants.");
                    Require(document.TryFindLayer(layer, out List<Layer> source, out int sourceIndex), "Layer not found.");
                    source.RemoveAt(sourceIndex);
                    destination.Insert(Int(operation, "index", 0, 0, destination.Count), layer);
                    break;
                default:
                    throw new SpriteEditorApiException("invalid_request", "Unknown operation: " + op);
            }
            if (execute && layer is DrawingLayer changedDrawing) changedDrawing.SetColorRange(layer.colorRange);
            return layer;
        }

        private static bool ContainsContainer(GroupLayer group, List<Layer> candidate)
        {
            if (group.layers == candidate) return true;
            foreach (Layer child in group.layers)
                if (child is GroupLayer nested && ContainsContainer(nested, candidate)) return true;
            return false;
        }

        private static Layer Resolve(TextureCompositor document, string reference, Dictionary<string, Layer> aliases)
        {
            Require(!string.IsNullOrWhiteSpace(reference), "A layer ID or @alias is required.");
            Layer layer;
            if (reference.StartsWith("@")) aliases.TryGetValue(reference.Substring(1), out layer);
            else layer = document.FindLayer(reference);
            Require(layer != null, "Layer not found: " + reference, "layer_not_found");
            return layer;
        }

        private static List<Layer> Container(TextureCompositor document, string parent, Dictionary<string, Layer> aliases)
        {
            if (string.IsNullOrEmpty(parent)) return document.layers;
            Layer layer = Resolve(document, parent, aliases);
            Require(layer is GroupLayer, "parent must reference a group.");
            return ((GroupLayer)layer).layers;
        }

        private static void SetLayer(TextureCompositor document, Layer layer, JObject settings)
        {
            Keys(settings, "name", "enabled", "clippingMask", "opacity", "blend", "filter", "source", "colorRange", "blendRange", "swizzle", "compositing", "color", "brush",
                "metric", "outlineWidth", "outlineSoftness", "outlinePosition", "sourceChannel", "threshold",
                "distancePosition", "inverted", "maxDistance", "gradient", "normalMap", "gaussianBlur", "noise");
            foreach (var property in settings.Properties())
            {
                string key = property.Name;
                bool valid = key == "name" || key == "enabled" || key == "clippingMask" || key == "opacity" || key == "blend" ||
                    key == "colorRange" || key == "blendRange" || key == "swizzle" || key == "compositing" && layer is GroupLayer || !layer.IsGroup &&
                    (key == "opacity" || key == "blend" || key == "filter" ||
                    key == "source" && layer is FileLayer || key == "brush" && layer is DrawingLayer ||
                    key == "color" && (layer is ColorFillLayer || layer is OutlineLayer) ||
                    key == "metric" && (layer is SDFLayer || layer is OutlineLayer) ||
                    key == "normalMap" && layer is NormalMapLayer ||
                    key == "gaussianBlur" && layer is GaussianBlurLayer ||
                    key == "noise" && layer is NoiseLayer ||
                    (key == "outlineWidth" || key == "outlineSoftness" || key == "outlinePosition") && layer is OutlineLayer ||
                    (key == "sourceChannel" || key == "threshold" || key == "distancePosition" || key == "inverted" || key == "maxDistance") && layer is SDFLayer ||
                    key == "gradient" && (layer is GradientLayer || layer is SDFLayer));
                Require(valid, key + " is not supported by " + TypeName(layer) + " layers.");
            }
            layer.layerName = Text(settings, "name", layer.layerName);
            layer.enabled = Bool(settings, "enabled", layer.enabled);
            layer.clippingMask = Bool(settings, "clippingMask", layer.clippingMask);
            Require(!(layer is ShaderProcessorLayer) || !layer.clippingMask, "Shader Processor is a stack operation and cannot be a clipping layer.");
            layer.opacity = Number(settings, "opacity", layer.opacity, 0f, 1f);
            layer.blendMode = Enum(settings, "blend", layer.blendMode);
            layer.colorRange = Enum(settings, "colorRange", layer.colorRange);
            layer.blendRange = Enum(settings, "blendRange", layer.blendRange);
            if (settings["swizzle"] != null)
            {
                Require(settings["swizzle"] is JArray array && array.Count == 4,
                    "swizzle must contain four channel names in output RGBA order.");
                var values = (JArray)settings["swizzle"];
                var swizzle = new LayerSwizzle();
                for (int channel = 0; channel < 4; channel++)
                {
                    int source = values[channel].Type == JTokenType.String
                        ? System.Array.IndexOf(LayerSwizzle.Labels, (string)values[channel]) : -1;
                    Require(source >= 0, "Invalid swizzle channel. Use " + string.Join(", ", LayerSwizzle.Labels) + ".");
                    swizzle[channel] = (SwizzleChannel)source;
                }
                layer.swizzle = swizzle;
            }
            if (layer is GroupLayer group)
                group.compositing = Enum(settings, "compositing", group.compositing);
            layer.filterMode = Enum(settings, "filter", layer.filterMode);
            if (layer is FileLayer file && settings["source"] != null)
            {
                string path = ReadAssetPath(Text(settings, "source"));
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Require(texture != null, "No imported Texture2D at " + path + ". Import the image first.", "texture_not_found");
                Require(!ReferenceEquals(TextureCompositor.FindDocument(texture), document), "A document cannot sample its own output texture.");
                file.AssignSourceTexture(texture, document);
            }
            if (layer is ColorFillLayer fill && settings["color"] != null) fill.color = Color(settings["color"]);
            if (layer is NormalMapLayer normal && settings["normalMap"] != null)
                SetNormalMap(normal, Obj(settings["normalMap"], "normalMap"));
            if (layer is GaussianBlurLayer gaussian && settings["gaussianBlur"] != null)
                SetGaussianBlur(gaussian, Obj(settings["gaussianBlur"], "gaussianBlur"));
            if (layer is NoiseLayer noise && settings["noise"] != null)
                SetNoise(noise, Obj(settings["noise"], "noise"));
            if (layer is DrawingLayer drawing && settings["brush"] != null) SetBrush(drawing, Obj(settings["brush"], "brush"));
            if (layer is OutlineLayer outline)
            {
                outline.metric = Enum(settings, "metric", outline.metric);
                outline.outlineWidth = Number(settings, "outlineWidth", outline.outlineWidth, 0f, 16384f);
                outline.outlineSoftness = Number(settings, "outlineSoftness", outline.outlineSoftness, 0f, 16384f);
                outline.outlinePosition = Enum(settings, "outlinePosition", outline.outlinePosition);
                if (settings["color"] != null) outline.outlineColor = Color(settings["color"]);
            }
            if (layer is SDFLayer sdf)
            {
                sdf.metric = Enum(settings, "metric", sdf.metric);
                sdf.sourceChannel = Enum(settings, "sourceChannel", sdf.sourceChannel);
                sdf.threshold = (byte)Int(settings, "threshold", sdf.threshold, 0, 255);
                sdf.distancePosition = Enum(settings, "distancePosition", sdf.distancePosition);
                sdf.inverted = Bool(settings, "inverted", sdf.inverted);
                sdf.maxDistanceNormalization = Number(settings, "maxDistance", sdf.maxDistanceNormalization, 0f, 16384f);
                if (settings["gradient"] != null) sdf.gradient = ReadGradient(settings["gradient"]);
            }
            if (layer is GradientLayer gradient && settings["gradient"] != null) gradient.gradient = ReadGradient(settings["gradient"]);
        }

        private static Gradient ReadGradient(JToken token)
        {
            Require(token is JArray keys && keys.Count >= 2 && keys.Count <= 8, "gradient must contain 2..8 {time, color} stops.");
            var values = (JArray)token;
            var colors = new GradientColorKey[values.Count];
            var alphas = new GradientAlphaKey[values.Count];
            float previous = -1f;
            for (int i = 0; i < values.Count; i++)
            {
                JObject stop = Obj(values[i], "gradient stop");
                Keys(stop, "time", "color");
                float time = Number(stop["time"], "time", 0f, 1f);
                Require(time > previous, "Gradient stop times must be strictly increasing.");
                var color = Color(stop["color"]);
                colors[i] = new GradientColorKey(color, time);
                alphas[i] = new GradientAlphaKey(color.a, time);
                previous = time;
            }
            return GradientUtility.Create(colors, alphas);
        }

        private static void SetTransform(TextureCompositor document, Layer layer, JObject settings)
        {
            Require(!layer.IsGroup, "Groups do not have a transform.");
            Keys(settings, "reset", "position", "scale", "pivot", "rotation", "tiling", "originalAspect");
            TextureTransform transform = Bool(settings, "reset") ? TextureTransform.Default : layer.transform;
            if (settings["position"] != null) transform.position = Vector(settings["position"], "position");
            if (settings["scale"] != null) transform.scale = Vector(settings["scale"], "scale");
            if (settings["pivot"] != null) transform.pivot = Vector(settings["pivot"], "pivot");
            Require(Mathf.Abs(transform.scale.x) >= 0.00001f && Mathf.Abs(transform.scale.y) >= 0.00001f, "Transform scale must be nonzero.");
            transform.rotation = Number(settings, "rotation", transform.rotation, -360000f, 360000f);
            transform.tiling = Enum(settings, "tiling", transform.tiling);
            layer.transform = transform;
            if (Bool(settings, "originalAspect"))
            {
                Require(layer.TryGetOriginalAspectTransform(document, out TextureTransform fitted), "Original Aspect requires a valid source and transform.");
                layer.transform = fitted;
            }
        }
    }
}
