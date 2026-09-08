using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositor
    {
        internal Dictionary<Layer, Layer> DuplicateLayers(List<Layer> requested)
        {
            const string undoName = "Duplicate Sprite Layers";
            HashSet<Layer> selected = new HashSet<Layer>(requested);
            List<Layer> roots = new List<Layer>();
            CollectRoots(layers);
            Dictionary<Layer, Layer> copies = new Dictionary<Layer, Layer>();
            if (roots.Count == 0)
                return copies;

            Dictionary<string, string> copiedIds = new Dictionary<string, string>();
            Dictionary<ShaderFX, ShaderFX> effects = new Dictionary<ShaderFX, ShaderFX>();
            List<Texture2D> textures = new List<Texture2D>();
            List<DrawingLayer> drawings = new List<DrawingLayer>();
            Dictionary<TargetedLayerEffect, Layer> previousInputs = new Dictionary<TargetedLayerEffect, Layer>();
            int undoGroup = -1;
            try
            {
                VisitDrawingLayers(roots, drawing => drawing.SyncSurfaceToTexture());
                foreach (Layer source in roots)
                {
                    Layer copy = (Layer)JsonUtility.FromJson(JsonUtility.ToJson(source), source.GetType());
                    PrepareCopy(source, copy);
                }
                foreach (Layer copy in copies.Values)
                    if (copy is TargetedLayerEffect effect &&
                        !string.IsNullOrEmpty(effect.TargetLayerId) &&
                        copiedIds.TryGetValue(effect.TargetLayerId, out string targetId))
                        effect.TargetLayerId = targetId;

                Undo.IncrementCurrentGroup();
                undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(undoName);
                foreach (DrawingLayer drawing in drawings)
                    drawing.MakeTexturePersistent(this);
                foreach (ShaderFX effect in effects.Values)
                    effect.PersistEmbedded(this);
                foreach (Texture2D texture in textures)
                    Undo.RegisterCreatedObjectUndo(texture, undoName);
                foreach (ShaderFX effect in effects.Values)
                    effect.RegisterCreatedCopyUndo(undoName);
                Undo.RegisterCompleteObjectUndo(this, undoName);
                foreach (KeyValuePair<Layer, Layer> pair in copies)
                    pair.Value.layerName = AllocateDuplicateName(pair.Key);
                embeddedShaderFX.AddRange(effects.Values);
                InsertCopies(layers);
                foreach (KeyValuePair<TargetedLayerEffect, Layer> input in previousInputs)
                {
                    Layer target = input.Value;
                    if (target != null && copies.TryGetValue(target, out Layer targetCopy))
                        target = targetCopy;
                    if (TryFindLayer(input.Key, out List<Layer> container, out int index) &&
                        (index + 1 < container.Count ? container[index + 1] : null) != target)
                    {
                        input.Key.inputMode = EffectInputMode.Specific;
                        input.Key.TargetLayerId = target?.Id;
                    }
                }
                MarkChanged();
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(undoGroup);
                return copies;
            }
            catch
            {
                if (undoGroup >= 0)
                    Undo.RevertAllDownToGroup(undoGroup);
                foreach (DrawingLayer drawing in drawings)
                    drawing.InvalidatePaintSurface();
                foreach (ShaderFX effect in effects.Values)
                    if (effect != null)
                        DestroyImmediate(effect, true);
                foreach (Texture2D texture in textures)
                    if (texture != null)
                        DestroyImmediate(texture, true);
                throw;
            }
            finally
            {
                if (undoGroup >= 0)
                    Undo.IncrementCurrentGroup();
            }

            void CollectRoots(List<Layer> source)
            {
                if (source == null)
                    return;
                foreach (Layer layer in source)
                    if (layer != null)
                    {
                        if (selected.Contains(layer))
                            roots.Add(layer);
                        else if (layer is GroupLayer group)
                            CollectRoots(group.layers);
                    }
            }

            void PrepareCopy(Layer source, Layer copy)
            {
                if (copy == null || copy.GetType() != source.GetType())
                    throw new InvalidOperationException("Could not copy the layer data.");
                copy.AssignNewId();
                copies.Add(source, copy);
                copiedIds.Add(source.Id, copy.Id);
                if (copy is DrawingLayer drawing)
                {
                    drawing.CloneStoredTexture();
                    drawing.InitializeCanvas(width, height);
                    if (drawing.StoredTexture != null)
                        textures.Add(drawing.StoredTexture);
                    drawings.Add(drawing);
                }
                if (source is TargetedLayerEffect sourceEffect && sourceEffect.inputMode == EffectInputMode.Previous &&
                    TryFindLayer(source, out List<Layer> sourceContainer, out int sourceIndex))
                    previousInputs.Add((TargetedLayerEffect)copy,
                        sourceIndex + 1 < sourceContainer.Count ? sourceContainer[sourceIndex + 1] : null);
                if (copy.modifiers != null)
                    for (int i = 0; i < copy.modifiers.Count; i++)
                        if (copy.modifiers[i] is ShaderFX effect && effect.EmbeddedOwner != null)
                        {
                            if (!effects.TryGetValue(effect, out ShaderFX effectCopy))
                            {
                                effectCopy = effect.CloneForDocument(this);
                                effects.Add(effect, effectCopy);
                            }
                            copy.modifiers[i] = effectCopy;
                        }
                if (source is GroupLayer sourceGroup && copy is GroupLayer copyGroup)
                {
                    if (sourceGroup.layers.Count != copyGroup.layers.Count)
                        throw new InvalidOperationException("Could not copy the group's children.");
                    for (int i = 0; i < sourceGroup.layers.Count; i++)
                        if (sourceGroup.layers[i] != null)
                            PrepareCopy(sourceGroup.layers[i], copyGroup.layers[i]);
                }
            }

            void InsertCopies(List<Layer> container)
            {
                for (int i = 0; i < container.Count; i++)
                {
                    Layer source = container[i];
                    if (roots.Contains(source))
                    {
                        int start = i;
                        List<Layer> block = new List<Layer>();
                        while (i < container.Count && roots.Contains(container[i]))
                            block.Add(copies[container[i++]]);
                        container.InsertRange(start, block);
                        i += block.Count - 1;
                    }
                    else if (source is GroupLayer group)
                        InsertCopies(group.layers);
                }
            }
        }

    }
}
