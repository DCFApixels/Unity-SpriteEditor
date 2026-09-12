using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        private const float GuideSnapPixels = 8f;
        private bool CanSnapPreviewGuides => !previewGuidesHidden && previewGuidesSnap && HasPreviewLayers &&
            previewGuidesDocument == compositor && previewGuides.Count > 0 && toolkitPreviewCanvas != null &&
            toolkitPreviewCanvas.PixelScale > .00001f;
        private float GuideSnapTolerance => GuideSnapPixels / toolkitPreviewCanvas.PixelScale;

        [System.NonSerialized] private int paintingGuideIndex = -1;
        [System.NonSerialized] private int paintingGuideRevision;
        private bool CanLockPaintingGuide => paintingLayer != null && IsPreviewPaintTool && CanSnapPreviewGuides &&
            paintingGuideRevision == previewGuidesRevision && paintingGuideIndex >= 0 && paintingGuideIndex < previewGuides.Count;

        private void CapturePaintingGuide(Vector2 position, bool disableSnap)
        {
            paintingGuideIndex = -1;
            if (disableSnap || !IsPreviewPaintTool || !CanSnapPreviewGuides) return;
            Rect image = toolkitPreviewCanvas.ImageRect;
            if (image.width <= 0f || image.height <= 0f) return;
            Vector2 canvasPoint = toolkitPreviewCanvas.ToCanvas(position);
            Vector2 point = new Vector2((canvasPoint.x - image.x) / image.width * compositor.width,
                (canvasPoint.y - image.y) / image.height * compositor.height);
            float distance = GuideSnapTolerance;
            for (int i = 0; i < previewGuides.Count; i++)
            {
                PreviewGuide guide = previewGuides[i];
                float delta = Mathf.Abs(guide.position - Vector2.Dot(point, guide.normal));
                if (delta > distance) continue;
                distance = delta;
                paintingGuideIndex = i;
            }
            paintingGuideRevision = previewGuidesRevision;
        }

        private Vector2 ProjectPaintingGuide(Vector2 position)
        {
            Rect image = toolkitPreviewCanvas.ImageRect;
            if (image.width <= 0f || image.height <= 0f) return position;
            Vector2 canvasPoint = toolkitPreviewCanvas.ToCanvas(position);
            Vector2 point = new Vector2((canvasPoint.x - image.x) / image.width * compositor.width,
                (canvasPoint.y - image.y) / image.height * compositor.height);
            PreviewGuide guide = previewGuides[paintingGuideIndex];
            point += guide.normal * (guide.position - Vector2.Dot(point, guide.normal));
            return toolkitPreviewCanvas.ToView(new Vector2(image.x + point.x / compositor.width * image.width,
                image.y + point.y / compositor.height * image.height));
        }

        private Vector2 GetPreviewPaintPosition(Vector2 position, bool shift, bool disableSnap, bool updateConstraint = true)
        {
            if (shift && !disableSnap && CanLockPaintingGuide)
            {
                if (updateConstraint) SetPaintingShift(shift);
                return ProjectPaintingGuide(position);
            }
            if (paintingLayer != null && updateConstraint) position = ConstrainPaintingPosition(position, shift);
            else if (paintingLayer != null && shift)
            {
                Rect rect = toolkitPreviewCanvas.ImageRect;
                Vector2 anchor = toolkitPreviewCanvas.ToView(new Vector2(rect.x + paintingAxisAnchor.x * rect.width,
                    rect.y + (1f - paintingAxisAnchor.y) * rect.height));
                position = paintingLockedAxis == 1 ? new Vector2(position.x, anchor.y) :
                    paintingLockedAxis == 2 ? new Vector2(anchor.x, position.y) : anchor;
            }
            if (disableSnap || !IsPreviewPaintTool || !CanSnapPreviewGuides) return position;
            Rect image = toolkitPreviewCanvas.ImageRect;
            if (image.width <= 0f || image.height <= 0f) return position;
            Vector2 canvasPoint = toolkitPreviewCanvas.ToCanvas(position);
            Vector2 point = new Vector2((canvasPoint.x - image.x) / image.width * compositor.width,
                (image.yMax - canvasPoint.y) / image.height * compositor.height);
            Vector2 snapped;
            if (paintingLayer != null && shift)
            {
                if (paintingLockedAxis == 0) return position;
                Vector2 right = previewViewport.ToCanvasDelta(Vector2.right);
                right.y = -right.y;
                Vector2 direction = paintingLockedAxis == 1 ? right : new Vector2(-right.y, right.x);
                snapped = SnapPreviewGuideResize(point, direction, false, right, point);
            }
            else snapped = SnapPreviewGuidePoint(point);
            if (snapped == point) return position;
            return toolkitPreviewCanvas.ToView(new Vector2(image.x + snapped.x / compositor.width * image.width,
                image.yMax - snapped.y / compositor.height * image.height));
        }

        private static bool GuideAxesParallel(Vector2 a, Vector2 b) =>
            Mathf.Abs(a.x * b.y - a.y * b.x) <= .0001f;

        private void GuideDocumentPlane(PreviewGuide guide, out Vector2 normal, out float position)
        {
            normal = new Vector2(guide.normal.x, -guide.normal.y);
            position = guide.position - guide.normal.y * compositor.height;
        }

        private Vector2 SnapPreviewGuidePoint(Vector2 point, bool axisAlignedOnly = false)
        {
            if (!CanSnapPreviewGuides) return point;
            float tolerance = GuideSnapTolerance;
            if (axisAlignedOnly)
            {
                float x = tolerance, y = tolerance;
                Vector2 result = point;
                foreach (PreviewGuide guide in previewGuides)
                {
                    GuideDocumentPlane(guide, out Vector2 n, out float d);
                    if (GuideAxesParallel(n, Vector2.right))
                    {
                        float delta = d / n.x - point.x;
                        if (Mathf.Abs(delta) <= x) { x = Mathf.Abs(delta); result.x = point.x + delta; }
                    }
                    else if (GuideAxesParallel(n, Vector2.up))
                    {
                        float delta = d / n.y - point.y;
                        if (Mathf.Abs(delta) <= y) { y = Mathf.Abs(delta); result.y = point.y + delta; }
                    }
                }
                return result;
            }

            int closest = -1;
            float distance = tolerance;
            Vector2 projected = point;
            for (int i = 0; i < previewGuides.Count; i++)
            {
                GuideDocumentPlane(previewGuides[i], out Vector2 n, out float d);
                float delta = d - Vector2.Dot(point, n);
                if (Mathf.Abs(delta) > distance) continue;
                distance = Mathf.Abs(delta); closest = i; projected = point + n * delta;
            }
            if (closest < 0) return point;
            GuideDocumentPlane(previewGuides[closest], out Vector2 first, out float firstD);
            Vector2 intersection = projected;
            float nearestIntersection = tolerance * tolerance;
            for (int i = 0; i < previewGuides.Count; i++)
            {
                if (i == closest) continue;
                GuideDocumentPlane(previewGuides[i], out Vector2 n, out float d);
                float determinant = first.x * n.y - first.y * n.x;
                if (Mathf.Abs(determinant) < .1f) continue;
                Vector2 candidate = new Vector2(firstD * n.y - first.y * d, first.x * d - firstD * n.x) / determinant;
                float squared = (candidate - point).sqrMagnitude;
                if (squared > nearestIntersection) continue;
                nearestIntersection = squared; intersection = candidate;
            }
            return intersection;
        }

        private Vector2 SnapPreviewGuideMove(Vector2 center, Vector2 axisX, Vector2 halfSize,
            Vector2 fallback, bool horizontal, bool vertical)
        {
            if (!CanSnapPreviewGuides) return fallback;
            Vector2 axisY = new Vector2(-axisX.y, axisX.x);
            Vector2 result = fallback;
            for (int axis = 0; axis < 2; axis++)
            {
                Vector2 direction = axis == 0 ? axisX : axisY;
                if ((!horizontal && Mathf.Abs(direction.x) > .0001f) ||
                    (!vertical && Mathf.Abs(direction.y) > .0001f)) continue;
                float previous = Vector2.Dot(fallback, direction);
                float nearest = previous == 0f ? GuideSnapTolerance : Mathf.Min(GuideSnapTolerance, Mathf.Abs(previous));
                float best = previous;
                foreach (PreviewGuide guide in previewGuides)
                {
                    GuideDocumentPlane(guide, out Vector2 n, out float d);
                    if (!GuideAxesParallel(n, direction)) continue;
                    float sign = Vector2.Dot(n, direction);
                    float offset = d / sign - Vector2.Dot(center, direction);
                    for (int edge = -1; edge <= 1; edge++)
                    {
                        float delta = offset - edge * halfSize[axis];
                        if (Mathf.Abs(delta) > nearest) continue;
                        nearest = Mathf.Abs(delta); best = delta;
                    }
                }
                result += direction * (best - previous);
            }
            return result;
        }

        private Vector2 SnapPreviewGuideResize(Vector2 point, Vector2 direction, bool free, Vector2 axisX, Vector2 fallback)
        {
            if (!CanSnapPreviewGuides) return fallback;
            if (free) return point + SnapPreviewGuideMove(point, axisX, Vector2.zero, fallback - point, true, true);
            Vector2 result = fallback;
            float nearest = fallback == point ? GuideSnapTolerance * GuideSnapTolerance :
                Mathf.Min(GuideSnapTolerance * GuideSnapTolerance, (fallback - point).sqrMagnitude);
            Vector2 axisY = new Vector2(-axisX.y, axisX.x);
            foreach (PreviewGuide guide in previewGuides)
            {
                GuideDocumentPlane(guide, out Vector2 n, out float d);
                if (!GuideAxesParallel(n, axisX) && !GuideAxesParallel(n, axisY)) continue;
                float denominator = Vector2.Dot(n, direction);
                if (Mathf.Abs(denominator) < .00001f) continue;
                Vector2 offset = direction * ((d - Vector2.Dot(n, point)) / denominator);
                float squared = offset.sqrMagnitude;
                if (squared > nearest) continue;
                nearest = squared; result = point + offset;
            }
            return result;
        }

        private float SnapPreviewGuideRotation(float rotation, bool includeCanvasAxes = false)
        {
            float nearest = 3f, result = rotation;
            if (includeCanvasAxes)
            {
                float target = Mathf.Round(rotation / 90f) * 90f;
                float delta = Mathf.Abs(target - rotation);
                if (delta <= nearest) { nearest = delta; result = target; }
            }
            if (!CanSnapPreviewGuides) return result;
            foreach (PreviewGuide guide in previewGuides)
            {
                GuideDocumentPlane(guide, out Vector2 n, out _);
                float angle = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg;
                float target = angle + Mathf.Round((rotation - angle) / 90f) * 90f;
                float delta = Mathf.Abs(target - rotation);
                if (delta > nearest) continue;
                nearest = delta; result = target;
            }
            return result;
        }

        private float SnapPreviewGuidePosition(PreviewGuide guide, int excluded)
        {
            if (!previewGuidesSnap || previewGuidesHidden || !HasPreviewLayers || toolkitPreviewCanvas == null ||
                toolkitPreviewCanvas.PixelScale <= .00001f) return guide.position;
            float best = guide.position, nearest = GuideSnapTolerance;
            void Consider(float position)
            {
                float distance = Mathf.Abs(position - guide.position);
                if (distance > nearest) return;
                nearest = distance; best = position;
            }
            if (GuideAxesParallel(guide.normal, Vector2.right))
                for (int i = 0; i <= 2; i++) Consider(guide.normal.x * compositor.width * i * .5f);
            else if (GuideAxesParallel(guide.normal, Vector2.up))
                for (int i = 0; i <= 2; i++) Consider(guide.normal.y * compositor.height * i * .5f);
            if (GetSelectedLayer() is Layer selected && selected.Behaviour != null && (!selected.IsGroup || PreviewFXParameter != null))
            {
                TextureTransform transform = CurrentPreviewTransform;
                float angle = transform.rotation * Mathf.Deg2Rad;
                float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
                Vector2 axisX = new Vector2(c, -s), axisY = new Vector2(s, c);
                bool x = GuideAxesParallel(guide.normal, axisX), y = GuideAxesParallel(guide.normal, axisY);
                if (x || y)
                {
                    Vector2 size = new Vector2(compositor.width, compositor.height);
                    Vector2 pivot = Vector2.Scale(transform.pivot, size);
                    Vector2 local = Vector2.Scale(size * .5f - pivot, transform.scale);
                    Vector2 center = pivot + transform.position + new Vector2(c * local.x - s * local.y, s * local.x + c * local.y);
                    center.y = size.y - center.y;
                    float extent = x ? Mathf.Abs(transform.scale.x) * size.x * .5f : Mathf.Abs(transform.scale.y) * size.y * .5f;
                    for (int edge = -1; edge <= 1; edge++) Consider(Vector2.Dot(guide.normal, center) + edge * extent);
                }
            }
            for (int i = 0; i < previewGuides.Count; i++)
            {
                if (i == excluded || !GuideAxesParallel(guide.normal, previewGuides[i].normal)) continue;
                Consider(previewGuides[i].position * Vector2.Dot(guide.normal, previewGuides[i].normal));
            }
            return best;
        }
    }
}
