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
- [Community & license](#community)

<a id="installation"></a>
## Installation

**Requires Unity 6 (`6000.0`) or newer.** Burst and Collections are declared package dependencies.

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
5. Press `Ctrl+S` to save the editable composition.
6. Choose **Export** when you need a flattened texture.

> [!TIP]
> Reopen a saved document by double-clicking its asset in Project, or use
> **Open in Sprite Editor** in its Inspector.

<a id="workspace"></a>
## Workspace & saving

The document header is shared by the entire window:

**New → document field → Save → Save As → Export**

- **Save** writes to the current asset without a dialog. It is disabled for temporary documents.
- **Save As** creates a separate editable document.
- `Ctrl+S` saves an existing asset, or opens Save As for a temporary document.
- **Export** writes the flattened image, not the editable layer tree.

The **Preview is on the left**; selected-layer settings and the layer list are on the right.
Drag the vertical divider to adjust the right pane's width, which stays fixed when resizing the
window. The right pane has its own horizontal divider and independently scrolling sections.

Settings follow the active layer. **Transform** starts collapsed; brush settings appear when
a Drawing layer is active. Each **⋮ → Properties** invocation opens a separate window.
**FX** lives in the same menu.

<a id="layers"></a>
## Layers & groups

**Layer types:** File · Drawing · Color Fill · Gradient · Outline · SDF · Group.

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

Per-row menus and inline controls affect that row. New names use independent, document-local
counters: `Layer 1…` and `Group 1…`. Deleted numbers are not reused.

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

Select a **Drawing Layer** and paint directly in the Preview. The image still shows the complete
layer stack, with blending and effects applied.

- `LMB` uses the selected Brush/Eraser tool; `RMB` temporarily erases.
- Adjust **Size**, **Hardness**, and **Step**. Step is stamp spacing as a percentage of brush diameter.
- Two color swatches store foreground/background colors; `X` swaps them.
- `Shift`-drag locks a stroke horizontally or vertically; `Shift`-click connects the previous
  painted endpoint to the new point.
- **Live Quality** sets painting-time Preview resolution: **12.5–100%**, default **37.5%**.
  Lower values reduce preview work; exports and saved canvas dimensions are unchanged.

### Symmetry & repeated strokes

Set a normalized **Center**, then combine the following:

| Setting | Behavior |
| :--- | :--- |
| Mirror X / Mirror Y | Reflect across the vertical / horizontal axis through Center. |
| Horizontal / Vertical | Repeat in a row or column. |
| Grid | Repeat with independent X/Y counts. |
| Radial | Repeat around a circle with an adjustable sector count. |
| Copy / Alternate Mirror | Repeat directly / reflect every second cell or sector. |
| Continue / Clip | Let the stroke cross boundaries / constrain it to the cell or sector where the stroke began. |

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

Select a non-group layer and enable **Transform** in the Preview header, or press `T`.

- Drag inside the frame to move; use its eight handles to resize and the round handle to rotate.
- Drag the **gold pivot cross** without moving the visible image.
- The pivot snaps to nine anchors: corners, edge midpoints, and center. Hold `Ctrl` to bypass snapping.
- Hold `Shift` for axis-constrained movement, proportional resizing, or 15° rotation steps.
- `Escape` cancels the drag; `T` / `Enter` exits Transform mode. Painting is paused while it is active.

**Tiling:** Clip leaves pixels outside the frame transparent; Repeat tiles the texture;
Mirror alternates reflected tiles. These settings affect Preview and export without changing
the source texture importer.

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
- **FX modifiers** are materials applied in list order after a layer's transform.

Existing serialized values for Normal, Multiply, and Overwrite are preserved for older documents.

</details>

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
| `Enter` / `Escape` | Exit Transform / cancel the current transform drag. |
| `Ctrl` during pivot drag | Disable snapping. |

On macOS, `Cmd` also works for saving, selection, and Undo/Redo.

Unity Shortcut Manager commands are suspended **only while this window has focus**, so they do
not consume drawing keys. Text/numeric fields keep normal input. Unity shortcuts are restored
when the window loses focus or closes.

<a id="community"></a>
## Community & license

Questions, ideas, or something unexpected? Join **[Discord · RU / EN](https://discord.gg/kqmJjExuCf)**.
For reproducible bugs, open a [GitHub issue](https://github.com/DCFApixels/Unity-SpriteEditor/issues)
with your Unity version and reproduction steps.

Distributed under the **[MIT License](LICENSE.md)**.
