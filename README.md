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

## From source to finished texture

[Start a document](#quick-start) → [Build the stack](#layers) → [Paint & select](#painting) →
[Add effects](#effects) → [Check the result](#preview) → [Save & use](#saving)

The essentials stay visible below. Expand a topic when you need its controls or edge cases.

<a id="installation"></a>
## Installation

**Unity 6 (`6000.0`) or newer.** In **Window → Package Management → Package Manager**,
choose **Install package from git URL** and paste:

```text
https://github.com/DCFApixels/Unity-SpriteEditor.git
```

Burst, Collections and Newtonsoft Json are declared in [package.json](package.json).

<details>
<summary>Install through manifest.json or pin a version</summary>

Add this entry to the `dependencies` object in `Packages/manifest.json`:

```json
"com.dcfa_pixels.sprite-editor": "https://github.com/DCFApixels/Unity-SpriteEditor.git"
```

The URL follows the default branch. Append `#<tag>` to pin an existing
[release tag](https://github.com/DCFApixels/Unity-SpriteEditor/tags).
The version badge reflects package.json, not the latest tag.

</details>

<a id="quick-start"></a>
## Your first document

1. Open **Window → Sprite Editor**, click **New**, and set **W / H** above the Preview.
2. Drag a texture from Project onto the Preview, or click **Page +** in the Layers footer
   to create an empty Drawing layer.
3. Select the layer: use **Transform** (`T`) to arrange it, or **Brush** (`B`) to paint on Drawing.
4. Press `Ctrl+S` to save the editable document and its full-resolution output.
5. Assign the saved asset to a texture field, or expand it in Project and use **Output Sprite**.

Double-click the saved texture, sprite, or nested document to reopen it.
**Assets → Open in Sprite Editor** and the document Inspector button also work.

<a id="workspace"></a>
### Find your way around

The Preview is on the left; **Layer Settings** and **Layers** are on the right.
Select a layer to edit its settings. Drag the dividers to resize the panes; the right pane keeps
its width when the window resizes. The left toolbar selects a tool, whose options appear above the Preview.

Pan with `MMB`-drag and zoom with the mouse wheel, regardless of the selected tool.
**Fit** shows the whole canvas; **100%** uses one UI unit per source pixel.

<details>
<summary>Tools, separate Properties windows and personal settings</summary>

| Tool | Key | Purpose |
| :--- | :---: | :--- |
| No Tool | `V` | View without editing handles. |
| Transform | `T` | Move, resize and rotate a non-group layer. |
| Rectangle Select | `M` | Select a rectangle. |
| Polygonal Lasso | `L` | Select a polygon by clicking its vertices. |
| Brush / Pencil | `B` / `P` | Soft-edged painting / crisp pixel painting. |
| Fill | `G` | Fill a connected region or all matching colors. |
| Zoom | `Z` | Click to zoom in, `Alt`-click to zoom out, or drag an area to frame it. |

Zoom can frame empty space too. Navigation changes only the view; opening another document restores Fit.
The last tool is remembered. No Tool keeps the options row empty instead of moving the canvas.

Each **layer ⋮ → Properties** invocation opens a separate window; **FX** opens the modifier editor.
**Window tab ⋮ → User Settings…** controls checkerboard colors, cell size and the invalid-pixel color.
These are user preferences, not document edits.

**Reset Sprite Editor Settings…** in the same menu resets layout and tool/view preferences in all
open windows after confirmation. It has no Undo, but does not delete documents, unsaved work or assets,
change per-layer settings, or alter Unity docking.

</details>

<a id="layers"></a>
## 1. Build and arrange the layer stack

The list shows the **topmost layer first**. The **last selected layer is active**: painting, transforms
and Layer Settings use that layer. Click a row to select it; use `Ctrl` to toggle rows,
`Shift` for a range, and `Up` / `Down` to navigate visible rows.

Start with a **File**, **Drawing**, **Color Fill**, **Gradient** or **Noise** layer from **+**.
Drag Project textures into the list to place them between rows or inside groups; dropping onto
the Preview adds them at the top. The first texture assigned to an empty File layer receives
**Original Aspect** automatically. Existing source textures are never modified.

Drag a selected row's background or thumbnail to move the selection. Use **Folder** to group it.
Adjust opacity and Blend inline; changing either on a selected row updates all selected rows.

<details>
<summary>Footer actions, groups, duplication and naming</summary>

| Footer icon | Click | Drop selected layers |
| :--- | :--- | :--- |
| **+** | Choose a layer type. | Duplicate the selection. |
| **Page +** | Create Drawing above the active layer. | Merge into a Drawing copy, keeping originals. |
| **Folder** | Group the selection. | Group the dragged layers. |
| **Trash** | Delete the selection. | Delete the dragged layers. |

Open the row menu with **⋮** or right-click. Its actions apply to the selection where supported.
A selected group includes descendants once. **Duplicate** creates independent Drawing pixels and
embedded FX; external source assets remain linked. Targets within the copied set follow the copies.

Groups default to **Pass Through**: children blend with the surrounding stack. Another blend mode
isolates the group; group opacity affects the whole result. Group transforms and modifiers are not supported.
The eye in the table header reveals all layers and groups.

Each layer type has its own increasing name counter. Drawing uses `Layer n`;
other types use their type name, such as `File n`, `Noise n` and `Group n`.
Copies append ` Copy n` using a separate shared counter. Deleted numbers are not reused.

</details>

<details>
<summary>Noise: seed, detail scale and color/data output</summary>

Noise generates OpenSimplex2, OpenSimplex2S, Perlin, Value, Value Cubic or Cellular on the GPU.
Choose Seed, adjust Scale/Offset, and add fractals or Domain Warp. Larger Scale means finer detail;
it is measured across the shorter canvas side. Drag numeric labels or sliders to update the preview.

**Color Values** outputs display-oriented grayscale; **Linear Data** keeps raw 0–1 values for masks,
height maps and channel packing. RGB contains the same scalar and alpha is 1. Only parameters are
stored, not pixel snapshots. Export uses the same coordinates/seed at full resolution.
Tiled preview repeats the result; Noise itself is not inherently seamless.

</details>

<a id="transform"></a>
### Place the image

Choose **Transform** (`T`): drag the frame to move, its edges/corners to resize, and the round
handle to rotate. Expand **Transform** in Layer Settings for numeric input.
**Original Aspect** restores source proportions inside the current frame; **Reset** resets the transform.

<details>
<summary>Pivot, snapping, tiling and filtering</summary>

- Drag the gold pivot without moving the image. It snaps to corners, edge midpoints and center.
  `Ctrl` bypasses snapping; the pivot may lie outside the frame.
- `Shift` constrains movement, keeps resize proportions, or rotates in 15° steps.
  `Escape` cancels the drag; `T` / `Enter` leaves Transform. Each drag is one Undo step.
- **Tiling:** Clip leaves pixels outside the frame transparent; Repeat tiles the source;
  Mirror alternates reflected tiles; Source inherits the texture's wrap modes, including separate U/V.
- **Filter:** Source inherits the texture setting, Point keeps pixel edges, Bilinear smooths them,
  and Trilinear uses existing mipmaps. Without mipmaps it behaves like Bilinear.
  Generated layers use Bilinear for Source. Filter survives Transform Reset.
- Position uses canvas pixels, Pivot normalized coordinates, Scale a multiplier, and Rotation degrees.
  Original Aspect shrinks one axis without changing image center, pivot, rotation, flips or tiling.

Transform tiling repeats the rendered source. It is separate from brush repetition and Tiled preview.

</details>

<details>
<summary>Merge layers or convert them to Drawing</summary>

`Ctrl+E` replaces the selection with one Drawing layer; `Ctrl+Alt+E` makes a merged copy
and keeps the originals enabled. Both are in the row menu. Visible content, transforms and effects
are baked at full canvas resolution; the result has an identity transform. Each merge is one Undo step.
Partial merges can change blending against unselected layers.

**Convert to Drawing** works on any layer, including Drawing itself:

| Mode | Result |
| :--- | :--- |
| **Keep Transform** | Rasterize the source and keep its transform editable. |
| **Apply Transform** | Bake transform/tiling into canvas-sized pixels, then reset Transform and use Clip. |

Single-layer conversion retains identity, name, stack position, opacity, blending and live modifiers.
Group conversion flattens visible descendants, uses Normal/opacity 1, and asks for confirmation:
outside blends can change and targets referring to removed descendants are lost.
Links to the converted group itself remain valid. Conversion supports Undo/Redo.

</details>

<a id="painting"></a>
## 2. Paint, fill and select an area

Select a **Drawing** layer and paint directly on the Preview while seeing the whole stack.
Choose **Brush** (`B`) for soft edges or **Pencil** (`P`) for crisp pixels.
Pencil offers Circle, Square and Diamond tips.

- **Size**, **Hard** and **Step** set brush diameter, hardness and stamp spacing.
  Drag the Size or Step label, or use `[` / `]` to resize.
- `X` swaps the two color swatches. Hold `Alt` and click/drag to sample the composition.
- `LMB` uses the chosen paint/erase mode; `RMB` temporarily erases.
- `Shift`-drag draws along an axis; `Shift`-click connects to the previous endpoint.

Tool preferences are shared and do **not** add Undo steps. Symmetry and transforms belong to the layer.
Clicking with a paint tool on a non-Drawing layer offers conversion; that first click does not paint.

<details>
<summary>Brush spacing, Pencil preview and straight-line details</summary>

Step is a percentage of brush diameter. Pencil has its own size (initially 1 px), no hardness or
spacing control, and continuous pixel-grid strokes. Size shortcuts change by 1 px below 20 px,
then by approximately 10%.

Pencil forces Point-filtered, full-resolution preview and temporarily locks Live Quality at 100%.
Switching tools restores the saved quality. Its cursor follows pixel boundaries at close zoom.

For a fresh axis-aligned line, click without `Shift`, then hold it while dragging.
Repeated `Shift`-clicks form connected segments; erasing, spacing and repetition still apply.
Undo/Redo, clearing the layer or changing documents resets the connection anchor.

The temporary eyedropper works with Brush, Pencil and Fill. It samples full-resolution composition
colors and alpha, excluding preview exposure, channel filtering, checkerboard and Debug overlays.

</details>

### Fill a region

With **Fill** (`G`), click to write the foreground color into the active Drawing layer.
**All Layers** chooses whether boundaries come from that layer or the visible composition.
**Contiguous** chooses a connected region versus all matching colors.

<details>
<summary>Fill controls and limits</summary>

| Option | Purpose | Default |
| :--- | :--- | :--- |
| All Layers | Sample the whole composition instead of source-layer pixels. | Off |
| Contiguous | Fill only the connected region at the click. | On |
| Tolerance | Color/alpha similarity, 0–255. | 32 |
| Antialias | Soften boundary coverage. | On |
| Expand (px) | Grow under a contour by 0–32 source pixels. | 0 |

Fill always writes to the active Drawing layer, not all sampled layers. Each fill is one Undo step.
It edits the primary source frame and does not repeat through brush symmetry. For rendered copies
outside that frame, use **Convert to Drawing → Apply Transform** first.
All Layers is projected into the source grid; an untransformed, canvas-sized layer gives canvas-pixel boundaries.
Sampling stays full resolution regardless of Live Quality, with a 16,777,216-pixel limit per image.

</details>

### Limit edits to an area

Use **Rectangle Select** (`M`) or **Polygonal Lasso** (`L`).
The selected area clips painting, erasing and fill. `Ctrl`-click a layer thumbnail or group arrow
to select its alpha; `Ctrl+D` clears the selection.

`Ctrl+C` copies the active layer's selected area; `Ctrl+Shift+C` copies the visible composition.
`Ctrl+V` pastes onto a **new Drawing layer**.

<details>
<summary>Lasso gestures, selection operations and clipboard behavior</summary>

Click lasso vertices, then finish with `Enter`, a double-click, the first vertex or **Close**.
`Backspace` / `RMB` removes a vertex; `Escape` cancels the unfinished shape.
Replace/Add/Subtract/Intersect are in the header; hold `Shift` / `Alt` / `Shift+Alt`
to add/subtract/intersect. `Ctrl+A` selects all and `Ctrl+Shift+I` inverts.

A selection survives tool/layer changes but clears on document switch, canvas resize, window close
or script reload. It is temporary view state, not Undo history, and does not restrict agent API strokes.
An active empty selection blocks painting. Selections support up to 16,777,216 canvas pixels.

Alpha selection includes transform, FX, Swizzle and clipping; the selected layer's outer
visibility/opacity are ignored, while child settings inside a group are retained.

The clipboard is internal to Sprite Editor, shared between its windows until script reload or exit.
Without an area selection, copy uses the whole canvas. Paste preserves position on an equal-sized
canvas, or centers/clips the copied region on a different-sized one. Preview settings do not affect copying.

</details>

<a id="symmetry"></a>
### Draw patterns and seamless textures

For mirrored or repeated strokes, open **Symmetry & Repeat** in the Drawing layer's settings.
Each layer remembers its own setup. For edge wrapping across the whole canvas, enable **Tiled**
above the Preview and paint on any copy: footprints crossing an edge continue on the opposite side.

<details>
<summary>Symmetry modes, boundaries and the difference from tiling</summary>

| Mode | Controls |
| :--- | :--- |
| Mirror | X/Y axes, Center and Angle. At 0°, X mirrors across the vertical axis, Y across the horizontal. |
| Horizontal / Vertical | Row or column with a copy count. |
| Grid | Independent X/Y copy counts. |
| Radial | Center, sector count and Start Angle. |
| None | No stroke copies. |

**Copy / Alternate Mirror** chooses ordinary or alternating reflected copies in repeat modes.
Mirror is its own mode, not an extra reflection on top. Mirror rotates counterclockwise;
Radial's start angle is counterclockwise from the left.

**Edges → Continue** allows crossing region boundaries. **Clip** constrains a stroke to its starting
region and each repeated footprint to its own region. The active copy stays under the cursor.

Symmetry duplicates **new strokes**. Transform tiling repeats **a layer's source**.
Tiled preview repeats **the whole composition** without changing document/export size.
In Tiled view, Brush/Pencil erasing and painting wrap across edges even with a transform;
the transformed source frame still determines where its pixels can receive marks.

</details>

<a id="effects"></a>
## 3. Add effects and control their coverage

Use **+** to add an effect layer above its source:

| Goal | Layer | Start with |
| :--- | :--- | :--- |
| Outline a shape | Outline | Width, softness and position. |
| Build a distance field | SDF | Source channel, threshold and distance. |
| Generate surface normals | Normal Map | Height Map or Texture; Simple controls first. |
| Soften an image | Gaussian Blur | Radius. |
| Create a motion trail | Motion Blur | Linear distance/angle or Circular arc/center. |

**Previous** means the next sibling below the effect; **Specific** selects a layer or group.
Drag a layer onto Target to assign it. With a dragged selection, the active layer is used.
A hidden target still works; inside a target group, hidden children remain excluded.
Effects see the group's own isolated content, while the group's main composition may stay Pass Through.

<details>
<summary>Blur controls and interactive quality</summary>

- **Gaussian Blur:** Radius is 0–256 original canvas pixels, default 8; zero bypasses it.
- **Motion Blur:** Linear uses Distance (0–512 px) and Angle. Circular uses Arc (0–360°)
  and normalized Center, with `(0.5, 0.5)` at the middle. Circular is rotation, not zoom.
  Direction is Centered/Forward/Backward; Forward follows Angle or turns counterclockwise.
- **Strength** on Motion Blur is 0–400%, default 100%. Below 100% it mixes with the original.
  Above 100% it makes translucent trails denser without changing length or color brightness;
  fully opaque pixels are unchanged.
- **Edges:** Transparent, Clamp, Repeat or Mirror, independent of Tiled preview.
  Both effects filter color with alpha correctly; use HDR Color Range to retain intensities above 1.

Large blurs use cheaper interactive previews and refine after editing settles. Saving and export
use full quality. Cache storage is bounded and separate from Undo; large cold renders can still be
expensive, and long circular arcs can show discrete traces on fine details.

[Gaussian rendering & cache](Documentation~/GaussianBlur.md) · [Motion Blur details](Documentation~/MotionBlur.md)

</details>

<details>
<summary>Outline/SDF sources and Normal Map output</summary>

Outline/SDF use grayscale coverage for groups; Normal Map and blurs use RGBA.
Available distance algorithms include exact Euclidean EDT, approximate Euclidean, Manhattan and Chebyshev.
Self-referencing and cyclic effect targets are rejected.

Normal Map **Height Map** uses a chosen height channel; **Texture** estimates relief from image detail.
Use **Output → Height** to tune the reconstruction, then return to Normal.
Advanced exposes filtering, detail bands, lighting removal, levels, orientation and encoding;
switching back to Simple does not reset them.

Keep Normal blending, opacity 1 and identity Swizzle for a usable normal texture. Mixing does not
renormalize vectors; Transform moves the image without reorienting normals.
Packed Color is suited to PNG/TGA/PSD; Linear Data is for linear EXR/Texture2D.
Import an exported PNG/TGA as **Normal Map**, without grayscale conversion.

[Normal Map parameters](Documentation~/AgentAPI.md#normal-map-settings)

</details>

### Clip a layer to the shape below

Enable **Clipping Mask** in the row menu, or `Alt`-click the boundary above the base layer.
Consecutive clipped layers use the first non-clipped sibling below, within the same group.
They keep their colors/effects but cannot expand the base's alpha.

<details>
<summary>Clipping, group blending and blend modes</summary>

A hidden, transparent or missing base hides the chain. Base opacity applies once to the whole result.
Groups participating in clipping are isolated while in that chain. Clipping supports Undo/Redo,
duplication, the agent API and PSD clipping flags; merging bakes it.

Ordinary blend modes combine RGB in overlaps with source-over alpha.
**Overwrite** replaces complete RGBA; layer opacity interpolates old/new pixels.
**None** leaves the composition unchanged.

Normal · Add · Subtract · Multiply · Divide · Screen · Overlay · Darken · Lighten ·
Color Dodge · Color Burn · Linear Dodge (Add) · Linear Burn · Linear Light ·
Linear Light Add/Sub · Vivid Light · Pin Light · Hard Mix · Hard Light · Soft Light ·
Difference · Exclusion · Negation · None · Overwrite.

</details>

### Write a custom effect

Use **+ Shader FX** in a layer's settings, write `ApplyFX`, and click **Apply**.
Code and parameters can live inside the document. For an effect on the already-composited
stack below a position, add a **Shader Processor** layer instead.

<details>
<summary>Shader FX: a first snippet, parameters and reusable code</summary>

Add a **Float** parameter named `_Amount`, then apply this example:

```hlsl
float4 ApplyFX(float2 uv, float4 color)
{
    return float4(lerp(color.rgb, 1.0 - color.rgb, saturate(_Amount)), color.a);
}
```

Parameters support Float, Color, Vector and Texture2D. Their uniforms are generated automatically.
Code and declarations stay drafts until Apply; a compile error keeps the last working effect.
Undo/Redo restores code, caret and selection, but restored code still needs Apply.

`SampleInput(uv)` reads the layer after earlier modifiers. Return straight RGBA; opacity/blending
come later. Built-in inputs include `_MainTex`, `_MainTex_TexelSize`, `_InputSize`,
`_CanvasSize` (width, height, 1/width, 1/height) and `_PreviewScale`. Do not redeclare generated uniforms.

Standard `#include` supports project/package paths and relative paths. Relative paths start in
the document/FX asset folder, or Assets before the first save. After library edits, click Apply again;
after Save As to another folder, check relative paths. Libraries must suit the fragment-shader environment.

**+ Reference** links an external FX shared by its users; **Embed** makes an independent document-owned
copy. Save As and layer duplication copy embedded FX independently. FX run in order after Transform;
changing parameter values does not regenerate shaders.

</details>

<details>
<summary>Shader Processor: process the lower stack instead of one layer</summary>

The Processor uses the same ApplyFX/SampleInput interface, with the lower composite as input.
Normal blending uses Opacity to mix original and processed RGBA; 100% replaces the input.
Other blends combine it with the result. Both ranges default to HDR; hiding the Processor bypasses it.

In Pass Through groups it also sees the external backdrop; isolated groups restrict it to their
children. Standalone previews and rasterization evaluate lower siblings against transparency.
Processors are clipping-chain boundaries, not clipping layers or bases. PSD bakes the composite
and retains the original layers in a hidden Source Layers folder.

</details>

<a id="preview"></a>
## 4. Check the result

The Preview footer provides quality, channel, exposure and diagnostic controls.
**Live Quality** changes painting-time resolution (12.5–100%, default 80%), not saved/exported pixels.
**EV** changes preview exposure; the bug button reveals invalid numeric pixels.

> [!IMPORTANT]
> The **R / G / B / A** buttons are also a **paint mask**.
> Disabled RGB channels write `0` in new strokes/fills; with A disabled, they leave no mark.
> Erasing ignores this mask. Existing pixels and exported channels are not changed by the switches.

<details>
<summary>Channel display, HDR colors and layer ranges</summary>

One RGB channel displays grayscale, with transparency when A is enabled. A alone displays opaque
grayscale alpha. Two or three RGB channels retain their colors; A off ignores transparency.

The footer **HDR** button changes color/gradient pickers, not stored colors or layer ranges.
With HDR off, colors display and paint without their stored HDR intensity; switching back restores it.
Editing a color replaces the stored value.

In **Layer Settings → Color & Blending**, Color Range and Blend Range independently control bounded
or extended behavior. The header dropdown sets both; a blank value means they differ.
The compositor works in linear HDR. Drawing starts in 8-bit storage and promotes to half-float;
returning to Standard does not discard stored HDR. Use **Convert to 8-bit** explicitly when needed.

[HDR, storage and groups](Documentation~/HDR.md)

</details>

<details>
<summary>Pack texture data with Swizzle and Assign Channels</summary>

**Swizzle** remaps any layer/group's RGBA after FX, before range handling and blending:
`R`, `G`, `B`, `A`, `1-R`, `1-G`, `1-B`, `1-A`, `0`, `1`, `R * A`, `G * A`, `B * A`.
Products use the original input alpha. A changed group Swizzle isolates its content.

**Row menu → Assign Channels** configures selected layers in top-to-bottom tree order:

- **1–3 layers:** each layer's `R * A` goes to R, G or B; other RGB channels are zero and A is 1.
  Upper selected layers use Add; the bottom layer's blend and all opacities stay unchanged.
- **4 layers:** each layer's `R * A` goes to R, G, B or A; other channels are zero. Only Swizzle changes.
  RGB layers then have zero alpha and are skipped by ordinary blending: this preset alone
  does **not** assemble a finished four-channel texture.

Disabled above four layers. The command does not merge, reorder or move layers; existing groups,
surrounding content and clipping still affect the composition. It is one Undo step.

</details>

### Preview game post-processing

Enable **Post FX** in the footer and open its arrow drawer to choose Scene View, Game Camera or
a Volume Profile. This optional feature currently supports **URP 17.x with Universal Renderer**.

Post FX affects presentation only: painting, sampling, saved textures and exports use the original
composition. The editor itself and Shader Processor do not require URP.

<details>
<summary>Post FX: sources, background and alpha-based depth</summary>

- **Scene View** inherits the active view's post-processing switch and volumes.
- **Game Camera** inherits a chosen camera's settings and volumes; an empty field uses MainCamera.
- **Profile** uses a Volume Profile with manually controlled camera parameters.
- **Background:** Solid Color or Checkerboard, both opaque underlays before processing.
  The solid color and checker appearance are shared with User Settings.
- **Depth:** Solid is constant; Alpha Height maps alpha to a height surface; Alpha Mask puts
  pixels below Threshold at the far plane. Depth uses original alpha, not the background/channel display.
- **Link to Zoom** links simulated distance to zoom. **Animate** refreshes time-varying effects
  up to 8 fps. Closing the drawer keeps Post FX enabled.

</details>

<details>
<summary>Post FX: Renderer Features and compatibility limits</summary>

The synthetic opaque surface runs through the selected Universal Renderer's active features,
including Full Screen Pass and SSAO. Depth/Depth Normals describe its alpha relief; extra color,
depth and normal passes are requested as needed. Disabling Post FX releases editor-owned resources.

This is not a copy of the scene: no scene geometry, lighting or camera stacks are rendered.
Features must declare their inputs and support offscreen cameras; scene-object filters may exclude
the surface. Motion-history and material-buffer features are not guaranteed.
TAA, the pipeline's Motion Blur and temporal AO are skipped. STP and unsupported pipelines/renderers
show the original preview with a notice. Cameras, profiles and project settings are never edited.

</details>

<a id="saving"></a>
## 5. Save, reopen and use the texture

`Ctrl+S` saves the document, or opens Save As the first time.
**Save** updates it without a dialog and is disabled when there is nothing to save;
**Save As** makes an independent copy. Closing a changed document offers **Save / Discard / Cancel**.

The saved `.asset` already contains a full-resolution **Texture2D**, **Output Sprite**,
and the editable layer document. Assign the main asset to a texture field, or expand it to use the sprite.
A separate export is optional.

> [!IMPORTANT]
> Unity uses the output from the **last explicit Sprite Editor save**.
> Save again after Undo/Redo, FX changes or edits to linked source textures.

<a id="export"></a>
### Export when you need a separate file

| Format | Best for |
| :--- | :--- |
| **PNG / TGA** | Color images with transparency. |
| **JPEG** | Opaque images; exports against white at quality 95. |
| **EXR** | Linear HDR RGB + alpha. |
| **PSD** | Layered interchange with nested groups and a merged image. |
| **Texture2D (.asset)** | A standalone Unity texture without the editable document. |

<details>
<summary>Saved references, HDR export and PSD trade-offs</summary>

Saving preserves output references, including after canvas resize. Drawing pixels and embedded FX
are stored in the document; external textures and referenced FX stay linked. Project thumbnails
show saved pixels. Output Sprite has a centered pivot, full-rectangle mesh and 100 PPU.

EXR and Texture2D retain HDR; PNG/JPEG/TGA/PSD clamp an export copy.
Images exported into Assets are imported as single Sprites, except EXR, which is a linear texture.
Replacing a standalone Texture2D asks for confirmation and preserves references.

PSD preserves names, order, visibility, opacity and supported blends. Compatible fills, gradients
and Outline strokes stay editable; other effects are rasterized and unsupported blends approximated.
Export notes explain compromises per layer. Keep the native document: PSD does not retain live
effect targets or shader code.

[PSD export details](Documentation~/PsdExport.md)

</details>

<a id="shortcuts"></a>
## Shortcut reference

<details>
<summary>Document, layers and opacity</summary>

| Shortcut | Action |
| :--- | :--- |
| `Ctrl+S` | Save / Save As. |
| `Ctrl+Z` · `Ctrl+Y` / `Ctrl+Shift+Z` | Undo · Redo. |
| `Up` / `Down` | Previous / next visible layer. |
| `Ctrl`-click / `Shift`-click outside thumbnails | Toggle a layer / select a range. |
| `Ctrl+Shift`-click | Add a range. |
| `Ctrl+E` / `Ctrl+Alt+E` | Merge selection / merge a copy. |
| `0`–`9` or numpad | Set selected-layer opacity. |

`5` means 50%; press `7` within 0.6 seconds for 57%. After a pause, `7` means 70%.
`0` means 100%, quick `00` means 0%, and `05` means 5%. A quick pair is one Undo step.

</details>

<details>
<summary>Painting, selection and navigation</summary>

| Shortcut | Action |
| :--- | :--- |
| `[` / `]` · `X` | Resize brush/pencil · swap colors. |
| `Alt`-click/drag with Brush, Pencil or Fill | Sample the primary color. |
| `RMB` with Brush/Pencil | Temporarily erase. |
| `Shift`-drag / `Shift`-click while painting | Axis line / connect endpoints. |
| `Ctrl`-click a thumbnail/group arrow | Select alpha. |
| `Ctrl+C` / `Ctrl+Shift+C` / `Ctrl+V` | Copy layer / copy merged / paste. |
| `Ctrl+A` / `Ctrl+D` / `Ctrl+Shift+I` | Select all / deselect / invert area. |
| `Shift` / `Alt` / `Shift+Alt` with selection tools | Add / subtract / intersect. |
| `Enter` / `Backspace` / `Escape` with Lasso | Close / remove vertex / cancel. |
| `MMB`-drag / mouse wheel | Pan / zoom with any tool. |
| `Alt`-click with Zoom | Zoom out. |
| `Ctrl` while snapping | Bypass snapping. |
| `Enter` in Transform · `Escape` during a gesture | Exit tool · cancel gesture. |

</details>

On macOS, `Cmd` also works for saving, selection and Undo/Redo.
Text/numeric fields keep normal input. Unity shortcuts are suspended only while Sprite Editor is focused.

<a id="automation"></a>
## Automate the same workflow

The C# / JSON API can create documents, import images, arrange layers, set effects and paint strokes.
An optional Unity Pipeline adapter exposes `sprite_editor_*` commands through Unity CLI;
the C# API works without it.

[Agent guide](AGENTS.md) · [API reference](Documentation~/AgentAPI.md) ·
[JSON examples](Documentation~/Examples) · [Changelog](CHANGELOG.md)

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
