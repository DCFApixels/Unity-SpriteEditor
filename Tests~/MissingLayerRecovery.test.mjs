import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
const read = path => readFileSync(new URL('../src/' + path, import.meta.url), 'utf8');
const ui = read('TextureCompositorWindow.MissingLayers.cs');
const inspector = read('TextureCompositorWindow.Inspector.cs');
const recovery = read('MissingLayerRecovery.cs');
const parser = read('MissingLayerData.cs');

// Source contracts only. Runtime YAML/field-transfer cases live in the opt-in smoke test.
assert.match(ui, /CreateLayerNameCell\(row, depth, null\)/);
assert.match(ui, /value = MissingLayerDisplayName\(container, index\), isReadOnly = true/);
assert.match(ui, /record = referenceId < 0 \? null : records.Find\(item => item.ReferenceId == referenceId\)/);
assert.match(ui, /missingLayerNames\[\(slot.Container, slot.Index\)\] = record.LayerName/);
assert.match(ui, /name.SetValueWithoutNotify\(displayName\)/);
assert.match(recovery, /record.LayerName \+ " — "/);
for (const part of ['layer-name', 'layer-opacity', 'layer-blend', 'layer-active-outline'])
    assert.ok(ui.includes(`sprite-editor-${part}`));
assert.match(inspector, /HasSelectedMissingLayer\) \{ BuildMissingLayerInspector\(root\); return; \}/);
assert.match(inspector, /!ReferenceEquals\(toolkitInspectorMissingLayer, selectedMissingLayer\)/);
assert.match(ui, /record.ReferenceId == referenceId && referenceId >= 0/);
assert.match(ui, /exact >= 0 \? exact \+ 2 : 0/, 'Ambiguous metadata must require an explicit choice');
assert.match(ui, /slot.IsValid\(compositor\)/);
assert.match(ui, /Container.Count != Snapshot.Length/);
assert.match(ui, /!ReferenceEquals\(Container\[i\], Snapshot\[i\]\)/);
assert.match(ui, /FindContainerPath\(document.layers, Container, "layers"\) == PropertyPath/);
assert.match(ui, /ExecuteContextChange\("Replace Missing Layer"/);
assert.match(ui, /slot.Container\[slot.Index\] = layer/);
assert.match(recovery, /document.FindLayer\(destination.Id\) != null/);
assert.match(recovery, /fields.TryGetValue\(property.Name/);
assert.match(recovery, /field.IsDefined\(typeof\(SerializeField\)\)/);
assert.match(recovery, /TryGetGUIDAndLocalFileIdentifier/);
assert.match(recovery, /if \(token is JObject managed && managed\["rid"\] != null\) return false/);
assert.match(recovery, /Enum.IsDefined\(type, value\)/);
assert.match(recovery, /float.MaxValue/);
assert.match(recovery, /TryGradient/);
assert.match(parser, /ScalarText/);
assert.doesNotMatch(ui + recovery, /ClearAllManagedReferencesWithMissingTypes|FromJsonOverwrite|WriteAllText|SaveAssets/);
console.log('Missing layer recovery: UI and safety source contracts passed; Unity smoke test not executed.');
