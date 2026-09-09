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
  <a href="#installation">Installation</a> ·
  <a href="#quick-start">Quick start</a> ·
  <a href="#shortcuts">Shortcuts</a> ·
  <a href="CHANGELOG.md">Changelog</a> ·
  <a href="https://github.com/DCFApixels/Unity-SpriteEditor/issues">Report an issue</a>
</p>

---

Create sprites, icons, patterns, and layered textures without leaving Unity. Combine source images,
paint on the Preview, and add procedural effects. Save the editable composition and its ready-to-use
texture in one asset — export a separate image only when you need one.

<p align="center">
  <a href="Documentation~/Images/sprite-editor-heart.jpg"><img src="Documentation~/Images/sprite-editor-heart.jpg" alt="Sprite Editor with the Heart document in tiled painting mode" width="720"></a>
</p>

> [!NOTE]
> Sprite Editor is an **Editor-only** tool built with UI Toolkit. Its saved textures and sprites
> can be used at runtime without rendering the layer stack.

## Features

| Area | Highlights |
| :--- | :--- |
| Layers | Images, drawing, fills, gradients, nested groups, multi-selection, and drag-and-drop. |
| Painting | Brush and eraser, straight lines, flood fill, and RGBA channel masks. |
| Patterns | Rotatable mirror symmetry, rows, grids, and radial repetition with boundary clipping. |
| Transforms | On-canvas handles, movable snapping pivot, source aspect ratio, tiling, and filtering. |
| Effects | Outline, SDF, blend modes, and embedded Shader FX with editable HLSL. |
| Output | Editable documents with Texture2D/Sprite output; layered PSD, PNG, JPEG, TGA, EXR, and Texture2D export. |
| Automation | C# and JSON APIs, optional CLI commands, and an agent guide. |

## Guide

[Workspace](#workspace) · [Layers](#layers) · [Painting & fill](#painting) ·
[Symmetry](#symmetry) · [Transforms](#transform) · [Effects](#effects) ·
[Saving & export](#saving) · [Shortcuts](#shortcuts) · [Agent API](#automation)

<a id="installation"></a>
## Installation

**Requires Unity 6 (`6000.0`) or newer.** Burst, Collections, and Newtonsoft Json are declared
in [package dependencies](package.json).

In **Window → Package Management → Package Manager**, choose **Install package from git URL**:

```text
https://github.com/DCFApixels/Unity-SpriteEditor.git
```

<details>
<summary>Install through manifest.json or pin a version</summary>

Add this entry to the `dependencies` object in `Packages/manifest.json`:

```json
"com.dcfa_pixels.sprite-editor": "https://github.com/DCFApixels/Unity-SpriteEditor.git"
```

This URL follows the default branch. Append `#<tag>` to pin an existing
[release tag](https://github.com/DCFApixels/Unity-SpriteEditor/tags).
The version badge shows [package.json](package.json), not the latest tag.

</details>

<a id="quick-start"></a>
## Quick start

1. Open **Window → Sprite Editor** and click **New**.
2. Set the canvas dimensions with **W** and **H** above the Preview.
3. Drag a texture from Project onto the Preview to add a File layer, or use **+ → Drawing Layer**
   in the Layers footer to start painting.
4. Select a Drawing layer, choose **Brush** (`B`), and draw. Its options appear above the Preview.
5. Press `Ctrl+S` to save the document and its full-resolution output.
6. Assign the saved asset to a texture field, or expand it in Project and use **Output Sprite**.

> [!TIP]
> Double-click the saved texture, sprite, or nested layer document to reopen it.
> **Assets → Open in Sprite Editor** and the layer document's Inspector button also work.

<a id="workspace"></a>
## Workspace

The **Preview is on the left**. **Layer Settings** and **Layers** are stacked on the right.
Drag the dividers to resize the panes; the right pane keeps its width when the window is resized.
Layer Settings follows the active layer, with **Transform** collapsed by default.
**Color & Blending** follows Transform and is also collapsed by default; its expansion state is shared
across layers within the window. The header's **Standard / HDR** dropdown sets both ranges in one
Undo step. A blank value means the ranges differ; expand the section to edit them independently.

The left toolbar selects the editing tool:

| Tool | Key | Use |
| :--- | :---: | :--- |
| No Tool | `V` | View the composition without editing handles or painting guides. Used when no saved tool is available. |
| Transform | `T` | Move, resize, and rotate a non-group layer. |
| Brush | `B` | Paint or erase on a Drawing layer. |
| Pencil | `P` | Draw crisp pixels with a Circle, Square or Diamond tip. |
| Fill | `G` | Fill a region or matching colors on a Drawing layer. |
| Zoom | `Z` | Click to zoom, or drag a rectangle to frame an area. |

Tool options live above the Preview; the row stays in place with No Tool selected.
Hold `Alt` (`Option` on macOS) with Brush, Pencil or Fill and click or drag to sample the
primary color from all layers. Sampling uses full canvas resolution, works in Tiled view,
and preserves HDR and alpha without exposure, channel filtering, checkerboard or Debug overlays.
It does not paint, add Undo entries or change the selected tool. Standard color input still
displays and paints the sampled color without HDR intensity.
The last selected tool is restored when reopening the window or reloading scripts, even without a compatible layer selected. Leaving Transform returns to the previous tool.
The footer always shows **Live Quality** on the left and **RGBA channels** on the right.

### Viewing controls

With Zoom selected, click to double the scale or `Alt`-click to halve it.
Drag a rectangle to frame an area, including empty space around the image.
With any tool selected, `MMB`-drag pans and the mouse wheel zooms around the cursor, including over
empty preview space. `Escape` ends navigation or cancels a pending area selection. **Fit** shows the whole canvas;
**100%** uses one UI unit per source pixel.

Zoom and pan affect only the view and survive tool/layer changes. Opening another document restores Fit.
**Live Quality** controls painting-time preview resolution: **12.5–100%**, default **80%**.
It does not change the saved canvas or export resolution.

Enable **Tiled** beside the canvas dimensions to repeat the composition across the whole Preview.
Paint on any copy: brush and eraser footprints wrap across canvas edges and corners.
Layer scale, rotation and position remain editable; no Apply Transform step is required.
Painting still affects the Drawing layer's source pixels, so a transformed layer only receives marks
where its source frame covers the canvas. Tiled viewing does not change the document or export size.

### Tool settings and layer settings

Brush/Eraser mode, colors, size, hardness, spacing, and Fill options are shared tool preferences:
switching layers or documents keeps them. **Changing tool settings does not add Undo steps.**
Symmetry, repetition, and transforms belong to individual layers and remain undoable document edits.

Brush, Pencil and Fill can be selected and configured even without a Drawing layer; their icons are dimmed
when they cannot act. The brush cursor remains visible. Clicking a non-Drawing layer offers
conversion with **Keep Transform**, **Apply Transform**, or **Cancel**.
That click never paints or fills — click again after conversion.

<details>
<summary>Separate Properties windows and resetting the workspace</summary>

Each **⋮ → Properties** invocation on a layer opens a separate window. **FX** is in the same row menu.

Use **window tab ⋮ → Reset Sprite Editor Settings…** to reset panel sizes, scrolling, selection,
foldouts, preview tools, shared painting preferences, channels, and Live Quality.
The confirmed reset applies to all open Sprite Editor windows and has no Undo.
It does not delete documents, unsaved work, per-layer settings, or assets, and does not change
Unity settings or docking.

</details>

<a id="layers"></a>
## Layers & groups

**Layer types:** File · Drawing · Color Fill · Gradient · Outline · SDF · Group.

Drag textures from Project into Layers to place them between rows or inside a group.
Dropping onto the Preview adds File layers at the top of the root list.
The first texture assigned to an empty File layer gets **Original Aspect** automatically;
replacing an existing source keeps its transform. Source assets are not modified.

### Selection and organization

Click to select a layer, `Ctrl`-click to toggle one, `Shift`-click for a visible range,
or `Ctrl+Shift`-click to add a range. The **last selected layer is active**, marked with a subtle
brighter blue border. Painting, transforms, and Layer Settings operate on that layer.

The layer table aligns visibility, Name, opacity, and Blend columns. Click the eye in its header
to show every layer and group. Click a group's arrow to expand/collapse it, or drag the arrow to move it.

Drag a selected row's background or thumbnail to move the selection together. Drop into a group to nest it,
or use the footer:

| Footer icon | Click | Drop selected layers |
| :--- | :--- | :--- |
| **+** | Add a layer. | Duplicate the selection. |
| **Folder** | Group selected layers. | Group the dragged selection. |
| **Trash** | Delete selected layers. | Delete the dragged selection. |

**⋮ → Duplicate** copies a single layer or a whole group. Copies appear above their originals.
Drawing pixels and embedded Shader FX are independent; external assets remain linked.
Targets within the copied set follow the copies. Moving, grouping, deleting, and duplicating
a selection support Undo/Redo.

**Merge selection:** `Ctrl+E` replaces selected layers with one Drawing layer; `Ctrl+Alt+E` creates
a merged copy and keeps the originals enabled. Both commands are also available from **⋮** or a right-click
on the layer row. Right-clicking a selected row preserves the selection; an unselected row becomes selected.
Groups include their descendants once. Visible content, transforms, opacity and FX are baked at full
canvas resolution into half-float pixels, with an identity transform. The result is placed above the
top selected branch in the nearest common group. Each merge is one Undo step, without a confirmation dialog.
Blending against unselected content can change. Effects targeting removed layers are redirected to the
merged result; effects using Previous retain their former input where it still exists.

Groups default to **Pass Through**: children blend directly into the surrounding stack.
Choose another blend mode to isolate a group. Opacity applies to the whole group, including nested groups.
Outline/SDF can use a group's own content as input. Group transforms and FX are not supported.

<details>
<summary>Layer names, opacity, and duplication details</summary>

Each type has an independent document-local name counter: `Layer n` for Drawing, `File n`,
`Color Fill n`, `Gradient n`, `Outline n`, `SDF n`, and `Group n`.
Duplicates retain the full name and append ` Copy n`, using a separate shared copy counter.
Deleted numbers are not reused.

Number keys set the selected layers' and groups' opacity: `5` → 50%, then `7` within 0.6 seconds → 57%.
After a pause, `7` → 70%. `0` means 100%; quick `00` means 0%, and `05` means 5%.
A quick pair is one Undo step. This changes layer opacity, not brush opacity.

A selected group carries its descendants only once. Duplicated effects retain their inputs:
if a Previous input is no longer adjacent, the copy switches to an explicit target.
Clicking opacity or blend fields on selected rows preserves the selection; editing either assigns
the same value to all selected rows in one Undo step. Unselected children of selected groups are
unchanged. **Pass Through** applies only to selected groups.
Other inline controls and row menus operate on that row, except **Merge Selected**, which uses the selection.

</details>

<details>
<summary>Convert to Drawing and bake a transform</summary>

**⋮ → Convert to Drawing** works on any layer, including an existing Drawing layer:

| Mode | Result |
| :--- | :--- |
| **Keep Transform** | Rasterize the source and keep its transform editable. |
| **Apply Transform** | Bake the transform and tiling into full-resolution pixels; reset the transform and set tiling to Clip. |

The replacement retains its ID, name, position in the stack, visibility, opacity, blend mode,
and live FX. Drawing layers retain repetition settings. Outline/SDF become static pixels.
Applying the transform crops pixels beyond the canvas. Conversion supports Undo/Redo.

Groups flatten their visible descendants, transforms, and FX against transparency.
Both modes produce a Drawing layer with Normal blending, opacity 1, and an identity transform.
Confirmation is required: interactions with outside layers may change, and effects targeting
removed descendants lose their targets. References to the converted group itself remain valid.

</details>

<a id="painting"></a>
## Painting & fill

### Brush and Pencil

**Pencil (`P`)** shares colors, paint/erase mode, `RMB` erasing, `X`, Shift lines, channel masks and
layer symmetry with Brush. Its tips have no soft edge: choose **Circle** (default), **Square** or
**Diamond**. Pencil size is independent of Brush and starts at **1 px**. For both tools, `[` / `]`
change size by 1 px below 20 px, then by roughly 10% (rounded down). There is no pencil hardness
or spacing control; strokes follow a continuous pixel grid.
Pencil settings are shared across layers, saved with tool preferences and excluded from Undo.
At close zoom, Circle and Diamond cursors follow the exact pixel boundary. Small on-screen pixels
or outlines exceeding 512 segments use a simplified contour. Cursor movement reuses cached geometry.
While Pencil is selected, Preview uses Point filtering and full canvas resolution. Live Quality is
temporarily locked at 100%; switching tools restores your saved quality setting and normal filtering.

Select a Drawing layer and choose Brush (`B`). Paint on the Preview while seeing the full stack,
including blending and effects.

- **Size**, **Hard**, and **Step** control diameter, hardness, and spacing between brush stamps.
  Step is a percentage of brush diameter. Drag the **Size** or **Step** label to adjust its value.
- Two swatches hold foreground/background colors; `X` swaps them.
- `LMB` uses the selected Brush/Eraser mode; `RMB` temporarily erases.
- `Shift`-drag locks a line horizontally or vertically; `Shift`-click connects the previous
  painted endpoint to the new point.

<details>
<summary>Straight-line behavior</summary>

To start an independent axis-aligned line, click without `Shift`, then hold `Shift` while dragging.
The first movement chooses the axis in canvas space. Repeated `Shift`-clicks make connected segments;
eraser, spacing, symmetry, and repetition also apply.

Clip constrains the connection to its starting region. Undo/Redo, clearing the layer, and switching
documents reset the connection anchor. Connections do not carry across Drawing layers or canvas sizes.

</details>

### Fill

Choose Fill (`G`) and click. It writes the foreground color into the **active Drawing layer only**,
even when sampling the whole composition. Each fill is one Undo/Redo step.

| Option | Behavior | Default |
| :--- | :--- | :--- |
| **All Layers** | Off: sample source-layer pixels before transform, opacity, and FX. On: sample the visible composition at full resolution. | Off |
| **Contiguous** | On: fill the connected region at the click. Off: fill all matching pixels, including separate regions. | On |
| **Tolerance** | Color/alpha similarity threshold, `0–255`. | `32` |
| **Antialias** | Soften the outer edge with partial coverage. | On |
| **Expand (px)** | Grow the region under a contour, `0–32` source pixels. Independent of Tolerance. | `0` |

<details>
<summary>Fill boundaries and transformed layers</summary>

Contiguous uses four-connected neighbors. Fully transparent pixels ignore hidden RGB when compared.
Tolerance, Antialias, and Expand work with either sampling mode and with Contiguous on or off.

Fill edits the primary source frame; it does not repeat through brush symmetry.
For tiled copies outside that frame, use **Convert to Drawing → Apply Transform** first.
All Layers sampling is projected into the source pixel grid. For canvas-pixel-accurate boundaries,
use an untransformed, canvas-sized Drawing layer.

Live Quality does not reduce All Layers sampling resolution.
The memory-safety limit is 16,777,216 pixels per source/reference image.

</details>

### RGBA channels

The footer's **R / G / B / A** buttons control both Preview display and the paint color mask.

- Disabled RGB channels are written as `0` in new brush strokes and fills.
- With A disabled, brush strokes and fills leave no mark. The eraser ignores the mask.
- A single selected R, G, or B channel is displayed in grayscale, with transparency if A is enabled.
  A alone shows opaque grayscale alpha. Two or three RGB channels retain their channel colors;
  A off ignores transparency. All channels off shows black.

Channel switches leave existing pixels, selected colors, and Save/Export output unchanged.
New masked strokes and fills are real pixel edits with Undo/Redo.

<a id="symmetry"></a>
## Symmetry & repetition

Open **Symmetry & Repeat** in a Drawing layer's settings. Each layer keeps its own configuration;
changing it affects subsequent strokes, not existing pixels.

| Mode | Controls |
| :--- | :--- |
| **None** | Paint without copies. |
| **Mirror** | Reflect across X and/or Y axes through **Center**. **Angle (°)** rotates both axes, `0–360°`. |
| **Horizontal / Vertical** | Repeat in a row or column with adjustable count. |
| **Grid** | Repeat with independent X/Y counts. |
| **Radial** | Repeat around **Center**, with sector count and **Start Angle (°)**, `0–360°`. |

At 0°, Mirror X reflects across the vertical axis and Mirror Y across the horizontal axis.
Mirror angles rotate counterclockwise in source-pixel space. Radial's start angle rotates
counterclockwise from the left; its angle is independent of Mirror's.

**Elements → Copy / Alternate Mirror** controls ordinary or alternating reflected copies
in Horizontal, Vertical, Grid, and Radial. Mirror is a separate mode, not an extra reflection
on top of these modes.

**Edges → Continue / Clip** works in Mirror and all repeating modes.
Continue lets a stroke cross region boundaries. Clip stops it at the boundary of its starting region
and clips each copy's brush footprint to its own region. The active copy stays under the cursor.

<a id="transform"></a>
## Transform & pivot

Select a non-group layer and choose Transform (`T`). Use the Preview handles or expand
**Transform** in Layer Settings for numeric input.

- Drag inside the frame to move, an edge/corner handle to resize, or the round handle to rotate.
- Drag the **gold pivot cross** to reposition the pivot without moving the image.
  It snaps to nine anchors: corners, edge midpoints, and center. Hold `Ctrl` to bypass snapping.
- Hold `Shift` for axis-constrained movement, proportional resizing, or 15° rotation steps.
- `Escape` cancels a drag; `T` / `Enter` exits Transform.

**Reset** restores the transform. **Original Aspect** fits source proportions inside the current
frame by shrinking one axis, preserving image center, pivot, rotation, flips, and tiling.
Filter is independent and survives Transform Reset.

<details>
<summary>Tiling, filtering, and coordinate details</summary>

| Tiling | Result |
| :--- | :--- |
| **Clip** — default | Pixels outside the frame are transparent. |
| **Repeat** | Tile the source texture. |
| **Mirror** | Alternate reflected tiles. |
| **Source** | Inherit the source's wrap modes, including separate U/V settings. Clamp extends edge pixels instead of leaving transparency. |

**Filter:** Source (default) inherits the texture's Filter Mode; Point preserves hard pixel edges;
Bilinear smooths sampling; Trilinear blends existing mip levels. Without mipmaps, Trilinear behaves
like Bilinear. Generated layers use Bilinear for Source. Import settings are never modified.

The frame includes transparent source pixels. Position uses output pixels, Pivot normalized coordinates,
Scale a multiplier, and Rotation degrees. Pivot snapping uses a 10-UI-pixel radius;
the pivot may lie outside the frame. Each drag is one Undo step.

Original Aspect uses File texture dimensions, Drawing stored pixels, or the canvas ratio for generated
layers. Missing File sources and near-zero scales disable it; near-zero scales also disable pivot dragging.

On Drawing layers, transform tiling creates rendered copies while painting edits the source tile.
Brush repetition is a separate feature.

</details>

<a id="effects"></a>
## Effects

### Outline & SDF

Use the item directly below the effect in the same group (**Previous**), or choose a layer/group
explicitly (**Specific**). Drag a layer onto **Target** to assign it; with a dragged selection,
the active layer is used. This also works in separate Properties windows.
Cross-document, self-referencing, and cyclic targets are rejected.

Group inputs combine visible descendants' alpha without isolating their color blending.
Available distance metrics are **exact Euclidean EDT**, approximate Euclidean, Manhattan, and Chebyshev.
Width, softness, and maximum distance use output pixels; processing uses Burst and Native Collections.

### Blending

RGB blend functions apply where layers overlap; non-overlapping regions retain the present layer's
color and alpha through source-over compositing. **Overwrite** instead replaces complete RGBA pixels,
with layer opacity interpolating between old and new pixels. **None** leaves the composite unchanged.

<details>
<summary>Blend mode list</summary>

Normal · Add · Subtract · Multiply · Divide · Screen · Overlay · Darken · Lighten ·
Color Dodge · Color Burn · Linear Dodge (Add) · Linear Burn · Linear Light ·
Linear Light Add/Sub · Vivid Light · Pin Light · Hard Mix · Hard Light · Soft Light ·
Difference · Exclusion · Negation · None · Overwrite.

</details>

### Shader FX

Write a fragment effect directly in layer settings — no separate shader or material file required.

1. Click **+ Shader FX** in the layer's **FX** section.
2. Write `ApplyFX`, add parameters, and click **Apply**.
3. Adjust parameter values for immediate feedback. Save the document to keep the embedded effect
   and update the output texture.

For example, add a **Float** parameter named `_Amount`:

```hlsl
float4 ApplyFX(float2 uv, float4 color)
{
    return float4(lerp(color.rgb, 1.0 - color.rgb, saturate(_Amount)), color.a);
}
```

Parameters support **Float**, **Color**, **Vector**, and **Texture2D**; their uniforms are generated.
Code and parameter declarations remain drafts until Apply. Failed compilation keeps the last working
effect. The code editor supports Undo/Redo, including caret and selection; restored code still needs Apply.

<details>
<summary>Libraries, shader inputs, and reusable effects</summary>

Use standard `#include` with project, package, or relative paths:

```hlsl
#include "Assets/Shaders/MyLibrary.hlsl"
#include "Packages/com.example.library/Shaders/MyLibrary.hlsl"
#include "./MyLibrary.hlsl"
```

These are path examples, not bundled libraries. Relative paths start at the document/FX asset's folder,
or **Assets** before its first save. Nested includes, guards, and `#include_with_pragmas` use Unity's
preprocessor; `UnityCG.cginc` is already included. Libraries must suit this fragment-shader environment.
After editing a library, click Apply again. Check relative paths after saving into another folder.

`SampleInput(uv)` reads the layer after earlier modifiers. Return **straight RGBA**; opacity and blending
are applied afterwards. Built-in inputs include `_MainTex`, `_MainTex_TexelSize`, `_InputSize`,
`_CanvasSize` (`width, height, 1/width, 1/height`), and `_PreviewScale` (full-size pixels per preview pixel).
Do not redeclare generated parameter uniforms.

FX run in list order after the layer transform; each Shader FX is one GPU pass without CPU readback.
Materials are also supported as modifiers. Embedded code, parameters, and compiled shaders live in the
document; parameter edits do not regenerate shaders.

**+ Reference** links a reusable external FX; changes affect all its users.
**Embed** makes an independent document-owned copy without changing the original asset.
Save As and layer duplication copy embedded FX independently.

</details>

<a id="saving"></a>
## Saving & export

The document header provides **New · document field · Save · Save As · Export**.

| Action | Result |
| :--- | :--- |
| **Save** | Update the current document and output without a dialog. Disabled before the first save. |
| **Save As** | Create an independent editable document and outputs. |
| `Ctrl+S` | Save an existing document, or open Save As for a new one. |
| **Export** | Write a separate image or a layered PSD. |

### Use the saved asset directly

The `.asset` contains the layer document, Drawing textures, embedded FX, a full-resolution
**Texture2D** as its main object, and **Output Sprite**. External source images and referenced FX
remain linked. Project displays a thumbnail from saved pixels.

Assign the main texture to a material, RawImage, or Texture2D field. Expand the asset to assign
Output Sprite to a SpriteRenderer or UI Image. Subsequent saves preserve output references,
including after canvas resizing. Output Sprite uses a centered pivot, full-rectangle mesh, and 100 PPU.

> [!IMPORTANT]
> Output reflects the last explicit Sprite Editor save. Save again after Undo/Redo, FX edits,
> or changes to external source textures to update the texture used elsewhere in Unity.

<a id="export"></a>
### Export formats

| Format | Result |
| :--- | :--- |
| **PNG / TGA** | Transparency preserved. |
| **JPEG** | White background, quality 95. |
| **EXR** | Linear RGB + alpha, half-float with ZIP compression. |
| **PSD** | Nested groups, raster layers, editable fills/gradients and compatible Outline strokes; includes a merged image. |
| **Texture2D (.asset)** | Readable standalone Unity texture, separate from the editable document. |

PNG/JPEG/TGA exported into `Assets` are imported as single Sprite assets; EXR as a linear texture.
Replacing a standalone Texture2D asks for confirmation and preserves references.
Other asset types and sub-assets are protected.

The compositor uses **linear HDR** working pixels. Layer **Color Range** and **Blend Range** independently
control bounded and extended behavior. Drawing starts in 8-bit storage and promotes to half-float for HDR;
returning to Standard does not discard stored HDR. Explicit **Convert to 8-bit** supports Undo.
EXR and Texture2D output preserve HDR; PNG/JPEG/TGA/PSD clamp an export copy.

The **HDR** toggle beside **EV** in the preview footer switches all color and gradient pickers without changing
stored values or layer ranges. Standard displays and paints colors without HDR intensity; switching back
restores the stored HDR colors. Editing a color replaces it. The toggle defaults to off, persists and stays outside Undo.
Preview **EV** and the **bug button** control exposure and numeric-error visualization only.
See [HDR, storage and group behavior](Documentation~/HDR.md).

**Layered PSD** prioritizes structure and editability. It preserves layer names, order, visibility,
opacity and supported blend modes. SDF, Shader FX and incompatible procedural settings are rasterized;
unsupported blends are approximated. Outline can remain a stroke effect on a snapshot of its target alpha.
The export summary links these compromises to individual layers through Console notes.
Keep the native document as the source: PSD does not retain live effect-target links or shader code.
See [PSD export details and API](Documentation~/PsdExport.md).

<a id="shortcuts"></a>
## Shortcuts

Tool keys: `V` — No Tool · `T` — Transform · `B` — Brush · `P` — Pencil · `G` — Fill · `Z` — Zoom.

| Shortcut | Action |
| :--- | :--- |
| `Ctrl+S` | Save / Save As. |
| `Ctrl+Z` | Undo. |
| `Ctrl+E` / `Ctrl+Alt+E` | Merge selected layers / create a merged copy. |
| `Ctrl+Y` / `Ctrl+Shift+Z` | Redo. |
| `[` / `]` | Decrease / increase brush size. |
| `X` | Swap foreground/background colors. |
| `Alt`-click / drag with Brush, Pencil or Fill | Temporarily sample the primary color from the composition. |
| `RMB` | Temporarily erase with Brush. |
| `Shift`-drag / `Shift`-click with Brush | Axis-aligned line / connection from the previous endpoint. |
| `0`–`9` or numpad | Set active-layer opacity; quick pairs enter a percentage. |
| `Ctrl`-click / `Shift`-click on layers | Toggle a layer / select a range. |
| `Ctrl` during pivot drag | Disable snapping. |
| `Alt`-click with Zoom | Zoom out. |
| `MMB`-drag / mouse wheel with any tool | Pan / zoom around the cursor. |
| `Enter` in Transform | Exit the tool. |
| `Escape` during a transform or zoom gesture | Cancel the gesture. |

On macOS, `Cmd` also works for saving, selection, and Undo/Redo.
Text/numeric fields keep normal input. Unity Shortcut Manager commands are suspended only while
Sprite Editor has focus, and restored when it loses focus or closes.

<a id="automation"></a>
## Agent API & CLI

Create documents, import images, organize groups, adjust transforms, target effects, and paint
Drawing-layer strokes without operating the window. The versioned JSON API supports batches,
dry-run validation, stable IDs, revision checks, Undo, and preview rendering.

The public C# API works without Pipeline. With Unity Pipeline installed, the optional
`sprite_editor_*` commands expose it through Unity CLI.

- [Agent guide](AGENTS.md) — workflow and safety rules.
- [API reference](Documentation~/AgentAPI.md) — operations, coordinates, limits, and full workflows.
- [JSON examples](Documentation~/Examples) — starting requests.

<a id="community"></a>
## Community & license

Questions or ideas? Join **[Discord · RU / EN](https://discord.gg/kqmJjExuCf)**.
For bugs, open a [GitHub issue](https://github.com/DCFApixels/Unity-SpriteEditor/issues)
with your Unity version and reproduction steps.

Distributed under the **[MIT License](LICENSE.md)**.
