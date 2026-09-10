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
| Effects | Outline, SDF, Normal Map, Gaussian Blur, Motion Blur, blend modes, and embedded Shader FX with editable HLSL. |
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
| Rectangle Select | `M` | Drag a rectangular pixel selection. |
| Polygonal Lasso | `L` | Click vertices to select a polygonal area. |
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
<summary>User settings, separate Properties windows and workspace reset</summary>

Each **⋮ → Properties** invocation on a layer opens a separate window. **FX** is in the same row menu.

Open **window tab ⋮ → User Settings…** to choose the light/dark checkerboard colors, cell size (1–128 UI pixels, default 16), and the
invalid-pixel highlight color. Changes update all open previews immediately and persist for your
user account, without changing documents, exports or Undo. **Reset Preview Appearance** restores the defaults.

Use **window tab ⋮ → Reset Sprite Editor Settings…** to reset panel sizes, scrolling, selection,
foldouts, preview tools, shared painting preferences, preview colors, channels, and Live Quality.
The confirmed reset applies to all open Sprite Editor windows and has no Undo.
It does not delete documents, unsaved work, per-layer settings, or assets, and does not change
Unity settings or docking.

</details>

<a id="layers"></a>
## Layers & groups

**Layer types:** File · Drawing · Color Fill · Gradient · Noise · Outline · SDF · Normal Map · Gaussian Blur · Motion Blur · Shader Processor · Group.

Drag textures from Project into Layers to place them between rows or inside a group.
Dropping onto the Preview adds File layers at the top of the root list.
The first texture assigned to an empty File layer gets **Original Aspect** automatically;
replacing an existing source keeps its transform. Source assets are not modified.

### Procedural noise

Add **Noise Layer** for GPU-generated OpenSimplex2, OpenSimplex2S, Perlin, Value, Value Cubic or Cellular noise.
Drag the **Scale**, **Offset** and other numeric labels, or use the sliders: the preview updates during editing,
without rebuilding the settings panel or waiting for the gesture to finish. Seed, fractals and Domain Warp
are stored as parameters, not full-size pixel snapshots.

**Scale** is measured across the shorter canvas side; higher values give finer detail. **Offset** uses noise-space
units. **Color Values** outputs display-oriented grayscale; **Linear Data** preserves raw 0–1 values for masks,
height maps and channel packing. RGB contains the same scalar value and alpha is 1; use Swizzle and blending
for channel assignment. Transform, clipping and Shader FX remain available.

Generation runs directly at the requested render resolution. The existing normal preview limit is 512 px;
full-resolution export uses the same coordinates and seed. There is no new low/high-resolution refinement stage
or background animation. Ordinary tiled preview repeats the result; noise is not inherently seamless.
Powered by FastNoiseLite HLSL; see [third-party notices](ThirdPartyNotices.md).

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
| **Page +** | Create an empty Drawing layer above the active layer. | Merge the dragged layers/groups into a new Drawing layer, keeping the originals. |
| **Folder** | Group selected layers. | Group the dragged selection. |
| **Trash** | Delete selected layers. | Delete the dragged selection. |

Commands in **⋮** or the row's right-click menu apply to the entire selection. Group-only commands
affect selected groups; **Properties** and **FX** open a separate window for each supported layer.
Selected groups carry their descendants once when moving, duplicating, converting or deleting.

**⋮ → Duplicate** copies selected layers and whole groups. Copies appear above their originals.
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

### Area selection and clipboard

**Rectangle Select** (`M`) and **Polygonal Lasso** (`L`) share a canvas-space selection.
For the lasso, click vertices and finish with `Enter`, a double-click, the first vertex, or **Close**.
`Backspace` / RMB removes the last vertex; `Esc` cancels the unfinished shape. The header offers
Replace, Add, Subtract and Intersect; `Shift` adds, `Alt` subtracts, and `Shift+Alt` intersects.

- `Ctrl`-click a layer thumbnail (the foldout icon for a group) to select its alpha, including
  transform, FX, Swizzle and clipping. Outer visibility/opacity are ignored; a group's child
  visibility/opacity are retained. `Ctrl`-click elsewhere on a row still toggles layer multi-selection.
- The active area limits Brush, Pencil, erasing and Fill. Switching layers or tools keeps it.
  `Ctrl+A` selects all, `Ctrl+D` deselects, and `Ctrl+Shift+I` inverts. An empty active area blocks painting.
- `Ctrl+C` copies the selected area of the active layer/group with its rendered settings;
  `Ctrl+Shift+C` copies the visible composition. Without an area selection, copying uses the whole canvas.
- `Ctrl+V` adds an independent Drawing layer at the top of the root stack, with identity transform
  and HDR pixels. Position is preserved for equal-sized canvases; different-sized canvases center the
  copied region and clip to the destination. Paste supports Undo/Redo and does not reapply the current mask.

The clipboard is internal to Sprite Editor, shared between its windows until script reload/editor exit;
it is not the system image clipboard. Copy ignores preview exposure, channels and Live Quality.
Area selection is temporary window state, not an asset setting or Undo step: it clears on document
switch, canvas resize, window close or script reload. It does not restrict independent agent API strokes.
It supports up to 16,777,216 canvas pixels. Tiled preview wraps selection coverage at canvas edges;
very complex contours use display-only LOD, and polygon gestures are limited to 256 vertices and
16 repeated canvas heights. Painting still uses the full-resolution mask.

### Layer clipping masks

Enable **Clipping Mask** in a layer's context menu, or `Alt`-click the boundary above its base.
The menu applies to all selected layers; the boundary gesture toggles just the upper sibling.
A `↳` marker identifies clipped layers. Several consecutive clipped layers share the first
non-clipped sibling below them, within the same group. Reordering changes that base.

The chain keeps the base's alpha, including soft edges, and its opacity applies once to the
whole result. Upper layers keep their colors, blend modes, opacity, transforms, FX and Swizzle;
they cannot expand the base's coverage. Clipped **Overwrite** replaces color within its source
coverage without erasing base alpha. A hidden, transparent or missing base hides the chain.
Groups participating as a base or clipped layer are temporarily isolated; a configured
Pass Through group uses Normal until it leaves the chain.

Clipping is non-destructive and supports Undo/Redo, copies, conversion to Drawing, agent API,
and native PSD clipping flags. Outline/SDF targets see clipped coverage. Merging bakes clipping;
when the base is not selected, it supplies only the mask, not its color, so blend-dependent
results can change as with other partial merges.

<details>
<summary>Layer names, opacity, and duplication details</summary>

Each type has an independent document-local name counter: `Layer n` for Drawing, `File n`,
`Color Fill n`, `Gradient n`, `Noise n`, `Outline n`, `SDF n`, `Normal Map n`, `Gaussian Blur n`, `Motion Blur n`, and `Group n`.
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

Effect layers (Outline, SDF, Normal Map, Gaussian Blur and Motion Blur) can process hidden sources in both Previous and
Specific modes. The source's eye toggle controls its own contribution to the composition, not
its availability as an effect input. A hidden group still supplies its visible children;
individually hidden children remain excluded. Source opacity and clipping behavior are unchanged.

Group inputs use grayscale coverage from the group's own isolated composition, respecting visible
descendants' blending and opacity without the external backdrop.
Available distance metrics are **exact Euclidean EDT**, approximate Euclidean, Manhattan, and Chebyshev.
Width, softness, and maximum distance use output pixels; processing uses Burst and Native Collections.

### Gaussian Blur

Add **Gaussian Blur Layer** and use **Previous** or assign a layer/group with **Specific**.
Hidden sources remain usable. A group supplies its isolated content; its normal composition can
remain Pass Through. **Radius** is the kernel extent in original canvas pixels (0–256, default 8);
zero bypasses the blur. **Edges** offers Transparent (default), Clamp, Repeat and Mirror.
Use Repeat for seamless textures; enabling Tiled preview does not change the effect's edge mode.

Color and alpha are blurred together in premultiplied linear light. Set the effect's Color Range
to HDR to retain intensities above 1. While painting or changing settings, large kernels use a
reduced-resolution approximation; after editing settles, the Preview refines automatically.
Save, export and conversion use the full-quality algorithm; PSD stores the result as a raster layer.
The main window shares a bounded cache of effect results and group sources, separate from Undo.
See [rendering and cache details](Documentation~/GaussianBlur.md) and
[agent settings](Documentation~/AgentAPI.md#gaussian-blur-settings).

### Motion Blur

Add **Motion Blur** and choose a **Previous** or **Specific** source, including a hidden layer or
an isolated group. **Linear** uses Distance (0–512 canvas pixels) and Angle; **Circular** uses
Arc (0–360 degrees) and a normalized Center, with `(0.5, 0.5)` at the canvas center.
Circular follows a rotation arc, not a zoom toward the center.

**Strength** (0–400%, default 100%) mixes with the original below 100%; above 100%, it makes
translucent trails denser without changing their length or color brightness. Fully opaque areas
are unchanged above 100%. At 0%, the effect returns the original image.

**Direction** selects Centered, Forward or Backward. Forward follows the linear angle or turns
counterclockwise around the circular center. **Edges** offers Transparent, Clamp, Repeat and Mirror.
Like Gaussian Blur, filtering respects transparency and HDR, uses the shared effect cache and
refines the main preview after editing. Large circular arcs have a bounded sampling cost and can
show discrete traces on fine details. PSD export rasterizes the result.
See [rendering details](Documentation~/MotionBlur.md) and [agent settings](Documentation~/AgentAPI.md#motion-blur-settings).

### Normal Map

**Settings → Simple / Advanced** changes only the visible controls in Layer Settings and Properties.
Simple is the default; Advanced exposes the full set, grouped into Source, Surface, Height Levels,
Texture Detail and Output. The choice is remembered for the Editor session. Hidden values are preserved;
a small link in Simple indicates modified advanced settings. Texture Detail applies only to Texture generation.

Add a **Normal Map** layer and select **Previous** or a **Specific** source, including drag-and-drop
onto Target. Groups supply their own color composition against transparency, without the external
backdrop. The source remains editable; conversion to Drawing or PSD export bakes the generated pixels.

- **Height Map** converts luminance, R/G/B, alpha or maximum RGB into tangent-space normals.
- **Texture** estimates relief from three brightness bands. Tune fine/medium/large detail,
  their radii, and Light Removal to reduce broad lighting gradients. This is an approximation,
  not recovered geometry; shadows and painted color differences can become relief.
- Adjust strength, black/white levels, gamma, height inversion, smoothing, Sobel/Scharr/central
  derivatives, Flip X/Y, edge sampling (Clamp/Repeat/Mirror), and opaque/source alpha.
  Ignore Transparent prevents hidden RGB from producing fringes; Alpha height intentionally
  treats transparency as height. Radii and strength use full-resolution canvas pixels.
- **Output → Height** shows the reconstructed height for tuning; return to **Normal** for export.
- **Input Space → Color Values** uses displayed RGB. Use **Linear** for data textures imported
  without sRGB. The Alpha channel is unaffected by this setting.
- Default **Encoding → Packed Color** preserves packed normal values in PNG/TGA/PSD and the usual
  preview. Use **Linear Data** for raw vector data in linear EXR or Texture2D output; its color
  preview will look brighter. A flat normal is `(0.5, 0.5, 1)` in the intended output encoding.

For a usable normal texture, leave Normal blend, full opacity, identity Swizzle and no color FX
on the result. Ordinary compositing does not renormalize mixed normals; the layer Transform moves
the generated image without reorienting its vectors. Import an exported PNG/TGA as **Normal Map**,
with grayscale conversion disabled. Existing import settings are never changed automatically.
See [normal-map API settings](Documentation~/AgentAPI.md#normal-map-settings).

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

### Shader Processor

Add **Shader Processor** to process the already-composited image **below its position**.
Its embedded Shader FX editor uses the same `ApplyFX` / `SampleInput` interface and `#include` support
as other layers. Transform, Swizzle and chained modifiers apply to this input; Gaussian Blur remains
an independent, targeted effect layer.

With **Normal**, Opacity interpolates the original and processed result in premultiplied linear light;
at 100% the result replaces the input, including alpha. Other blend modes combine the result with the
input. Both ranges default to HDR. Hide the Processor to bypass it.

In a Pass Through group it sees the external backdrop too. An isolated group confines processing to
its own children. Standalone previews, rasterization and effect targets evaluate the lower siblings
against transparency. Processors are clipping-chain boundaries, not clipping layers or bases.
PSD export bakes the composite and retains original layers in a hidden **Source Layers** folder.

### Post FX preview

The **Post FX** footer button enables presentation-only post-processing. Its arrow opens a settings
drawer beside the layer panes; closing the drawer leaves processing enabled. Painting, eyedropper,
fill sampling, saved textures and exports continue using the unprocessed composition.

- **Scene View** inherits the active scene view's post-processing switch and volume selection.
- **Game Camera** inherits a chosen camera's settings and volumes; an empty field uses MainCamera.
- **Profile** evaluates a selected Volume Profile with manually controlled camera parameters.
- **Solid** uses constant depth; **Alpha Height** maps alpha 1 to Distance and alpha 0 to
  Distance + Depth Range; **Alpha Mask** places alpha below Threshold at the far plane. Invert
  reverses alpha before mapping. Depth comes from original alpha, not the displayed channel mask.
- **Background → Mode** selects **Solid Color** (default) or **Checkerboard**, both opaque underlays
  before processing. The solid color is shared with **User Settings → Post FX Preview**. Checkerboard
  uses the transparency checker colors and cell size from User Settings; cells are measured in canvas
  pixels, zoom with the image and remain stable across Live Quality changes. Soft edges blend with
  the selected background; the original alpha still controls synthetic depth.
- **Link to Zoom** optionally changes simulated distance. **Animate** refreshes time-varying effects
  up to 8 fps; otherwise changes invalidate the preview and volume settings are checked periodically.

The optional adapter currently supports **URP 17.x with Universal Renderer**. No URP installation is
required to use the editor or Shader Processor. Unsupported pipelines/renderers show a notice and
the original preview. The isolated opaque surface runs through the selected **Universal Renderer's
active Renderer Features**, including Full Screen Pass and SSAO. Depth and world-space normals
describe the same alpha relief; SSAO can read either Depth or Depth Normals. Color copies, depth
textures and normal passes are scheduled by URP only when a feature, post-effect or rendering mode
requests them. The ordinary opaque depth attachment is still needed. Disabling Post FX stops these
preview passes and releases editor-owned resources; URP can retain its shared resource pools.

This is a synthetic surface, not a copy of the scene: no scene geometry, lighting or camera stacks
are rendered, and scene-object filters may exclude it. Features must declare their texture inputs
and support offscreen cameras. Projection can be inherited or overridden; the pipeline's render
scale applies. TAA, Motion Blur and AO temporal filtering are skipped; STP is unsupported and shows
the original preview with a notice. Other features depending on motion history, real geometry,
lighting or material buffers are not guaranteed. Existing cameras, profiles and project settings
are never edited. Renderer Feature and referenced material edits also invalidate the preview.

<a id="saving"></a>
## Saving & export

The document header provides **New · document field · Save · Save As · Export**.

| Action | Result |
| :--- | :--- |
| **Save** | Update the current document and output without a dialog. Disabled before the first save or when there are no unsaved changes. |
| **Save As** | Create an independent editable document and outputs. |
| `Ctrl+S` | Save an existing document, or open Save As for a new one. |
| **Export** | Write a separate image or a layered PSD. |

A warning symbol on **Save As** indicates a document with no saved file. Closing a changed document
opens Unity's **Save / Discard / Cancel** dialog: Save runs Save As, and Cancel keeps the window open.

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

**Swizzle** in Layer Settings remaps the four output channels of any layer or group.
Each dropdown offers `R`, `G`, `B`, `A`, `1-R`, `1-G`, `1-B`, `1-A`, `0`, `1`, `R * A`, `G * A` and `B * A`.
Products use the original input alpha, regardless of the output A mapping; they do not change alpha automatically.
The default `R G B A` leaves pixels unchanged. Remapping happens after FX in linear space,
before Color Range and blending, and does not modify source pixels.
A changed group swizzle isolates its children. Restoring `R G B A` restores the selected Pass Through
mode when no clipping chain requires isolation. PSD export bakes swizzled pixels; for groups it also
preserves the original children in a hidden folder.

**Layer context menu → Assign Channels** applies presets to selected layers in top-to-bottom tree
order, not selection-click order. One command automatically chooses the preset from the selection count.
Groups count as layers; explicitly selected children count separately.

- **RGB** (1–3 layers): routes each layer's `R * A` into output R, G, B in order, zeros other RGB channels and sets
  output alpha to `1`. All but the bottom selected layer switch to Add; such groups also switch to
  Isolated so Add takes effect. The bottom layer's blend and all layer opacities remain unchanged.
- **RGBA** (4 layers): routes each layer's `R * A` into output R, G, B, A in order, zeroing all other channels. Changes
  Swizzle only. RGB layers consequently have zero alpha and are skipped by ordinary blending;
  this preset does not itself assemble a finished four-channel texture.

The command is one Undo step and is disabled for more than four selected layers. Layers are not merged,
reordered or moved between groups; surrounding layers and existing clipping still affect composition.

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

Tool keys: `V` — No Tool · `T` — Transform · `M` — Rectangle Select · `L` — Polygonal Lasso · `B` — Brush · `P` — Pencil · `G` — Fill · `Z` — Zoom.

| Shortcut | Action |
| :--- | :--- |
| `Ctrl+S` | Save / Save As. |
| `Ctrl+Z` | Undo. |
| `Up` / `Down` | Select the previous / next visible layer row; children of collapsed groups are skipped. |
| `Ctrl+E` / `Ctrl+Alt+E` | Merge selected layers / create a merged copy. |
| `Ctrl+Y` / `Ctrl+Shift+Z` | Redo. |
| `[` / `]` | Decrease / increase brush size. |
| `X` | Swap foreground/background colors. |
| `Alt`-click / drag with Brush, Pencil or Fill | Temporarily sample the primary color from the composition. |
| `RMB` | Temporarily erase with Brush. |
| `Shift`-drag / `Shift`-click with Brush | Axis-aligned line / connection from the previous endpoint. |
| `0`–`9` or numpad | Set active-layer opacity; quick pairs enter a percentage. |
| `Ctrl`-click / `Shift`-click on layer rows, outside thumbnails | Toggle a layer / select a range. |
| `Ctrl`-click on a layer thumbnail or group arrow | Select the layer/group alpha. |
| `Ctrl+C` / `Ctrl+Shift+C` / `Ctrl+V` | Copy the active layer / copy merged / paste on a new Drawing layer. |
| `Ctrl+A` / `Ctrl+D` / `Ctrl+Shift+I` | Select all / deselect / invert the area selection. |
| `Shift` / `Alt` / `Shift+Alt` with a selection tool | Add / subtract / intersect. |
| `Enter` / `Backspace` / `Escape` with Polygonal Lasso | Close / remove the last vertex / cancel the unfinished polygon. |
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

## Acknowledgements

Thanks to the authors and maintainers of the libraries that help power Sprite Editor:

- **[FastNoiseLite](https://github.com/Auburn/FastNoiseLite)** — Jordan Peck and contributors;
  the HLSL implementation powers the Noise layer. [Included MIT license](ThirdPartyNotices.md#fastnoiselite).
- **[Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)** — James Newton-King and contributors;
  JSON serialization for compositor documents and the agent API, provided through Unity's package.
  [Included third-party licenses](Documentation~/Licenses/Newtonsoft-ThirdPartyNotices.md).
- **[Unity Burst](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/index.html)** and
  **[Unity Collections](https://docs.unity3d.com/Packages/com.unity.collections@2.5/manual/index.html)** —
  optimized CPU processing and native collections.

See [Third-party notices](ThirdPartyNotices.md) for source versions and package license details.
Third-party components retain their own licenses.

<a id="community"></a>
## Community & license

Questions or ideas? Join **[Discord · RU / EN](https://discord.gg/kqmJjExuCf)**.
For bugs, open a [GitHub issue](https://github.com/DCFApixels/Unity-SpriteEditor/issues)
with your Unity version and reproduction steps.

Distributed under the **[MIT License](LICENSE.md)**.
