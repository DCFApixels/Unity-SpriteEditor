using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        [NonSerialized] private LayerDragAutoScrollManipulator layerDragAutoScroll;

        private sealed class LayerDragAutoScrollManipulator : PointerManipulator
        {
            private readonly TextureCompositorWindow owner;
            private readonly ScrollView scroll;
            private IVisualElementScheduledItem timer;
            private Vector2 pointer;
            private double lastTick;
            private bool running;

            internal LayerDragAutoScrollManipulator(TextureCompositorWindow owner, ScrollView scroll)
            {
                this.owner = owner;
                this.scroll = scroll;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
                target.RegisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
                target.RegisterCallback<DragExitedEvent>(OnDragExited, TrickleDown.TrickleDown);
                target.RegisterCallback<DragLeaveEvent>(OnDragLeave);
                target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                Stop();
                timer = null;
                target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
                target.UnregisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
                target.UnregisterCallback<DragExitedEvent>(OnDragExited, TrickleDown.TrickleDown);
                target.UnregisterCallback<DragLeaveEvent>(OnDragLeave);
                target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            }

            private void OnDragUpdated(DragUpdatedEvent evt)
            {
                pointer = evt.mousePosition;
                if (owner.GetDraggedLayer() == null || EdgeSpeed(scroll.contentViewport.worldBound, pointer) == 0f)
                {
                    Stop();
                    return;
                }
                if (running) return;
                running = true;
                lastTick = EditorApplication.timeSinceStartup;
                if (timer == null) timer = target.schedule.Execute(Tick).Every(16);
                else timer.Resume();
            }

            internal void Stop()
            {
                running = false;
                timer?.Pause();
            }

            private void OnDragPerform(DragPerformEvent evt) => Stop();
            private void OnDragExited(DragExitedEvent evt) => Stop();
            private void OnDragLeave(DragLeaveEvent evt) { if (evt.target == target) Stop(); }
            private void OnDetach(DetachFromPanelEvent evt) { if (evt.target == target) Stop(); }

            internal static float EdgeSpeed(Rect viewport, Vector2 point)
            {
                if (viewport.width <= 0f || viewport.height <= 0f || !viewport.Contains(point)) return 0f;
                float band = Mathf.Min(32f, viewport.height * .25f);
                float top = point.y - viewport.yMin, bottom = viewport.yMax - point.y;
                if (top < band) return -480f * (1f - top / band);
                if (bottom < band) return 480f * (1f - bottom / band);
                return 0f;
            }

            private void Tick()
            {
                if (!running || target.panel == null || owner.GetDraggedLayer() == null)
                {
                    Stop();
                    return;
                }
                float speed = EdgeSpeed(scroll.contentViewport.worldBound, pointer);
                if (speed == 0f) { Stop(); return; }
                double now = EditorApplication.timeSinceStartup;
                float seconds = Mathf.Clamp((float)(now - lastTick), 0f, .05f);
                lastTick = now;
                RefreshDropIndicator();
                Vector2 offset = scroll.scrollOffset;
                offset.y = Mathf.Clamp(offset.y + speed * seconds,
                    scroll.verticalScroller.lowValue, scroll.verticalScroller.highValue);
                if (offset != scroll.scrollOffset) scroll.scrollOffset = offset;
            }

            private void RefreshDropIndicator()
            {
                VisualElement picked = target.panel.Pick(pointer);
                if (picked == null || !scroll.contentViewport.Contains(picked)) return;
                for (VisualElement element = picked; element != null && element != target; element = element.parent)
                {
                    if (element.ClassListContains("sprite-editor-layer-row") && element.userData is string id)
                    {
                        Layer layer = owner.compositor.FindLayer(id);
                        if (layer != null && owner.compositor.TryFindLayer(layer, out List<Layer> container, out int index) &&
                            owner.TryGetToolkitDrop(element, pointer, layer, container, index,
                                out _, out _, out _, out GroupLayer group, out bool before))
                        {
                            owner.SetToolkitDropIndicator(element, group != null, before, 0);
                            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        }
                        else
                        {
                            owner.ClearToolkitDropIndicator();
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        }
                        return;
                    }
                    if (element.userData is List<Layer> destination)
                    {
                        if (owner.CanDropLayer(owner.GetDraggedLayer(), destination, destination.Count))
                        {
                            owner.SetToolkitDropIndicator(element, false, true, 0);
                            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        }
                        else
                        {
                            owner.ClearToolkitDropIndicator();
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        }
                        return;
                    }
                }
                owner.ClearToolkitDropIndicator();
            }
        }
    }
}
