import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const read = path => readFileSync(new URL('../src/' + path, import.meta.url), 'utf8');
const viewport = read('PreviewViewport.cs');
const ui = read('TextureCompositorWindow.UI.cs');
const zoom = read('TextureCompositorWindow.Zoom.cs');
const transform = read('TextureCompositorWindow.Transform.cs');
const selection = read('TextureCompositorWindow.AreaSelectionView.cs');

// Evaluate the actual scalar expressions from the C# rotation helpers, not a second matrix.
function deltaMethod(name) {
    const match = viewport.match(new RegExp(`${name}\\(Vector2 delta\\) => new Vector2\\(\\s*([^,]+), ([^;]+)\\);`));
    assert.ok(match, name);
    return new Function('cosine', 'sine', 'delta', `return [${match[1]}, ${match[2]}];`);
}
const forward = deltaMethod('ToViewDelta'), inverse = deltaMethod('ToCanvasDelta');
const close = (a, b, tolerance = 1e-7) => a.forEach((v, i) => assert.ok(Math.abs(v - b[i]) < tolerance, `${a} vs ${b}`));
const add = (a, b) => a.map((v, i) => v + b[i]);
const sub = (a, b) => a.map((v, i) => v - b[i]);
const mul = (a, b) => a.map((v, i) => v * b[i]);
const div = (a, b) => a.map((v, i) => v / b[i]);
let checks = 0;
for (const angle of [0, 17, 45, 90, 133, 179.9, -90, -178, 270, 1081]) {
    const c = Math.cos(angle * Math.PI / 180), s = Math.sin(angle * Math.PI / 180);
    const rotate = (fn, a) => fn(c, s, { x: a[0], y: a[1] });
    for (const extent of [[808, 408], [100, 2000], [1920, 1080]]) {
        const pivot = mul(extent, [.5, .5]);
        const toView = a => add(pivot, rotate(forward, sub(a, pivot)));
        const toCanvas = a => add(pivot, rotate(inverse, sub(a, pivot)));
        for (const point of [[0, 0], [203, 116], [-1000, 2000], pivot]) {
            close(toCanvas(toView(point)), point);
            const size = [672, 336], position = [-37, 51], nextSize = mul(size, [1.2, 1.2]);
            const anchor = toCanvas(point), uv = div(sub(anchor, position), size);
            // Same anchored-zoom and center formula as PreviewViewport.ZoomAt.
            const center = add(uv, div(sub(pivot, anchor), nextSize));
            const nextPosition = sub(pivot, mul(center, nextSize));
            close(toView(add(nextPosition, mul(uv, nextSize))), point);
            const pan = [37, -21];
            close(sub(toView(add(position, rotate(inverse, pan))), toView(position)), pan);
            // A rotated surface is positioned by its rotated center, then rotated locally.
            const surfaceCenter = add(position, mul(size, [.5, .5]));
            close(add(toView(surfaceCenter), rotate(forward, sub(point, surfaceCenter))), toView(point));
            checks += 4;
        }
        const corners = [[0, 0], [extent[0], 0], extent, [0, extent[1]]];
        const canvasCorners = corners.map(toCanvas);
        for (const [u, v] of [[0, 0], [.3, .7], [.5, .5], [1, 1]]) {
            // Tiled UVs are affine: a single full-viewport quad works at every angle.
            const interpolated = add(canvasCorners[0], add(
                mul(sub(canvasCorners[1], canvasCorners[0]), [u, u]),
                mul(sub(canvasCorners[3], canvasCorners[0]), [v, v])));
            close(interpolated, toCanvas(mul(extent, [u, v])));
            checks++;
        }
    }
}
assert.match(viewport, /anchor = ToCanvas\(viewport, anchor\)/);
assert.match(viewport, /selectionCenter = ToCanvas\(viewport, selection.center\)/);
assert.match(viewport, /delta = ToCanvasDelta\(delta\)/);
assert.match(viewport, /Mathf.Round\(degrees \/ 90f\) \* 90f/);
assert.match(viewport, /Mathf.Abs\(degrees - nearest\) <= 3f/);
assert.match(zoom, /rotating = panning && evt.shiftKey/);
assert.match(zoom, /new FloatField\("Angle °"\)\s*\{\s*isDelayed = true/);
assert.match(zoom, /SetPreviewRotation\(evt.newValue\);\s*previewRotationField.SetValueWithoutNotify\(previewViewport.Rotation\)/);
assert.match(zoom, /SetViewRotation\(degrees, snap: false\)/);
assert.match(zoom, /!HasPreviewLayers \|\| float.IsNaN\(degrees\) \|\| float.IsInfinity\(degrees\)/);
assert.match(zoom, /previewRotationField.SetValueWithoutNotify\(displayedPreviewRotation\)/);
const styles = read('SpriteEditorSplitView.uss');
assert.match(styles, /\.sprite-editor-view-field\s*\{\s*width: 130px;/);
assert.match(styles, /\.sprite-editor-view-field > \.unity-base-field__label\s*\{\s*min-width: 0;/);
assert.match(zoom, /new FloatField\("Zoom %"\)\s*\{\s*isDelayed = true/);
assert.match(zoom, /SetPreviewZoomPercent\(evt.newValue\);\s*previewZoomPercent.SetValueWithoutNotify\(toolkitPreviewCanvas.PixelScale \* 100f\)/);
assert.match(zoom, /percent <= 0f \|\| float.IsNaN\(percent\) \|\| float.IsInfinity\(percent\)/);
assert.match(zoom, /ZoomAt\(toolkitPreviewCanvas.contentRect.center, percent \/ 100f\)/);
assert.match(zoom, /previewZoomPercent.SetValueWithoutNotify\(scale \* 100f\)/);
assert.match(zoom, /freeRotation \+= Vector2.SignedAngle\(from, to\)/);
assert.match(zoom, /SetViewRotation\(freeRotation, !disableSnap\)/);
assert.match(zoom, /RotateTo\(point, evt.ctrlKey\)/);
assert.match(zoom, /if \(!owner.HasPreviewLayers/);
assert.match(zoom, /pointerId = -1;\s*if \(captured >= 0/);
for (const event of ['Down', 'Move', 'Up'])
    for (const action of ['Register', 'Unregister'])
        assert.ok(zoom.includes(`${action}Callback<Pointer${event}Event>(On${event}, TrickleDown.TrickleDown)`));
assert.match(ui, /presentedRotation == viewport.Rotation/);
assert.match(ui, /PositionSurface\(checker, presentationRect, !tiled\)/);
assert.match(ui, /PositionSurface\(image, ImageRect, true\)/);
assert.match(ui, /rect.position = ToView\(rect.center\) - rect.size \* 0.5f/);
assert.match(ui, /Vector2 canvasCursor = ToCanvas\(cursorPosition\)/);
assert.match(ui, /x = viewport.ToViewDelta\(x\)/);
assert.match(ui, /y = viewport.ToViewDelta\(y\)/);
assert.match(ui, /position = toolkitPreviewCanvas.ToCanvas\(position\)/);
assert.match(read('TextureCompositorWindow.cs'), /mousePosition = toolkitPreviewCanvas.ToCanvas\(mousePosition\)/);
assert.match(read('TextureCompositorWindow.Tiling.cs'), /ImageRect.Contains\(toolkitPreviewCanvas.ToCanvas\(position\)\)/);
assert.match(transform, /lastPointerPosition = point;\s*point = owner.toolkitPreviewCanvas.ToCanvas\(point\)/);
assert.match(selection, /point = owner.toolkitPreviewCanvas.ToCanvas\(point\)/);
assert.match(selection, /Rect bounds = owner.toolkitPreviewCanvas.VisibleCanvasBounds/);
assert.match(selection, /PreviewPoint\(new Vector2\(gesture.Current.x, gesture.Start.y\), image\)/);
assert.ok(!/RenderTexture|Undo\.|SetDirty|SerializeField/.test(viewport));
console.log(`Preview rotation: ${checks} geometry checks and integration source checks passed (Unity/UI not executed).`);
