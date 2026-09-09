using System.Collections.Generic;

namespace DCFApixels.SpriteEditor
{
    internal static class LayerSelectionOperations
    {
        internal static List<Layer> Collect(List<Layer> tree, HashSet<Layer> selected, bool rootsOnly)
        {
            var result = new List<Layer>();
            void Visit(List<Layer> children)
            {
                foreach (Layer layer in children)
                {
                    if (layer == null) continue;
                    bool included = selected.Contains(layer);
                    if (included) result.Add(layer);
                    if (layer is GroupLayer group && (!included || !rootsOnly)) Visit(group.layers);
                }
            }
            Visit(tree);
            return result;
        }

        internal static bool Move(TextureCompositor document, List<Layer> roots, int direction, bool execute)
        {
            var selected = new HashSet<Layer>(roots);
            var containers = new HashSet<List<Layer>>();
            foreach (Layer layer in roots)
                if (document.TryFindLayer(layer, out List<Layer> container, out _)) containers.Add(container);
            bool moved = false;
            foreach (var container in containers)
            {
                int start = direction < 0 ? 1 : container.Count - 2;
                for (int i = start; i >= 0 && i < container.Count; i += direction < 0 ? 1 : -1)
                {
                    int adjacent = i + direction;
                    if (!selected.Contains(container[i]) || selected.Contains(container[adjacent])) continue;
                    if (!execute) return true;
                    (container[i], container[adjacent]) = (container[adjacent], container[i]);
                    moved = true;
                }
            }
            return moved;
        }

        internal readonly struct GroupMove
        {
            internal readonly Layer layer;
            internal readonly GroupLayer group;
            internal readonly List<Layer> source, destination;
            internal GroupMove(Layer layer, GroupLayer group, List<Layer> source, List<Layer> destination)
            { this.layer = layer; this.group = group; this.source = source; this.destination = destination; }
        }

        internal static List<GroupMove> PlanGroupMoves(TextureCompositor document, List<Layer> roots, bool into)
        {
            var moves = new List<GroupMove>();
            var selected = new HashSet<Layer>(roots);
            foreach (Layer layer in roots)
            {
                if (!document.TryFindLayer(layer, out List<Layer> container, out int index)) continue;
                if (into)
                {
                    int above = index - 1;
                    while (above >= 0 && selected.Contains(container[above])) above--;
                    if (above >= 0 && container[above] is GroupLayer group)
                        moves.Add(new GroupMove(layer, group, container, group.layers));
                }
                else if (document.TryFindParentGroup(container, out GroupLayer parent, out List<Layer> destination, out _))
                    moves.Add(new GroupMove(layer, parent, container, destination));
            }
            return moves;
        }

        internal static void ApplyGroupMoves(List<GroupMove> moves, bool into)
        {
            for (int step = 0; step < moves.Count; step++)
            {
                GroupMove move = moves[into ? step : moves.Count - 1 - step];
                int anchor = into ? move.destination.Count : move.destination.IndexOf(move.group) + 1;
                if ((!into && anchor == 0) || !move.source.Remove(move.layer)) continue;
                move.destination.Insert(anchor, move.layer);
            }
        }

        internal static List<Layer> Ungroup(TextureCompositor document, List<Layer> selected)
        {
            var result = new List<Layer>(selected);
            for (int i = selected.Count - 1; i >= 0; i--)
            {
                if (!(selected[i] is GroupLayer group) || !document.TryFindLayer(group, out List<Layer> container, out int index)) continue;
                container.RemoveAt(index);
                container.InsertRange(index, group.layers);
                result.Remove(group);
                foreach (Layer child in group.layers) if (!result.Contains(child)) result.Add(child);
            }
            return result;
        }
    }
}
