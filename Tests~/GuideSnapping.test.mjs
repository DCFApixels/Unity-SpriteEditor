import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
const read = name => readFileSync(new URL(`../src/${name}`, import.meta.url), 'utf8');
const src = read('TextureCompositorWindow.GuideSnapping.cs');
class V {
    constructor(x, y) { this.x = x; this.y = y; }
    get 0() { return this.x; } get 1() { return this.y; }
    get sqrMagnitude() { return this.x * this.x + this.y * this.y; }
}
const add = (a, b) => new V(a.x + b.x, a.y + b.y);
const sub = (a, b) => new V(a.x - b.x, a.y - b.y);
const mul = (a, b) => new V(a.x * b, a.y * b);
const dot = (a, b) => a.x * b.x + a.y * b.y;
const axis = degrees => new V(Math.cos(degrees * Math.PI / 180), Math.sin(degrees * Math.PI / 180));
const close = (a, b) => assert.ok(Math.abs(a - b) < 1e-6, `${a} != ${b}`);
const closeV = (a, b) => { close(a.x, b.x); close(a.y, b.y); };
const parallelExpression = src.match(/GuideAxesParallel\(Vector2 a, Vector2 b\) =>\s*([^;]+);/)[1];
const parallel = new Function('a', 'b', `return ${parallelExpression.replace('Mathf.Abs', 'Math.abs').replace(/(\d)f\b/g, '$1')};`);
function body(name) {
    const signature = src.search(new RegExp(`private (?:Vector2|float|bool|void) ${name}\\(`));
    assert.ok(signature >= 0, name);
    const start = src.indexOf('{', signature);
    let level = 1, end = start + 1;
    for (; level; end++) { if (src[end] === '{') level++; if (src[end] === '}') level--; }
    return src.slice(start + 1, end - 1);
}
function compile(name, args) {
    let code = body(name)
        .replace(/foreach \(PreviewGuide guide in previewGuides\)/g, 'for (const guide of previewGuides)')
        .replace(/GuideDocumentPlane\(([^;]+?), out Vector2 (\w+), out float (\w+)\);/g, 'let [$2, $3] = plane($1);')
        .replace(/GuideDocumentPlane\(guide, out Vector2 n, out _\);/g, 'let [n] = plane(guide);')
        .replace('return point + SnapPreviewGuideMove(point, axisX, Vector2.zero, fallback - point, true, true)', 'return add(point, snapMove(point, axisX, new V(0, 0), sub(fallback, point), true, true))')
        .replace(/\b(?:float|Vector2|int) (\w+)/g, 'let $1')
        .replace(/new Vector2/g, 'new V').replace(/Vector2.Dot/g, 'dot').replace(/GuideAxesParallel/g, 'parallel')
        .replace(/Mathf.Abs/g, 'Math.abs').replace(/Mathf.Min/g, 'Math.min')
        .replace(/Mathf.Atan2/g, 'Math.atan2').replace(/Mathf.Round/g, 'Math.round').replace(/Mathf.Rad2Deg/g, '(180 / Math.PI)')
        .replace(/previewGuides.Count/g, 'previewGuides.length').replace(/(\d)f\b/g, '$1')
        .replace('Vector2.right', 'new V(1, 0)').replace('Vector2.up', 'new V(0, 1)')
        .replace('projected = point + n * delta', 'projected = add(point, mul(n, delta))')
        .replace('new V(firstD * n.y - first.y * d, first.x * d - firstD * n.x) / determinant', 'mul(new V(firstD * n.y - first.y * d, first.x * d - firstD * n.x), 1 / determinant)')
        .replace('(candidate - point).sqrMagnitude', 'sub(candidate, point).sqrMagnitude')
        .replace('Vector2 result = fallback', 'let result = fallback')
        .replace('result += direction * (best - previous)', 'result = add(result, mul(direction, best - previous))')
        .replace('fallback == point', 'sub(fallback, point).sqrMagnitude < 1e-10')
        .replace('(fallback - point).sqrMagnitude', 'sub(fallback, point).sqrMagnitude')
        .replace('direction * ((d - dot(n, point)) / denominator)', 'mul(direction, ((d - dot(n, point)) / denominator))')
        .replace('result = point + offset', 'result = add(point, offset)');
    const fn = new Function(...args, 'previewGuides', 'GuideSnapTolerance', 'CanSnapPreviewGuides', 'plane', 'V', 'dot', 'parallel', 'add', 'sub', 'mul', 'snapMove', code);
    return (values, guides, tolerance = 8, enabled = true) => fn(...args.map((_, i) => values[i]), guides, tolerance, enabled,
        g => [g.n, g.d], V, dot, parallel, add, sub, mul, (...args) => moveSnap(args, guides, tolerance, enabled));
}
const pointSnap = compile('SnapPreviewGuidePoint', ['point', 'axisAlignedOnly']);
const moveSnap = compile('SnapPreviewGuideMove', ['center', 'axisX', 'halfSize', 'fallback', 'horizontal', 'vertical']);
const resizeSnap = compile('SnapPreviewGuideResize', ['point', 'direction', 'free', 'axisX', 'fallback']);
const rotationSnap = compile('SnapPreviewGuideRotation', ['rotation', 'includeCanvasAxes']);
const paintBody = body('GetPreviewPaintPosition')
    .replace(/\b(?:Rect|Vector2) (\w+)/g, 'let $1').replace(/new Vector2/g, 'new V')
    .replace('Vector2.right', 'new V(1, 0)').replace(/(\d)f\b/g, '$1')
    .replace('snapped == point', '(snapped.x === point.x && snapped.y === point.y)');
const paintPosition = new Function('state', 'position', 'shift', 'disableSnap', 'updateConstraint', 'V', `with (state) { ${paintBody} }`);
function guideStrokeMethod(name) {
    const code = body(name)
        .replace(/\b(?:Rect|Vector2|PreviewGuide|float|int) (\w+)/g, 'let $1')
        .replace(/new Vector2/g, 'new V').replace(/Vector2.Dot/g, 'dot')
        .replace(/Mathf.Abs/g, 'Math.abs').replace(/previewGuides.Count/g, 'previewGuides.length')
        .replace(/(\d)f\b/g, '$1')
        .replace('point += guide.normal * (guide.position - dot(point, guide.normal))',
            'point = add(point, mul(guide.normal, guide.position - dot(point, guide.normal)))');
    const fn = new Function('state', 'position', 'disableSnap', 'V', 'dot', 'add', 'mul', `with (state) { ${code} }`);
    return (state, position, disableSnap = false) => fn(state, position, disableSnap, V, dot, add, mul);
}
const captureGuide = guideStrokeMethod('CapturePaintingGuide');
const projectGuide = guideStrokeMethod('ProjectPaintingGuide');
const canLock = new Function('state', `with (state) { return ${src.match(/CanLockPaintingGuide =>([^;]+);/)[1]
    .replace(/previewGuides.Count/g, 'previewGuides.length')}; }`);
let checks = 0;
for (const rotation of [0, 17, 45, 90, -90, 178]) for (const scale of [.25, 1, 8]) {
    const c = Math.cos(rotation * Math.PI / 180), s = Math.sin(rotation * Math.PI / 180);
    const rotate = p => new V(c * p.x - s * p.y, s * p.x + c * p.y);
    const inverse = p => new V(c * p.x + s * p.y, -s * p.x + c * p.y);
    const center = new V(400, 300), image = { x: -100, y: 20, width: 512 * scale, height: 256 * scale };
    image.yMax = image.y + image.height;
    const toView = p => add(center, rotate(sub(p, center)));
    const toCanvas = p => add(center, inverse(sub(p, center)));
    const screen = p => toView(new V(image.x + p.x * scale, image.yMax - p.y * scale));
    for (const direction of [0, 31, 90, 137]) {
        const n = axis(direction), guide = { n, d: 100 };
        const p = add(mul(n, 100 + 3 / scale), mul(new V(-n.y, n.x), 50));
        const expected = sub(p, mul(n, 3 / scale));
        let constraints = 0;
        const state = { paintingLayer: null, IsPreviewPaintTool: true, CanSnapPreviewGuides: true,
            paintingGuideIndex: -1, paintingGuideRevision: 0, previewGuidesRevision: 1,
            previewGuides: [{ normal: new V(n.x, -n.y), position: guide.d - n.y * 256 }],
            GuideSnapTolerance: 8 / scale,
            get CanLockPaintingGuide() { return canLock(this); },
            ProjectPaintingGuide(p) { return projectGuide(this, p); },
            SetPaintingShift() { constraints++; },
            compositor: { width: 512, height: 256 }, paintingLockedAxis: 0, paintingAxisAnchor: new V(0, 0),
            toolkitPreviewCanvas: { ImageRect: image, ToCanvas: toCanvas, ToView: toView },
            previewViewport: { ToCanvasDelta: inverse },
            ConstrainPaintingPosition(p) { constraints++; return p; },
            SnapPreviewGuidePoint: p => pointSnap([p, false], [guide], 8 / scale),
            SnapPreviewGuideResize: (...args) => resizeSnap(args, [guide], 8 / scale) };
        closeV(paintPosition(state, screen(p), false, false, true, V), screen(expected));
        closeV(paintPosition(state, screen(p), false, false, false, V), screen(expected));
        closeV(paintPosition(state, screen(p), false, true, true, V), screen(p));
        state.CanSnapPreviewGuides = false;
        closeV(paintPosition(state, screen(p), false, false, true, V), screen(p));
        state.CanSnapPreviewGuides = true; state.IsPreviewPaintTool = false;
        closeV(paintPosition(state, screen(p), false, false, true, V), screen(p));
        state.IsPreviewPaintTool = true; state.paintingLayer = {};
        paintPosition(state, screen(p), false, false, false, V);
        assert.equal(constraints, 0, 'Cursor rendering must not mutate the Shift axis lock');
        paintPosition(state, screen(p), false, false, true, V);
        assert.equal(constraints, 1, 'Stroke input still applies its Shift constraint');
        checks += 7;
        captureGuide(state, screen(p));
        assert.equal(state.paintingGuideIndex, 0);
        const far = add(expected, add(mul(n, 300 / scale), mul(new V(-n.y, n.x), 700)));
        const onGuide = sub(far, mul(n, 300 / scale));
        closeV(paintPosition(state, screen(far), true, false, true, V), screen(onGuide));
        assert.equal(constraints, 2, 'Guide lock updates Shift without choosing a screen axis');
        closeV(paintPosition(state, screen(far), true, false, false, V), screen(onGuide));
        assert.equal(constraints, 2, 'Guide-locked cursor rendering is read-only');
        closeV(paintPosition(state, screen(far), false, false, true, V), screen(far));
        closeV(paintPosition(state, screen(far), true, false, true, V), screen(onGuide));
        assert.equal(state.paintingGuideIndex, 0, 'Release/re-press retains the original guide');
        const crossing = { normal: new V(n.y, n.x), position: dot(new V(far.x, 256 - far.y), new V(n.y, n.x)) };
        state.previewGuides.push(crossing);
        closeV(paintPosition(state, screen(far), true, false, true, V), screen(onGuide));
        assert.equal(state.paintingGuideIndex, 0, 'A closer guide never steals the stroke lock');
        state.previewGuides.pop();
        state.paintingLockedAxis = 1;
        closeV(paintPosition(state, screen(far), true, true, true, V), screen(far));
        state.CanSnapPreviewGuides = false;
        assert.equal(state.CanLockPaintingGuide, false, 'Hidden/disabled guides do not lock');
        state.CanSnapPreviewGuides = true;
        state.previewGuidesRevision++;
        assert.equal(state.CanLockPaintingGuide, false, 'Changed guide lists invalidate the lock');
        captureGuide(state, screen(p), true);
        assert.equal(state.paintingGuideIndex, -1, 'Ctrl at stroke start does not reserve a guide');
        captureGuide(state, screen(far));
        assert.equal(state.paintingGuideIndex, -1, 'Starting outside snap range does not reserve a guide');
        captureGuide(state, screen(p));
        state.paintingLayer = null;
        assert.equal(state.CanLockPaintingGuide, false, 'Hover cannot continue a finished stroke lock');
        checks += 17;
    }
}
for (let rotation = -180; rotation <= 180; rotation += 15) {
    const x = axis(rotation), y = new V(-x.y, x.x), center = new V(120, 180);
    for (const sign of [-1, 1]) for (const edge of [-1, 0, 1]) for (const tolerance of [.8, 8, 40]) {
        const delta = tolerance * .5;
        const g = { n: mul(x, sign), d: sign * (dot(center, x) + edge * 300 + delta) };
        closeV(moveSnap([center, x, new V(300, 500), new V(0, 0), true, true], [g], tolerance), mul(x, delta));
        const wrong = { n: axis(rotation + 12), d: dot(center, axis(rotation + 12)) + delta };
        closeV(moveSnap([center, x, new V(30, 50), new V(0, 0), true, true], [wrong], tolerance), new V(0, 0));
        const perpendicular = { n: y, d: dot(center, y) + 500 - delta };
        closeV(moveSnap([center, x, new V(300, 500), new V(0, 0), true, true], [perpendicular], tolerance), mul(y, -delta));
        const p = add(mul(x, 100), mul(y, 40));
        const line = { n: mul(x, sign), d: sign * (100 + delta) };
        closeV(pointSnap([p, false], [line], tolerance), add(p, mul(x, delta)));
        closeV(pointSnap([p, false], [line], tolerance, false), p);
        if (!parallel(x, new V(1, 0)) && !parallel(x, new V(0, 1)))
            closeV(pointSnap([p, true], [line], tolerance), p);
        const crossing = { n: y, d: 40 + delta };
        closeV(pointSnap([p, false], [line, crossing], tolerance), add(p, add(mul(x, delta), mul(y, delta))));
        checks += 7;
    }
}
for (let angle = -180; angle <= 180; angle += 15) {
    const x = axis(angle), y = new V(-x.y, x.x), p = new V(300, 250);
    const g = { n: x, d: dot(x, p) + 2 };
    closeV(resizeSnap([p, x, false, x, p], [g]), add(p, mul(x, 2)));
    closeV(resizeSnap([p, y, false, x, p], [g]), p);
    const cornerDirection = add(mul(x, 2), mul(y, 3));
    closeV(resizeSnap([p, cornerDirection, false, x, p], [g]), add(p, cornerDirection));
    const across = { n: y, d: dot(y, p) - 3 };
    closeV(resizeSnap([p, x, true, x, p], [g, across]), add(p, add(mul(x, 2), mul(y, -3))));
    const oblique = { n: axis(angle + 10), d: dot(axis(angle + 10), p) + 2 };
    closeV(resizeSnap([p, x, false, x, p], [oblique]), p);
    const distant = { n: x, d: dot(x, p) + 80 };
    closeV(resizeSnap([p, x, false, x, p], [distant]), p);
    const fallback = add(p, x);
    closeV(resizeSnap([p, x, false, x, fallback], [g]), fallback);
    for (const turns of [-4, -1, 0, 1, 4]) for (const quarter of [0, 90, 180, 270]) {
        const base = angle + turns * 360 + quarter;
        close(rotationSnap([base + 2], [g]), base);
        close(rotationSnap([base - 2], [g]), base);
        close(rotationSnap([base + 4], [g]), base + 4);
        close(rotationSnap([base + 2], [g], 8, false), base + 2);
        checks += 4;
    }
    checks += 7;
}
closeV(pointSnap([new V(98, 201), true], [{ n: new V(1, 0), d: 100 }, { n: new V(0, -1), d: -200 }]), new V(100, 200));
for (const normalAngle of [0, 17, -31, 45, 90, 133, 179.9]) {
    const normal = axis(normalAngle);
    const guide = { n: new V(normal.x, -normal.y), d: 0 };
    for (const turns of [-4, -1, 0, 1, 4]) for (const quarter of [0, 90, 180, 270]) {
        const target = -normalAngle + turns * 360 + quarter;
        for (const offset of [-2, 0, 2]) {
            const input = target + offset;
            const result = rotationSnap([input, true], [guide]);
            const base = Math.round(input / 90) * 90;
            const expected = Math.abs(base - input) < Math.abs(offset) ? base : target;
            close(result, expected);
            if (expected === target) {
                const screenAngle = (normalAngle + result) * Math.PI / 180;
                assert.ok(Math.min(Math.abs(Math.cos(screenAngle)), Math.abs(Math.sin(screenAngle))) < 1e-6,
                    'The guide becomes horizontal or vertical in the rotated view');
            }
            const disabledExpected = Math.abs(base - input) <= 3 ? base : input;
            close(rotationSnap([input, true], [guide], 8, false), disabledExpected);
            checks += 3;
        }
    }
}
close(rotationSnap([1, true], [{ n: axis(2), d: 0 }]), 2);
close(rotationSnap([2, true], [{ n: axis(2), d: 0 }]), 2);
close(rotationSnap([.5, true], [{ n: axis(2), d: 0 }]), 0);
close(rotationSnap([22, true], [{ n: axis(17), d: 0 }]), 22);
close(rotationSnap([88, true], []), 90);
closeV(moveSnap([new V(100, 100), axis(45), new V(0, 0), new V(0, 0), true, false],
    [{ n: axis(45), d: dot(new V(100, 100), axis(45)) + 2 }]), new V(0, 0));
assert.match(src, /position = guide.position - guide.normal.y \* compositor.height;/);
assert.match(src, /normal = new Vector2\(guide.normal.x, -guide.normal.y\)/);
assert.match(src, /Mathf.Abs\(determinant\) < .1f/);
assert.match(src, /if \(!GuideAxesParallel\(n, axisX\) && !GuideAxesParallel\(n, axisY\)\) continue;/);
assert.match(src, /GuideSnapPixels \/ toolkitPreviewCanvas.PixelScale/);
assert.match(src, /!previewGuidesHidden && previewGuidesSnap/);
const transform = read('TextureCompositorWindow.Transform.cs');
const ui = read('TextureCompositorWindow.UI.cs');
assert.equal((ui.match(/GetPreviewPaintPosition\(evt.localPosition, evt.shiftKey, evt.ctrlKey\)/g) ?? []).length, 3, 'Down, Move and Up share snapping');
assert.match(ui, /GetPreviewPaintPosition\(localPosition, paintingShiftHeld, previewPointerControl, updateConstraint: false\)/);
assert.ok(!ui.includes('UpdatePreviewCursor(paintPosition'), 'Keep the raw cursor position to avoid double snapping and a sticky Ctrl bypass');
assert.match(ui, /paintingAxisPointerAnchor = previewPointerPosition/);
assert.match(ui, /CapturePaintingGuide\(evt.localPosition, evt.ctrlKey\);\s*if \(!TryBeginPreviewStroke/);
assert.equal((ui.match(/CapturePaintingGuide\(/g) ?? []).length, 1, 'Capture only at stroke start, never on Move or hover');
assert.match(read('TextureCompositorWindow.cs'), /paintingPointerMoved = false;\s*paintingGuideIndex = -1;/);
for (const name of ['SnapPreviewGuidePoint', 'SnapPreviewGuideMove', 'SnapPreviewGuideResize', 'SnapPreviewGuideRotation'])
    assert.ok(transform.includes(`owner.${name}(`));
assert.match(transform, /else if \(!disableSnap\)\s*next.rotation = owner.SnapPreviewGuideRotation/);
const selection = read('TextureCompositorWindow.AreaSelectionView.cs');
assert.equal((selection.match(/CanvasPoint\(evt.localPosition, evt.ctrlKey\)/g) ?? []).length, 3);
assert.match(selection, /disableSnap \? documentPoint : owner.SnapPreviewGuidePoint\(documentPoint, owner.previewTool == PreviewTool.RectangleSelect\)/);
const commands = read('TextureCompositorWindow.GuideCommands.cs');
function commandBody(name) {
    const start = commands.indexOf('{', commands.indexOf(` ${name}(`));
    let level = 1, end = start + 1;
    for (; level; end++) { if (commands[end] === '{') level++; if (commands[end] === '}') level--; }
    return commands.slice(start + 1, end - 1);
}
class List extends Array {
    get Count() { return this.length; }
    Add(value) { this.push(value); }
    AddRange(values) { this.push(...values); }
    RemoveAt(index) { this.splice(index, 1); }
    Clear() { this.length = 0; }
    ToArray() { return Array.from(this); }
}
const remember = new Function('state', `with (state) { ${commandBody('RememberPreviewGuides')} }`);
const restore = new Function('state', 'redo', `with (state) { ${commandBody('RestorePreviewGuides')} }`);
const state = { previewGuides: new List(), previewGuideUndo: new List(), previewGuideRedo: new List(),
    previewGuidesLocked: false, previewGuidesRevision: 0, selectedPreviewGuide: -1,
    previewGuideManipulator: { Cancel() {} }, RefreshPreviewGuides() {} };
for (let i = 0; i < 70; i++) { remember(state); state.previewGuides.Clear(); state.previewGuides.Add(i); }
assert.equal(state.previewGuideUndo.Count, 64);
restore(state, false); assert.deepEqual(state.previewGuides.ToArray(), [68]);
restore(state, true); assert.deepEqual(state.previewGuides.ToArray(), [69]);
restore(state, false); remember(state); state.previewGuides[0] = 99;
assert.equal(state.previewGuideRedo.Count, 0, 'A new edit invalidates the redo branch');
state.previewGuidesLocked = true;
restore(state, false); assert.equal(state.previewGuides[0], 99, 'Lock protects guide history too');
state.previewGuidesLocked = false;
while (state.previewGuideUndo.Count) restore(state, false);
const oldest = state.previewGuides.ToArray();
restore(state, false); assert.deepEqual(state.previewGuides.ToArray(), oldest, 'Empty history is a no-op');
assert.equal(state.previewGuideRedo.Count, 64);
for (const text of ['Show Guides', 'Lock Guides', 'Snap to Guides', 'Delete Guide', 'Duplicate Guide', 'Edit Guide', 'Undo Guide Change', 'Redo Guide Change', 'Clear Guides'])
    assert.ok(commands.includes(text));
assert.match(commands, /previewGuideUndo.Count == 64/);
assert.match(commands, /previewGuideRedo.Clear\(\)/);
assert.match(commands, /owner.previewGuidesRevision != revision/);
assert.match(commands, /focusedElement != toolkitPreviewCanvas/);
assert.ok(!/\bUndo\.|RenderTexture|MarkChanged|SetDirty/.test(commands), 'Guide history does not snapshot or dirty the compositor');
console.log(`Guide snapping: ${checks} extracted geometry cases and management/integration source checks passed (Unity UI not executed).`);
