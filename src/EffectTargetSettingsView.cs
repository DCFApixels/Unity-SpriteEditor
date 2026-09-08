using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    // Shared retained target selector for the embedded inspector and standalone Edit windows.
    internal sealed class EffectTargetSettingsView
    {
        private readonly TextureCompositor compositor;
        private readonly Action<string, Action> applyChange;
        private readonly SpriteEditorUI.ValueBindings bindings;
        private string[] effectTargetIds;
        private string[] effectTargetLabels;
        private string effectTargetOptionsForLayerId;
        private string effectTargetOptionsForTargetId;

        internal EffectTargetSettingsView(TextureCompositor compositor,
            Action<string, Action> applyChange, SpriteEditorUI.ValueBindings bindings)
        {
            this.compositor = compositor;
            this.applyChange = applyChange;
            this.bindings = bindings;
        }

        internal void Build(VisualElement root, TargetedLayerEffect effect)
        {
            EnumField input = SpriteEditorUI.ConfigureField(new EnumField("Input", effect.inputMode));
            bindings.Track(input, () => (Enum)effect.inputMode);
            input.RegisterValueChangedCallback(evt =>
            {
                applyChange("Change Effect Input", () => effect.inputMode = (EffectInputMode)evt.newValue);
            });
            root.Add(input);

            EnsureEffectTargetOptions(effect);
            int selectedIndex = FindEffectTargetIndex(effect.TargetLayerId);
            PopupField<string> target = SpriteEditorUI.ConfigureField(
                new PopupField<string>("Target", new List<string>(effectTargetLabels), selectedIndex));
            target.RegisterValueChangedCallback(evt =>
            {
                EnsureEffectTargetOptions(effect);
                int nextIndex = Array.IndexOf(effectTargetLabels, evt.newValue);
                if (nextIndex < 0 || nextIndex >= effectTargetIds.Length)
                    return;
                applyChange("Change Effect Target", () =>
                {
                    effect.inputMode = EffectInputMode.Specific;
                    effect.TargetLayerId = effectTargetIds[nextIndex];
                    Invalidate();
                });
            });
            target.tooltip = "Select a source, or drop a layer/group from this document. A multi-selection uses its active layer.";
            target.AddManipulator(new TargetDropManipulator(this, effect));
            root.Add(target);
            HelpBox status = SpriteEditorUI.AddHelpBox(root, string.Empty, HelpBoxMessageType.Info);
            bindings.Add(() =>
            {
                EnsureEffectTargetOptions(effect);
                bool choicesChanged = target.choices.Count != effectTargetLabels.Length;
                for (int i = 0; !choicesChanged && i < effectTargetLabels.Length; i++)
                    choicesChanged = target.choices[i] != effectTargetLabels[i];
                if (choicesChanged)
                    target.choices = new List<string>(effectTargetLabels);
                status.style.display = DisplayStyle.Flex;
                status.messageType = HelpBoxMessageType.Info;
                if (effect.inputMode == EffectInputMode.Previous)
                    status.text = "Uses the item directly below this effect. A group is read as the combined alpha of all visible descendants.";
                else if (string.IsNullOrEmpty(effect.TargetLayerId))
                {
                    status.text = "Select a source layer or group for this effect.";
                    status.messageType = HelpBoxMessageType.Warning;
                }
                else if (!compositor.IsUsableEffectTarget(effect, effect.TargetLayerId))
                {
                    status.text = "The selected target is missing or would create a cyclic effect dependency.";
                    status.messageType = HelpBoxMessageType.Error;
                }
                else if (compositor.FindLayer(effect.TargetLayerId) is GroupLayer)
                    status.text = "The selected group is read as the combined alpha of all visible descendant layers.";
                else
                    status.style.display = DisplayStyle.None;
            });
            bindings.Track(target, () =>
            {
                EnsureEffectTargetOptions(effect);
                return effectTargetLabels[FindEffectTargetIndex(effect.TargetLayerId)];
            });
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

        internal void Invalidate()
        {
            effectTargetIds = null;
            effectTargetLabels = null;
            effectTargetOptionsForLayerId = null;
            effectTargetOptionsForTargetId = null;
        }

        private sealed class TargetDropManipulator : PointerManipulator
        {
            private readonly EffectTargetSettingsView owner;
            private readonly TargetedLayerEffect effect;

            internal TargetDropManipulator(EffectTargetSettingsView owner, TargetedLayerEffect effect)
            {
                this.owner = owner;
                this.effect = effect;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<DragUpdatedEvent>(OnUpdated);
                target.RegisterCallback<DragPerformEvent>(OnPerform);
                target.RegisterCallback<DragLeaveEvent>(OnLeave);
                target.RegisterCallback<DragExitedEvent>(OnExited);
                target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                Clear();
                target.UnregisterCallback<DragUpdatedEvent>(OnUpdated);
                target.UnregisterCallback<DragPerformEvent>(OnPerform);
                target.UnregisterCallback<DragLeaveEvent>(OnLeave);
                target.UnregisterCallback<DragExitedEvent>(OnExited);
                target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            }

            private Layer GetSource()
            {
                Layer source = TextureCompositorWindow.GetDraggedLayerForDocument(owner.compositor);
                return target.enabledInHierarchy && source != null &&
                    owner.compositor.TryFindLayer(effect, out _, out _) &&
                    owner.compositor.IsUsableEffectTarget(effect, source.Id) ? source : null;
            }

            private void OnUpdated(DragUpdatedEvent evt)
            {
                bool valid = GetSource() != null;
                DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
                target.EnableInClassList("sprite-editor-effect-target--drop", valid);
                evt.StopImmediatePropagation();
            }

            private void OnPerform(DragPerformEvent evt)
            {
                evt.StopImmediatePropagation();
                Layer source = GetSource();
                Clear();
                if (source == null)
                    return;
                DragAndDrop.AcceptDrag();
                try
                {
                    owner.applyChange("Change Effect Target", () =>
                    {
                        effect.inputMode = EffectInputMode.Specific;
                        effect.TargetLayerId = source.Id;
                        owner.Invalidate();
                    });
                }
                finally
                {
                    TextureCompositorWindow.ClearDraggedLayerReference();
                }
            }

            private void Clear() => target.RemoveFromClassList("sprite-editor-effect-target--drop");
            private void OnLeave(DragLeaveEvent evt) => Clear();
            private void OnExited(DragExitedEvent evt) => Clear();
            private void OnDetach(DetachFromPanelEvent evt) => Clear();
        }
    }
}
