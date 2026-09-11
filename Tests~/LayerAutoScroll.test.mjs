// Scalar/source checks only. Does not compile or open Unity.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
const read = path => readFileSync(new URL('../' + path, import.meta.url), 'utf8').replace(/\r\n/g, '\n');
const source = read('src/TextureCompositorWindow.LayerAutoScroll.cs');
const body = source.match(/static float EdgeSpeed\(Rect viewport, Vector2 point\)\s*\{([^}]+)\}/)[1]
  .replace(/float /g, 'let ').replace(/(\d)f\b/g, '$1').replace(/Mathf.Min/g, 'Math.min');
const speed = new Function('viewport', 'point', body);
function rect(x, y, width, height) {
  return { width, height, yMin:y, yMax:y+height,
    Contains: p => p.x >= x && p.x < x+width && p.y >= y && p.y < y+height };
}
const view = rect(100,200,300,400);
assert.equal(speed(view, {x:150,y:200}), -480);
assert.equal(speed(view, {x:150,y:216}), -240);
assert.equal(speed(view, {x:150,y:232}), 0);
assert.equal(speed(view, {x:150,y:400}), 0);
assert.equal(speed(view, {x:150,y:568}), 0);
assert.equal(speed(view, {x:150,y:584}), 240);
assert.equal(speed(view, {x:150,y:599}), 465);
for (const p of [{x:99,y:201},{x:400,y:599},{x:150,y:199},{x:150,y:600}])
  assert.equal(speed(view,p),0,'No scrolling over header/footer/outside list');
for (const height of [0,1,20,64,400]) {
  const r=rect(0,0,100,height);
  assert.equal(speed(r,{x:50,y:height/2}),0);
  for(let i=0;i<100;i++) assert.ok(Math.abs(speed(r,{x:50,y:height*i/100}))<=480);
}
for (const event of ['DragUpdatedEvent','DragPerformEvent','DragExitedEvent']) {
  assert.ok(source.includes(`RegisterCallback<${event}>`));
  assert.ok(source.includes(`UnregisterCallback<${event}>`));
}
assert.match(source,/RegisterCallback<DragUpdatedEvent>\(OnDragUpdated, TrickleDown.TrickleDown\)/);
assert.ok(source.includes('target.schedule.Execute(Tick).Every(16)'));
assert.ok(source.includes('timer?.Pause()'));
assert.ok(source.includes('owner.GetDraggedLayer() == null'));
assert.ok(source.includes('target.panel.Pick(pointer)'));
assert.ok(source.includes('owner.TryGetToolkitDrop('));
assert.ok(source.includes('scroll.verticalScroller.lowValue, scroll.verticalScroller.highValue'));
assert.ok(read('src/TextureCompositorWindow.cs').includes('layerDragAutoScroll?.Stop();'));
assert.ok(read('src/TextureCompositorWindow.UI.cs').includes('new LayerDragAutoScrollManipulator(this, toolkitSettingsScroll)'));
console.log('Layer auto-scroll scalar/source checks passed (Unity UI not executed).');
