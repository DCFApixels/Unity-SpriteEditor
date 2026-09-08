# Sprite Editor: agent API v1

The API edits the same model and uses the same renderer, brush and save path as the window.
No Sprite Editor window or active selection is required. It creates ordinary compositor `.asset`
files, with their layers, owned Drawing textures, baked Texture2D and Sprite subassets, and Project preview.

## Connecting

Use a running Unity Editor in Edit Mode. The plugin's optional adapter supports
`com.unity.pipeline` 0.5.0-exp.1 or later and registers these commands after scripts compile.
The core API has no dependency on Pipeline. JSON parsing uses Unity's Newtonsoft Json package.
When copying the plugin into `Assets` instead of using UPM, ensure its declared package dependencies
are already installed; an `Assets`-local `package.json` does not install dependencies automatically.

```powershell
unity status --project-path 'D:/Projects/MyGame' --format json
unity command --query sprite_editor --project-path 'D:/Projects/MyGame' --format json
unity command sprite_editor_describe --project-path 'D:/Projects/MyGame' --format json
```

If commands are absent, check whether Pipeline is installed and the plugin is compiled. Follow the
project's rules for compilation or installation; do not trigger them automatically when prohibited.
No API command calls `AssetDatabase.Refresh`, requests script compilation, enters Play Mode or opens a scene.
Import/save commands do import the specific image or compositor asset they write.

| Command | Parameters | Result |
|---|---|---|
| `sprite_editor_describe` | none | Protocol, operations, enums, limits |
| `sprite_editor_inspect` | `assetPath` | Document revision, stable IDs, hierarchy and settings |
| `sprite_editor_import_image` | `sourcePath`, `assetPath` | Imported texture path, GUID, dimensions |
| `sprite_editor_execute` | `requestPath` | Batch result, created IDs, updated document |
| `sprite_editor_render` | `assetPath`, `outputPath`, optional `maxSize=1024`, `overwrite=false` | Absolute PNG path and dimensions |

Pass `--project-path` and `--format json` on every command. The API object is nested inside the
CLI/Pipeline response: check its `apiVersion` and `success` as well as transport success/exit code.
An API validation error can arrive through a successful transport. `errorCode` and `error` describe it;
`failedOperation`, when present, is zero-based (`-1` means batch/save level).

Direct C# entry points, all on Unity's main thread, return a JSON string:

```csharp
SpriteEditorApi.Describe();
SpriteEditorApi.Inspect("Assets/Art/Icon.asset");
SpriteEditorApi.ExecuteJson(requestJson);
SpriteEditorApi.ExecuteFile(absoluteRequestPath);
SpriteEditorApi.ImportImage(absolutePngPath, "Assets/Art/Source.png");
SpriteEditorApi.Render("Assets/Art/Icon.asset", "Temp/SpriteEditor/icon.png", 1024, false);
```

The full namespace is `DCFApixels.SpriteEditor`. With an existing C# eval bridge, call these methods
instead of installing Pipeline solely for this tool. Send a request file to avoid shell-escaping JSON.

## Generated image → compositor

1. Generate a PNG/JPEG using the agent's image tool, or use a user-provided local image.
2. Import it to a new asset path. Import never overwrites; for an existing imported texture, skip this step.

```powershell
unity command sprite_editor_import_image --sourcePath 'C:/Temp/generated.png' --assetPath 'Assets/Art/AgentIcon/source.png' --project-path 'D:/Projects/MyGame' --format json
```

The API copies only that file, preserves its dimensions up to the resource limit, disables texture
compression/mipmaps and keeps non-power-of-two dimensions. It does not change existing source importers.
PNG/JPEG only; the destination must use the same extension. No URLs or automatic image generation.

3. Write a JSON batch outside `Assets`, for example `Temp/SpriteEditor/create.json`:

```json
{
  "apiVersion": 1,
  "assetPath": "Assets/Art/AgentIcon/Icon.asset",
  "create": true,
  "width": 1024,
  "height": 1024,
  "save": true,
  "operations": [
    {"op": "add", "type": "group", "as": "art", "settings": {"name": "Artwork"}},
    {
      "op": "add", "type": "file", "as": "image", "parent": "@art",
      "settings": {"source": "Assets/Art/AgentIcon/source.png", "filter": "Source"},
      "transform": {"position": [20, -12], "rotation": 8, "tiling": "Clip"}
    }
  ]
}
```

4. Optionally run the same request with `dryRun:true`, then change it to false to apply:

```powershell
unity command sprite_editor_execute --requestPath 'D:/Projects/MyGame/Temp/SpriteEditor/create.json' --project-path 'D:/Projects/MyGame' --format json
unity command sprite_editor_render --assetPath 'Assets/Art/AgentIcon/Icon.asset' --outputPath 'Temp/SpriteEditor/icon-v1.png' --project-path 'D:/Projects/MyGame' --format json
```

5. View the returned PNG. Revise the document if needed, using IDs/revision from the response or a new inspect.
The `.asset` is already usable as a texture/Sprite in Unity; this temporary PNG is only for inspection.

An initially empty File layer automatically gets Original Aspect when assigned its first texture.
An explicit transform patch is applied **after** this fit. Setting `scale:[1,1]` explicitly therefore
stretches a non-square source to the full canvas; omit scale to preserve the initial aspect fit.

## Batch contract

| Request field | Meaning |
|---|---|
| `apiVersion` | Required integer `1` |
| `assetPath` | Required project-relative `Assets/.../*.asset`; no overwrite on create |
| `create` | Default false. True creates a new document |
| `width`, `height` | Create only; integers, default 512 each, 1..16384 and at most 16,777,216 total pixels |
| `expectedRevision` | Required for existing documents; copy the latest inspect/execute revision verbatim |
| `save` | Default true. False keeps edits in the loaded existing document without rebaking output |
| `dryRun` | Default false. Validate the entire batch on a detached model, without strokes, rendering, saving or consuming real name counters |
| `operations` | Required array, at most 256; use `[]` to save/rebake without additional edits |

Unknown/duplicate fields, wrong JSON types, invalid enum names, non-finite numbers and out-of-range
values fail validation. Omitted patch fields are preserved; JSON null is not a reset instruction.
Property and enum names are case-sensitive. Do not pass Unity instance IDs, YAML file IDs or display names.

Each `add` can define `as:"image"`. Later operations refer to it as `layer:"@image"`, `parent:"@image"`
or `target:"@image"`. Aliases are local to one batch, must be unique and cannot forward-reference.
Persistent layer IDs are returned per operation and in `document.layers`.

`document.layers` is flat, with `parent` and sibling `index`. Index zero is visually topmost.
`settings` contains editable values; hierarchy, target and transform have separate fields/operations.
`gradientKeys` is inspection data with separate color/alpha keys, not the editable gradient-stop format.

### Operations

```json
{"op":"add", "type":"drawing", "as":"ink", "parent":"@art", "index":0, "settings":{"name":"Ink"}}
{"op":"set", "layer":"@ink", "settings":{"opacity":0.6, "blend":"Multiply"}}
{"op":"transform", "layer":"@image", "transform":{"position":[24,-10], "rotation":15}}
{"op":"move", "layer":"@ink", "parent":"", "index":0}
{"op":"target", "layer":"@outline", "input":"Specific", "target":"@art"}
{"op":"target", "layer":"@outline", "input":"Previous"}
```

- `add`: types `file`, `drawing`, `group`, `color`, `gradient`, `outline`, `sdf`.
  Optional `parent` defaults to root, `index` to 0. `settings` and `transform` are optional patches.
- `set`: requires `layer` and `settings`.
- `transform`: requires `layer` and `transform`.
- `move`: `index` is the insertion index **after removal** from the old container; omitted parent
  or `parent:""` moves to root. A group cannot move into itself or its descendants.
- `target`: SDF/Outline only; default input Specific. Previous means the next sibling below the effect.
  Specific targets can be groups, but cannot create a dependency cycle.
- `stroke`: Drawing only, detailed below.

### Layer settings

| Applies to | Supported keys |
|---|---|
| All | `name` (string), `enabled` (boolean) |
| Non-group | `opacity` (0..1), `blend` (BlendMode name), `filter` (`Source`, `Point`, `Bilinear`, `Trilinear`) |
| File | `source` (already imported Texture2D path in Assets or Packages) |
| Color | `color` (`[r,g,b,a]`, each 0..1) |
| Drawing | `brush` (partial brush settings below) |
| Outline | `color`, `metric`, `outlineWidth`, `outlineSoftness` (0..16384), `outlinePosition` (`Outside`, `Inside`, `Center`) |
| SDF | `metric`, `sourceChannel` (`Alpha`, `Red`, `Green`, `Blue`, `Luminance`), `threshold` (integer 0..255), `distancePosition` (`Outside`, `Inside`, `Center`, `Signed`), `inverted` (bool), `maxDistance` (0..16384; zero = automatic) |
| Gradient, SDF | `gradient`: 2..8 `{"time":0.0,"color":[1,1,1,1]}` stops in strictly increasing time order, time 0..1 |

Discover blend modes and distance metrics with `sprite_editor_describe`. Groups are organizational,
pass-through containers: group opacity, transform and modifiers are deliberately rejected.
Other settings of existing layers and all existing FX are preserved. API v1 does not author Shader FX,
delete layers, duplicate/rasterize layers, resize an existing canvas or change gradient geometry.
These remain available in the window. Use `enabled:false` to hide an unwanted layer non-destructively.

### Transforms and coordinates

Transform patches support `position:[x,y]`, `scale:[x,y]`, `pivot:[u,v]`, `rotation`, `tiling`,
`reset:true`, `originalAspect:true`.

- Position is in canvas pixels: positive X goes right, positive Y goes up.
- Rotation is counterclockwise degrees.
- Pivot is bottom-left UV: `[0.5,0.5]` is the center.
- Scale `[1,1]` means the full canvas-sized source rectangle; negative values mirror, zero is rejected.
- Tiling is `Clip`, `Repeat`, `Mirror` or `Source`. `Source` reads the source texture's U/V wrap modes.
- `reset` is applied before the other fields, Original Aspect after them.
- Changing pivot through this API uses raw transform semantics; it does not compensate position.

### Drawing strokes

```json
{
  "op":"stroke", "layer":"@ink", "space":"canvasPixels", "erase":false,
  "brush":{"color":[1,0.2,0.1,1], "size":12, "hardness":0.8, "spacing":0.16},
  "points":[[100,100],[180,140],[240,110]]
}
```

Each stroke has 1..4096 points: one point is a dab, multiple points form a polyline. Use sparse points
for straight segments; the brush interpolates stamps. Curves can be sampled as a polyline.
The brush uses the same renderer and source-over alpha as manual painting. `erase:true` uses the eraser.
No selection or RGBA Preview mask is inherited from the window: specify the desired RGBA explicitly.
Color alpha zero leaves no mark, including for the eraser; eraser strength otherwise follows alpha.

- `space:"canvasPixels"` (default): top-left origin, X right, Y down. The API inverts the layer's
  transform to place ink under that canvas position. This mode requires Clip tiling.
- `space:"layerUv"`: bottom-left origin in the untransformed source tile; `[0,0]` bottom-left,
  `[1,1]` top-right. Useful for repeating transforms, where multiple visible copies share one source.
- Brush size is source-space diameter in canvas pixels, before layer transform. Nonuniform scale
  stretches it, just as when painting the layer manually.
- `brush` is optional; supplied fields update the layer's saved brush settings. Missing fields retain
  their current values, including symmetry/repeat. Set `repeat:"None"`
  explicitly when a one-off unmirrored stroke is intended.
- API brush parameters remain independent of the window's shared interactive brush/color/fill
  preferences. API strokes do not read or change those preferences. Symmetry/repeat and transforms
  are layer-local and are shared by API and interactive painting.

| Brush field | Values |
|---|---|
| `color` | RGBA array, 0..1 |
| `size` | 1..4096 |
| `hardness` | 0..1 |
| `spacing` | 0.01..4, fraction of brush size (0.16 = 16%) |
| `mirrorX`, `mirrorY` | Vertical/horizontal axis reflection respectively; used only with `repeat:"Mirror"` |
| `center` | Bottom-left UV, each component 0..1 |
| `repeat` | `None`, `Mirror`, `Horizontal`, `Vertical`, `Grid`, `Radial` (mutually exclusive) |
| `repeatCount`, `repeatSecondaryCount` | Integers 2..64; secondary is the grid Y count |
| `radialStartAngle` | 0..360 degrees counterclockwise from the left; Radial only, default 0 preserves the original layout |
| `elements` | `Copy`, `AlternateMirror` |
| `boundary` | `Continue`, `Clip` |

For simple symmetry, set `repeat:"Mirror"` and at least one of `mirrorX`/`mirrorY` to true.
Mirror axis choices are retained but ignored in other modes. `center` affects Mirror and Radial;
`elements` and `boundary` apply only to Horizontal, Vertical, Grid and Radial.
For reflected radial sectors, use `repeat:"Radial", elements:"AlternateMirror"`.
Legacy mirror-only settings migrate to Mirror; legacy Repeat+Mirror uses Repeat without extra mirrors.

Clip anchors to the stroke's first cell/sector and terminates the polyline at its first exit.
Start a new stroke to draw in another segment. Continue permits crossing segment boundaries.
The validator caps estimated replicated stamps at 100,000 per stroke, covered brush pixels at
250,000,000 per stroke and source UV at -4..5. Documents support up to 1024 layers and
67,108,864 total owned Drawing pixels through the API.

## Validation, Undo and recovery

- Every batch is preflighted on a detached model before the live document is touched. `dryRun`
  does not prove GPU availability, successful image decoding or writable disk space.
- Existing-document changes form one Undo step, including drawing pixels. Model/paint execution
  errors before saving attempt to revert that step. Asset saves/imports and new file creation are
  **not filesystem transactions**. Empty directories or copied files can remain after I/O failures.
- Saving rebakes output. Undo restores editing state, but an already saved/baked output must be
  saved again after Undo/Redo. Use a new revision and an empty operation list to save without dialogs.
- `save:false` edits are in memory and visible to an open Sprite Editor; they are not a persisted output.
- A revision includes serialized state, drawing pixels, modifier state and saved asset dependencies.
  It is an opaque optimistic-concurrency token, not a portable version-control ID. Re-inspect after
  Undo, save, import or domain reload. Do not cache it across sessions.
- On `revision_conflict`, inspect and reconsider the patch. On `already_exists`, inspect/reuse the
  asset or choose a new path; do not delete it to make the request succeed.
- On a lost response/timeout, the command may already have executed. Inspect layer IDs/names and
  render before deciding what remains. Do not automatically retry additions/strokes.
- On `saveMayBePartial:true`, editing has been applied but saving failed. Inspect first. If the asset
  exists, issue a save-only batch with its current revision; don't replay the edits.
- If an import fails after copying, the response reports `fileCreated:true` and the retained path.
- If reverting a failed edit also fails, `rollbackFailed:true` reports that explicitly. Stop and inspect;
  neither the old state nor a fully applied batch can be assumed.

Destinations stay under Assets (not StreamingAssets); path traversal and write-through symlinks are
rejected. Preview PNGs are limited to `Temp/SpriteEditor/` and do not overwrite unless requested.
Import accepts at most 64 MiB and 16,777,216 source pixels. Resource limits protect the editor from
accidental huge requests, but effect-heavy documents can still take time: use preview resolution
appropriately and keep batches focused. The API executes on the main thread; it is not an async job queue.

## Verification

[Tests~/AgentApiSmoke.cs](../Tests~/AgentApiSmoke.cs) is an opt-in C# eval-file smoke test. After the
user compiles the plugin, run it through an available `eval_file` bridge on the intended project.
It uses a new uniquely named folder under Assets and retains its fixtures for inspection; it does
not edit existing documents. It verifies create/inspect, aspect/transform, preflight rejection,
revision conflict, painting, Undo/Redo, save and output subassets. See the test's result for its path.
Do not run it when the project's rules prohibit creating test assets.

[Tests~/DrawingPatternSmoke.cs](../Tests~/DrawingPatternSmoke.cs) is a separate opt-in eval-file
regression test for mutually exclusive Mirror/Repeat modes, legacy migration, movable mirror centers,
source stamps under the cursor and JSON round-trips. It creates no assets or GPU resources; run only
after the user has compiled the updated plugin. It does not replace visual painting checks.
