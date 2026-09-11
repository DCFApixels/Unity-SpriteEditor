// Source/compatibility checks only: does not compile or invoke Unity.
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root = fileURLToPath(new URL('../', import.meta.url));
const read = p => readFileSync(path.join(root, p), 'utf8');
const pkg = JSON.parse(read('package.json'));
assert.equal(pkg.displayName, 'WhimTex');
assert.equal(pkg.name, 'com.dcfa_pixels.sprite-editor', 'Existing UPM installations retain their identity');
assert.equal(pkg.documentationUrl, 'https://dcfapixels.github.io/WhimTex/');
assert.equal(pkg.repository.url, 'https://github.com/DCFApixels/WhimTex.git');
assert.match(read('Documentation~/_config.yml'), /^baseurl: \/WhimTex$/m);
assert.match(read('Documentation~/_config.yml'), /^repository: DCFApixels\/WhimTex$/m);
assert.equal(JSON.parse(read('src/DCFApixels.SpriteEditor.asmdef')).name, 'DCFApixels.SpriteEditor');
const window = read('src/TextureCompositorWindow.cs');
assert.ok(window.includes('[MenuItem("Window/WhimTex")]'));
assert.match(window, /void OnEnable\(\)\s*\{\s*titleContent = SpriteEditorBranding.WindowTitle\("WhimTex"\)/,
  'Restored windows update their persisted title without resetting their document');
const commands = read('src/Automation/Pipeline/SpriteEditorCommands.cs');
for (const id of ['begin', 'sessions', 'live', 'lock', 'describe', 'execute', 'render', 'inspect', 'import_image'])
  assert.ok(commands.includes(`"sprite_editor_${id}"`), `Stable CLI command: ${id}`);
for (const [file, key] of [
  ['src/SpriteEditorColorInputs.cs', 'DCFApixels.SpriteEditor.HdrColorInputs'],
  ['src/SpriteEditorUserSettings.cs', 'DCFApixels.SpriteEditor.PresetsFolder'],
  ['src/TextureCompositorWindow.Tools.cs', 'DCFApixels.SpriteEditor.PaintToolSettings']
]) assert.ok(read(file).includes(`"${key}"`), `Retain persisted preference: ${key}`);
function scan(dir) {
  for (const entry of readdirSync(path.join(root, dir), { withFileTypes: true })) {
    const file = path.join(dir, entry.name);
    if (entry.isDirectory()) scan(file);
    else if (file.endsWith('.cs')) assert.ok(!/Sprite Editor|"SpriteEditor shader/.test(read(file)), `Old display name: ${file}`);
  }
}
scan('src');
assert.ok(read('src/PsdWriter.cs').includes('w.Unicode("WhimTex"); w.Unicode("WhimTex");'));
assert.ok(read('Skills~/sprite-editor-live/SKILL.md').includes('name: sprite-editor-live'));
const logo = read('Documentation~/Images/whimtex-logo.svg');
assert.match(logo, /<svg\b/);
assert.match(logo, /<linearGradient\b/);
assert.ok(!/<image\b|data:image|<script\b/.test(logo), 'Logo remains editable vector artwork, not an embedded bitmap');
for (const file of ['README.md', 'README-RU.md'])
  assert.ok(read(file).includes('src="Documentation~/Images/whimtex-logo.svg"'));
const iconGuid = read('src/WhimTexIcon.png.meta').match(/^guid: (\w+)$/m)[1];
assert.ok(read('src/SpriteEditorBranding.cs').includes(`GUIDToAssetPath("${iconGuid}")`));
for (const file of ['src/TextureCompositorWindow.cs', 'src/SpriteEditorUserSettingsWindow.cs',
  'src/ModifierEditorWindow.cs', 'src/Utils.cs'])
  assert.ok(read(file).includes('SpriteEditorBranding.WindowTitle('), `${file}: branded title`);
for (const [file, size] of [
  ['src/WhimTexIcon.png', 64], ['Documentation~/Images/favicon-32.png', 32],
  ['Documentation~/Images/apple-touch-icon.png', 180], ['Documentation~/Images/whimtex-logo.png', 512]
]) {
  const png = readFileSync(path.join(root, file));
  assert.equal(png.subarray(1, 4).toString(), 'PNG');
  assert.equal(png.readUInt32BE(16), size);
  assert.equal(png.readUInt32BE(20), size);
  assert.equal(png[25], 6, `${file}: preserve RGBA transparency`);
}
assert.ok(read('Documentation~/_includes/title.html').includes('site.logo | relative_url'));
assert.ok(read('Documentation~/_includes/favicon.html').includes('site.logo | relative_url'));
console.log('WhimTex branding and legacy package/API/preference identity checks passed (Unity not executed).');
