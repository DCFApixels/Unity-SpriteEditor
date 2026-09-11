import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
const read = path => readFileSync(new URL('../' + path, import.meta.url), 'utf8');
const settings = read('src/SpriteEditorUserSettings.cs');
const ui = read('src/SpriteEditorUserSettingsWindow.cs');
assert.ok(settings.includes('PresetsFolder => EditorPrefs.GetString(PresetsFolderKey, DefaultPresetsFolder)'));
assert.ok(settings.includes('EditorPrefs.SetString(PresetsFolderKey, value)'));
assert.ok(settings.includes('Environment.SpecialFolder.LocalApplicationData'));
assert.ok(settings.includes('Path.IsPathFullyQualified(value)'));
assert.ok(settings.indexOf('Path.GetFullPath(value)') < settings.indexOf('EditorPrefs.SetString(PresetsFolderKey, value)'));
assert.ok(settings.indexOf('File.Exists(value)') < settings.indexOf('EditorPrefs.SetString(PresetsFolderKey, value)'));
assert.ok(ui.includes('EditorUtility.OpenFolderPanel'));
assert.ok(ui.includes('if (!string.IsNullOrEmpty(selected)) SetPresetsFolder(selected)'));
assert.ok(ui.includes('isDelayed = true'));
assert.ok(ui.includes('new Button(SpriteEditorUserSettings.ResetPreviewAppearance)'));
const previewReset = settings.split('internal static void ResetPreviewAppearance()')[1];
assert.ok(!previewReset.includes('PresetsFolderKey'), 'Appearance reset preserves library path');
assert.ok(settings.includes('EditorPrefs.DeleteKey(PresetsFolderKey);\n            ResetPreviewAppearance();') ||
          settings.includes('EditorPrefs.DeleteKey(PresetsFolderKey);\r\n            ResetPreviewAppearance();'));
for (const forbidden of ['Directory.CreateDirectory', 'Directory.Delete', 'Directory.Move', 'File.Delete', 'File.Move', 'AssetDatabase.', 'Application.dataPath'])
  assert.ok(!(settings + ui).includes(forbidden), forbidden);
console.log('Preset folder preference/UI/source checks passed (Unity not executed).');
