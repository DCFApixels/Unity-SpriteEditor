using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositor
    {
        [Serializable]
        private sealed class LayerNameCounter
        {
            public string prefix;
            public int next = 1;
        }

        [SerializeField, HideInInspector] private List<LayerNameCounter> layerNameCounters = new List<LayerNameCounter>();
        [SerializeField, HideInInspector] private int nextCopyNumber = 1;

        private static string LayerNamePrefix(Layer layer)
        {
            switch (layer)
            {
                case GroupLayer _: return "Group";
                case FileLayer _: return "File";
                case DrawingLayer _: return "Layer";
                case ColorFillLayer _: return "Color Fill";
                case GradientLayer _: return "Gradient";
                case NoiseLayer _: return "Noise";
                case OutlineLayer _: return "Outline";
                case SDFLayer _: return "SDF";
                case NormalMapLayer _: return "Normal Map";
                case GaussianBlurLayer _: return "Gaussian Blur";
                case MotionBlurLayer _: return "Motion Blur";
                case ShaderProcessorLayer _: return "Shader Processor";
                default: return ObjectNames.NicifyVariableName(layer.GetType().Name);
            }
        }

        internal string AllocateLayerName(Layer layer) => AllocateName(LayerNamePrefix(layer));
        internal string AllocateGroupName() => AllocateName("Group");

        private string AllocateName(string prefix)
        {
            SynchronizeNextAutomaticNumbers();
            LayerNameCounter counter = GetNameCounter(prefix);
            int number = counter.next;
            if (number == int.MaxValue)
                throw new InvalidOperationException("Layer name counter is exhausted.");
            counter.next++;
            return prefix + " " + number;
        }

        private string AllocateDuplicateName(Layer source)
        {
            SynchronizeNextAutomaticNumbers();
            if (nextCopyNumber == int.MaxValue)
                throw new InvalidOperationException("Copy name counter is exhausted.");
            return source.layerName + " Copy " + nextCopyNumber++;
        }

        private LayerNameCounter GetNameCounter(string prefix)
        {
            layerNameCounters ??= new List<LayerNameCounter>();
            LayerNameCounter result = null;
            foreach (LayerNameCounter counter in layerNameCounters)
            {
                if (counter == null)
                    continue;
                counter.prefix = ShortNamePrefix(counter.prefix);
                if (counter.prefix == prefix)
                {
                    result ??= counter;
                    result.next = Mathf.Max(result.next, counter.next);
                }
            }
            if (result != null)
                return result;
            LayerNameCounter created = new LayerNameCounter { prefix = prefix };
            layerNameCounters.Add(created);
            return created;
        }

        private static string ShortNamePrefix(string prefix)
        {
            switch (prefix)
            {
                case "Drawing Layer": return "Layer";
                case "File Layer": return "File";
                case "Color Fill Layer": return "Color Fill";
                case "Gradient Layer": return "Gradient";
                case "Outline Layer": return "Outline";
                case "SDF Layer": return "SDF";
                default: return prefix;
            }
        }

        private void SynchronizeNextAutomaticNumbers()
        {
            LayerNameCounter groupCounter = GetNameCounter("Group");
            groupCounter.next = Mathf.Max(1, Mathf.Max(groupCounter.next, nextGroupNumber));
            nextCopyNumber = Mathf.Max(1, nextCopyNumber);
            Scan(layers);
            nextGroupNumber = groupCounter.next;

            void Scan(List<Layer> source)
            {
                if (source == null)
                    return;
                foreach (Layer layer in source)
                {
                    if (layer == null)
                        continue;
                    string name = layer.layerName ?? string.Empty;
                    string prefix = LayerNamePrefix(layer);
                    LayerNameCounter counter = GetNameCounter(prefix);
                    counter.next = Mathf.Max(1, counter.next);
                    if (name.StartsWith(prefix + " ", StringComparison.Ordinal) &&
                        int.TryParse(name.Substring(prefix.Length + 1), out int number))
                        counter.next = SynchronizeCounter(counter.next, number);
                    string legacyPrefix = layer is DrawingLayer ? "Drawing Layer" : prefix + " Layer";
                    if (name.StartsWith(legacyPrefix + " ", StringComparison.Ordinal) &&
                        int.TryParse(name.Substring(legacyPrefix.Length + 1), out int legacyNumber))
                        counter.next = SynchronizeCounter(counter.next, legacyNumber);
                    int copySuffix = name.LastIndexOf(" Copy ", StringComparison.Ordinal);
                    if (copySuffix >= 0 && int.TryParse(name.Substring(copySuffix + 6), out int copyNumber))
                        nextCopyNumber = SynchronizeCounter(nextCopyNumber, copyNumber);
                    if (layer is GroupLayer group)
                        Scan(group.layers);
                }
            }
        }
    }
}
