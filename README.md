<a id="top"></a>
<h1 align="center">Unity Sprite Editor</h1>

<p align="center">
  A layered sprite and texture editor, right inside Unity.
</p>

<p align="center">
  <a href="package.json"><img alt="Package version" src="https://img.shields.io/github/package-json/v/DCFApixels/Unity-SpriteEditor?color=3984c6&amp;style=for-the-badge"></a>
  <a href="LICENSE.md"><img alt="License: MIT" src="https://img.shields.io/github/license/DCFApixels/Unity-SpriteEditor?color=3984c6&amp;style=for-the-badge"></a>
  <a href="#installation"><img alt="Unity 6 or newer" src="https://img.shields.io/badge/Unity-6%2B-383838?logo=unity&amp;logoColor=ffffff&amp;style=for-the-badge"></a>
  <a href="https://discord.gg/kqmJjExuCf"><img alt="Join Discord" src="https://img.shields.io/badge/Discord-JOIN-6473c8?logo=discord&amp;logoColor=ffffff&amp;style=for-the-badge"></a>
</p>

<p align="center">
  <b>English</b> · <a href="README-RU.md">Русский</a>
</p>

<p align="center">
  <a href="#quick-start">Quick start</a> ·
  <a href="#shortcuts">Shortcuts</a> ·
  <a href="CHANGELOG.md">Changelog</a> ·
  <a href="https://github.com/DCFApixels/Unity-SpriteEditor/issues">Report an issue</a>
</p>

---

Create sprites, icons, patterns, and layered textures without leaving the Editor. Paint directly
on the Preview, organize layers into nested groups, add Outline/SDF effects, and keep
the editable composition alongside the exported result.

> [!NOTE]
> This is an **Editor-only** tool built with UI Toolkit, not a runtime drawing component.

## At a glance

| Tool | What you can do |
| :--- | :--- |
| Layers & groups | Combine images, drawing, fills, gradients, Outline, and SDF; nest groups and reorder with drag-and-drop. |
| Live painting | Adjust brush size, hardness, and spacing; erase with `RMB` and draw straight lines with `Shift`. |
| Symmetry & repetition | Mirror strokes, tile in rows or grids, or repeat around a circle with alternating reflection. |
| Visual transforms | Move, resize, rotate, and reposition the pivot directly in the Preview. |
| Multi-selection | Select ranges, move layers together, and drop them onto the folder or trash icon. |
| Editable documents | Save the layer tree and drawing textures in a self-contained Unity asset. |
| Export | PNG, JPEG, TGA, EXR, or a native Unity Texture2D asset. |

## Contents

- [Installation](#installation)
- [Quick start](#quick-start)
- [Workspace & saving](#workspace)
- [Layers & groups](#layers)
- [Painting & repetition](#painting)
- [Transform & pivot](#transform)
- [Outline, SDF & blending](#effects)
- [Export formats](#export)
- [Shortcuts](#shortcuts)
- [Agent API & CLI](#automation)
- [Community & license](#community)

<a id="installation"></a>
## Installation

**Requires Unity 6 (`6000.0`) or newer.** Burst, Collections and Newtonsoft Json are declared package dependencies.

Open **Window → Package Management → Package Manager**, choose **Install package from git URL**,
and paste:

```text
https://github.com/DCFApixels/Unity-SpriteEditor.git
```

<details>
<summary>Install through manifest.json or pin a version</summary>

Add this entry to the `dependencies` object in `Packages/manifest.json`:

```json
"com.dcfa_pixels.sprite-editor": "https://github.com/DCFApixels/Unity-SpriteEditor.git"
```

The URL above follows the default branch. To pin a release, append an existing tag from
[Tags](https://github.com/DCFApixels/Unity-SpriteEditor/tags). The version badge reads
[`package.json`](package.json); it is not the latest release-tag number.

</details>

<a id="quick-start"></a>
## Quick start

1. Open **Window → Sprite Editor** and click **New**.
2. Set **Canvas → W / H** above the Preview.
3. Use the **+** button at the bottom of Layers to add a **Drawing Layer**.
4. Select the layer and paint in the Preview. Adjust the brush in its header.
5. Press `Ctrl+S` to save the layers and embedded output texture.
6. Drag the saved asset into a texture field, or expand it in Project and use **Output Sprite**.
   Use **Export** only when you need a separate image file.

> [!TIP]
> Reopen a saved document by double-clicking its texture, sprite, or layer document in Project.
> **Assets → Open in Sprite Editor** also works; the nested layer document retains its Inspector button.

<a id="workspace"></a>
## Workspace & saving

The document header is shared by the entire window:

**New → document field → Save → Save As → Export**

- **Save** writes to the current asset without a dialog. It is disabled for temporary documents.
- **Save As** creates a separate editable document.
- `Ctrl+S` saves an existing asset, or opens Save As for a temporary document.
- **Export** writes the flattened image, not the editable layer tree.

**Use the document without exporting.** Save / Save As writes a full-resolution Texture2D
and an **Output Sprite** inside the same `.asset` as the editable layers. The texture is the
main asset, with a normal image thumbnail in Project, and can be assigned to materials,
RawImage, and Texture2D fields. Expand the asset to assign Output Sprite to SpriteRenderer
or UI Image; it uses a centered pivot, a full-rectangle mesh, and 100 pixels per unit.

Subsequent saves update the existing output objects, including when the canvas size changes,
to preserve references. Save As creates independent outputs. Existing documents gain this
feature on their next Save in Sprite Editor; no bulk conversion is performed.
The nested layer document's Inspector also has **Save & Update Output** and a saved preview.
Output reflects the last explicit Sprite Editor save, not unfinished edits: save again after
Undo/Redo or changing external source textures. Project thumbnails use saved pixels rather
than recompositing layers. No layer rendering is needed at runtime to use the baked output.

The **Preview is on the left**; selected-layer settings and the layer list are on the right.
The narrow toolbar at the far left selects **Brush** (brush icon, `B`) or **Transform** (hand icon, `T`).
Eraser remains a Brush mode in the header; `RMB` temporarily erases. Brush requires a Drawing layer;
Transform supports any non-group layer. The active tool is highlighted, and its options appear in the header.
Drag the vertical divider to adjust the right pane's width, which stays fixed when resizing the
window. The right pane has its own horizontal divider and independently scrolling sections.
Both right-hand sections have fixed headers with thin dividers. The upper header shows the
active layer's name, or **Layer Settings** when nothing is selected; the lower header is **Layers**.

Settings follow the active layer. **Transform** starts collapsed; brush settings appear when
a Drawing layer is active. Each **⋮ → Properties** invocation opens a separate window.
**FX** lives in the same menu.

The **window tab's ⋮ menu → Reset Sprite Editor Settings…** restores panel sizes, scrolling,
selection, foldouts, preview tool state and Live Quality defaults after confirmation.
It resets all open Sprite Editor windows, without deleting documents (including unsaved work),
layer/brush settings or assets, or changing Unity settings and docking. The settings reset has no Undo.

<a id="layers"></a>
## Layers & groups

**Layer types:** File · Drawing · Color Fill · Gradient · Outline · SDF · Group.

Drag one or more **Texture2D assets from Project** into Layers to create File layers. Drop
between rows, inside a group, or onto the empty list area. New layers are selected together,
the last one is active, and the entire drop supports a single Undo. Source assets are not modified.
Dropping textures onto **Preview** adds them at the top of the root Layers list, outside any group.
Assigning a texture to an empty File layer automatically applies **Original Aspect**;
replacing an already assigned texture leaves its Transform unchanged.

Layers appear top-to-bottom and render from the bottom up. Groups are nested,
**pass-through** containers: their children blend into the surrounding stack without an isolated
color composite.

| Selection | Result |
| :--- | :--- |
| Click | Select one layer. |
| `Ctrl`-click | Add or remove an individual layer. |
| `Shift`-click | Select a range of visible rows. |
| `Ctrl+Shift`-click | Add a range to the selection. |

The **last selected layer is active**, with a brighter fill and a subtle blue border. Inspector,
painting, and Transform tools operate on that layer, not the whole selection.

Use the footer to add, group, or delete layers. Drag a selected layer's handle to move the
selection together; drop into the center of a group to move inside it. Dropping onto the
**folder** or **trash** icon groups or deletes the dragged selection in one Undo step.
A selected group carries its descendants only once.

Choose **⋮ → Duplicate** to copy one layer or a whole group. Drop the selection onto **+**
to duplicate it in one Undo step. Copies appear above the originals and become selected;
Drawing pixels and embedded Shader FX are independent, while external assets stay linked.
Outline/SDF targets inside the copied set follow the copies. If a Previous input is no longer
adjacent, the copied effect uses that input explicitly to preserve its result.

Per-row menus and inline controls affect that row. Each layer type has its own document-local
name counter: `Layer 1…` for Drawing, `File 1…`, `Color Fill 1…`, `Gradient 1…`,
`Outline 1…`, `SDF 1…`, and `Group 1…`. Existing names and counter progress are preserved.
Duplicates keep the full source name and append ` Copy n`, using one separate counter for all
copies in the document, including group descendants. Deleted numbers are not reused.

Drop a layer/group handle onto **Target** in SDF or Outline settings to assign it and switch to
**Specific**. A dragged selection uses its active layer, not the first row. The handle keeps the
current selection while dragging, so the destination settings stay visible. Cross-document and
cyclic targets are rejected. This also works in separate **Properties** windows and supports Undo.

Use the number row or numpad to set the active layer's opacity: `5` → 50%, then `7` within
0.6 seconds → 57%; after a pause, `7` → 70%. `0` sets 100%; quick `00` sets 0% and `05` sets 5%.
A quick pair is one Undo step. Text/numeric inputs and modified shortcuts are not intercepted.
This changes the active layer only, not brush opacity; pass-through groups have no opacity control.

> [!IMPORTANT]
> Groups currently have no group-level opacity, blend mode, Transform, or FX.
> Their visibility can be toggled, and Outline/SDF can use a group as input.

<details>
<summary>Convert any layer to Drawing / bake its transform</summary>

Use **⋮ → Convert to Drawing** — including on an existing Drawing layer.

| Mode | Result |
| :--- | :--- |
| **Keep Transform** | Rasterize the source while leaving its Transform editable. |
| **Apply Transform** | Bake the current Transform and tiling into full-resolution pixels, then reset Transform to identity and tiling to Clip. |

The replacement retains its ID, name, stack position, visibility, opacity, blend mode, and FX.
FX remain live. Drawing layers also retain brush and repetition settings. Outline/SDF become
static pixels. Pixels beyond the canvas are cropped when applying the transform.

A group is flattened against transparency using its visible descendants, their transforms,
and FX. The replacement uses Normal blending, opacity 1, and an identity Transform; both
conversion modes therefore produce the same result for groups.

Group conversion asks for confirmation: pass-through interactions with outside layers can
change, and effects targeting removed descendants lose those targets. References to the
converted layer itself remain valid. Conversion supports Undo/Redo, including pixel textures.

</details>

<a id="painting"></a>
## Painting & repetition

The **R / G / B / A** buttons in Preview's footer control both channel display and the brush color
mask. Disabled RGB components are painted as `0`; disabling A makes brush strokes a no-op.
The selected brush colors are unchanged, and the eraser ignores this mask. In Preview, A off
ignores transparency; A alone shows alpha in grayscale; all channels off shows black.
Switching channels does not edit existing pixels or affect Save/Export. New masked strokes
are real pixel edits with normal Undo/Redo. All channels start enabled and are restored by Reset.

Select a **Drawing Layer** and paint directly in the Preview. The image still shows the complete
layer stack, with blending and effects applied.

- `LMB` uses the selected Brush/Eraser tool; `RMB` temporarily erases.
- Adjust **Size**, **Hardness**, and **Step**. Step is stamp spacing as a percentage of brush diameter.
- Two color swatches store foreground/background colors; `X` swaps them.
- `Shift`-drag locks a stroke horizontally or vertically; `Shift`-click connects the previous
  painted endpoint to the new point.
- **Live Quality**, always visible on the left of the Preview footer, sets painting-time
  Preview resolution: **12.5–100%**, default **80%**.
  Lower values reduce preview work; exports and saved canvas dimensions are unchanged.

### Symmetry & repeated strokes

In the Drawing layer's settings, choose **Mode**: None, Mirror, Horizontal, Vertical, Grid or Radial.
Mirror is a separate mode, not an extra reflection applied on top of Repeat.
Symmetry and repetition settings are saved independently for each Drawing layer.

| Setting | Behavior |
| :--- | :--- |
| Mirror: X / Y | Reflect across the vertical / horizontal axis through Center; enable both for four-way symmetry. |
| Horizontal / Vertical | Repeat in a row or column. |
| Grid | Repeat with independent X/Y counts. |
| Radial | Repeat around a circle with an adjustable sector count. |
| Copy / Alternate Mirror | Repeat directly / reflect every second cell or sector. |
| Continue / Clip | Let the stroke cross boundaries / constrain it to the cell or sector where the stroke began. |

**Center** is available for Mirror and Radial. **Elements** and **Edges** apply only to repeating modes.
Radial also has a **Start Angle (°)** slider with numeric input (0–360°). It rotates sector boundaries
counterclockwise from the left, including Alternate Mirror and Clip; 0° preserves the original layout.
To mirror radial sectors, use **Radial → Alternate Mirror**. Legacy mirror-only layers become Mirror;
legacy combinations keep their Repeat mode without additional X/Y reflections. Existing pixels are unchanged.

Painting from a mirrored cell keeps the active copy under the cursor.

<details>
<summary>Straight-line behavior & drawing storage</summary>

For an independent axis-aligned line, click the start without `Shift`, then hold `Shift` while dragging.
The first movement chooses the lock axis in canvas space. Repeated `Shift`-clicks form connected
segments. Eraser, spacing, symmetry, and repetition apply to these lines too.

Clip constrains a connection to the repeat shape containing its starting endpoint. Undo/Redo,
clearing the layer, and switching documents reset the connection anchor. Connections do not
carry across different drawing layers or canvas sizes.

Drawing textures are stored as sub-assets of the document. They travel with the composition.

</details>

<a id="transform"></a>
## Transform & pivot

Select a non-group layer and choose **Transform** (hand icon) in the left toolbar, or press `T`.

- Drag inside the frame to move; use its eight handles to resize and the round handle to rotate.
- Drag the **gold pivot cross** without moving the visible image.
- The pivot snaps to nine anchors: corners, edge midpoints, and center. Hold `Ctrl` to bypass snapping.
- Hold `Shift` for axis-constrained movement, proportional resizing, or 15° rotation steps.
- `Escape` cancels the drag; `T` / `Enter` exits Transform mode. Painting is paused while it is active.

**Tiling:** Clip leaves pixels outside the frame transparent; Repeat tiles the texture;
Mirror alternates reflected tiles; Source inherits the source texture's wrap modes, including
separate U/V settings. Source with Clamp extends edge pixels, unlike transparent Clip.
Clip remains the default. These settings affect Preview and export without changing the source importer.

**Filter:** Source (default) inherits the assigned texture's Filter Mode; Point keeps hard pixel
edges; Bilinear smooths sampling; Trilinear also blends between existing mip levels.
Without source mipmaps, Trilinear behaves like Bilinear. Filter is independent of Tiling,
survives Transform Reset, and is available in Transform settings and the Preview Transform toolbar.
Generated layers use Bilinear for Source; no texture import settings are modified.

**Original Aspect**, next to Reset in layer settings and in the Preview Transform toolbar,
fits the source proportions inside the current frame by shrinking one axis. It preserves the
image center, pivot, rotation, flips, and tiling. File layers use their source texture dimensions;
Drawing layers use their stored pixels; generated layers use the canvas ratio. Missing File
sources and zero/near-zero scales disable the button. The change supports Undo/Redo.

<details>
<summary>Transform details</summary>

The frame covers the full source canvas, including transparent pixels. Its values are the same
Position, Scale, and Rotation fields found in layer settings. Each drag is one Undo action.

Position uses output pixels, Pivot uses normalized coordinates, Scale is a multiplier, and
Rotation uses degrees. The pivot may lie outside the frame; snapping uses a 10-UI-pixel radius.
Pivot dragging is disabled at zero/near-zero scale.

On Drawing layers, transform tiling produces rendered copies; painting still edits the primary
tile. Brush repetition is a separate feature.

</details>

<a id="effects"></a>
## Outline, SDF & blending

**Outline and SDF** can target the item directly below them in the same group, or a specific
layer/group. Group inputs combine the alpha of visible descendants into a mask without isolating
their color blending. Self-references and cyclic effect dependencies are rejected.

Distance metrics: **exact Euclidean EDT**, approximate Euclidean (8-neighbour chamfer),
Manhattan, and Chebyshev. Width, softness, and maximum distance use output pixels.
Processing uses Burst and Native Collections, with parallel exact-EDT passes and direct output
buffer writes.

**Blend modes** use source-over alpha: the RGB blend function affects overlapping
regions; non-overlapping regions retain the color and alpha of the layer present there.

<details>
<summary>All blend modes & modifiers</summary>

Normal · Add · Subtract · Multiply · Divide · Screen · Overlay · Darken · Lighten ·
Color Dodge · Color Burn · Linear Dodge (Add) · Linear Burn · Linear Light ·
Linear Light Add/Sub · Vivid Light · Pin Light · Hard Mix · Hard Light · Soft Light ·
Difference · Exclusion · Negation.

- **None** leaves the composite unchanged.
- **Overwrite** replaces complete RGBA pixels; layer opacity interpolates between the old and new pixels.
- **FX modifiers** are Materials or Shader FX assets applied in list order after a layer's transform.

Existing serialized values for Normal, Multiply, and Overwrite are preserved for older documents.

</details>

### Shader FX

Write a fragment effect without creating a complete shader or material:

1. Select a layer and click **+ Shader FX** in the **FX** section of its settings.
   No file dialog or separate asset is required, even in an unsaved document.
2. Write `ApplyFX` and add parameters directly in the layer settings. Click **Apply** to
   compile and preview the effect; **Save / Ctrl+S** saves it inside the compositor document.
3. Change parameter values to update the preview immediately. Code and parameter declarations
   remain drafts until **Apply**; an unsuccessful compilation leaves the last working effect in use.

The code editor supports `Ctrl+Z`, `Ctrl+Y` and `Ctrl+Shift+Z` (`Cmd` on macOS), using Unity's
Undo history. Continuous typing is grouped; pauses, navigation, pasting, cutting and selection
replacement separate editing steps. Undo/Redo restores the caret and selection without rebuilding
the field. It edits the draft only: click **Apply** to compile the restored code.

For example, add a **Float** parameter named `_Amount` and use:

```hlsl
float4 ApplyFX(float2 uv, float4 color)
{
    return float4(lerp(color.rgb, 1.0 - color.rgb, saturate(_Amount)), color.a);
}
```

Parameters support **Float**, **Color**, **Vector** and **Texture2D**. Their uniforms are generated
automatically; do not declare them a second time. Each **+ Shader FX** creates a document-owned
effect. **Save As** copies embedded FX and their shaders independently. External reusable FX are
still supported through **+ Reference**; editing an external FX affects every layer using it.
Use **Embed** to turn an external FX reference into an independent document-owned copy without
changing or deleting the original asset.
`SampleInput(uv)` reads the incoming layer, including earlier modifiers. Return **straight RGBA**;
layer opacity and blending are applied afterwards. One FX is one GPU pass, with no CPU pixel readback.

Use standard `#include` for your HLSL libraries, with quoted project, package or relative paths:

```hlsl
#include "Assets/Shaders/MyLibrary.hlsl"
// Or: #include "Packages/com.example.library/Shaders/MyLibrary.hlsl"
// Or: #include "./MyLibrary.hlsl"  // Relative to the containing document/FX asset.
```

Nested includes and include guards use Unity's shader preprocessor. `#include_with_pragmas` is
also supported. `UnityCG.cginc` is already included by the wrapper. Libraries must be compatible
with this fragment-shader environment; a complete ShaderLab shader or a pipeline-specific library
is not automatically a portable function library. Before the document's first save, relative paths
start at **Assets**. Click **Apply** again after changing a library. After saving to a different folder,
check relative paths before applying again; the previously applied shader remains available.

Built-in inputs: `_MainTex`, `_MainTex_TexelSize`, `_InputSize` and `_CanvasSize`
(`width, height, 1/width, 1/height`), plus `_PreviewScale` (full-size pixels per preview pixel).
Use canvas-relative distances for consistent reduced previews and full-resolution exports.
Embedded FX code, parameters and compiled shaders live inside the compositor's `.asset` file;
parameter edits and rendering do not regenerate shaders. External FX keep their own `.asset` file.
Save the compositor again to update its embedded output texture after editing an FX.

<a id="export"></a>
## Export formats

| Format | Alpha / behavior |
| :--- | :--- |
| **PNG** | Transparency preserved. |
| **TGA** | Transparency preserved. |
| **JPEG** | White background, quality 95. |
| **EXR** | Linear RGB + alpha, half-float output with ZIP compression. |
| **Texture2D (.asset)** | Readable native Unity texture, separate from the editable document. |

PNG/JPEG/TGA exported under `Assets` are imported as single Sprite assets; EXR as a linear texture.
Replacing an existing standalone Texture2D requires confirmation and preserves its references.
Other asset types and sub-assets are protected.

> [!NOTE]
> The compositor renders in 8-bit RGBA. EXR export does not add HDR range or recover lost precision.

<a id="shortcuts"></a>
## Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `Ctrl+S` | Save, or Save As for a new document. |
| `Ctrl+Z` | Undo. |
| `Ctrl+Y` / `Ctrl+Shift+Z` | Redo. |
| `[` / `]` | Decrease / increase brush size. |
| `X` | Swap foreground and background colors. |
| `RMB` | Temporary eraser. |
| `Shift`-drag / `Shift`-click | Axis-aligned stroke / line from the previous endpoint. |
| `T` | Toggle Preview Transform. |
| `B` | Select the Brush tool, retaining its Brush/Eraser mode. |
| `Enter` / `Escape` | Exit Transform / cancel the current transform drag. |
| `Ctrl` during pivot drag | Disable snapping. |

On macOS, `Cmd` also works for saving, selection, and Undo/Redo.

Unity Shortcut Manager commands are suspended **only while this window has focus**, so they do
not consume drawing keys. Text/numeric fields keep normal input. Unity shortcuts are restored
when the window loses focus or closes.

<a id="automation"></a>
## Agent API & CLI

Create compositor assets, import generated images as File layers, adjust transforms, organize groups,
target Outline/SDF and paint Drawing-layer strokes without operating the window.
The versioned JSON API supports batch validation, stable layer IDs, revision checks, Undo and preview rendering.

With Unity Pipeline installed, discover the optional `sprite_editor_*` commands through Unity CLI.
The public C# API also works without Pipeline. Read the [agent guide](AGENTS.md),
[API reference and workflows](Documentation~/AgentAPI.md), or start from the
[JSON examples](Documentation~/Examples). Commands become available after the plugin is compiled.

<a id="community"></a>
## Community & license

Questions, ideas, or something unexpected? Join **[Discord · RU / EN](https://discord.gg/kqmJjExuCf)**.
For reproducible bugs, open a [GitHub issue](https://github.com/DCFApixels/Unity-SpriteEditor/issues)
with your Unity version and reproduction steps.

Distributed under the **[MIT License](LICENSE.md)**.
