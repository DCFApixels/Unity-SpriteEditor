---
title: "Transform and rasterize"
parent: "English"
nav_order: 3
lang: "en"
permalink: "/en/transform/"
alternate: "ru/transform.md"
previous_page: "en/layers.md"
next_page: "en/painting.md"
---

# Transform and rasterize

Choose **Transform** (`T`): drag the frame to move, its edges/corners to resize, and the round
handle to rotate. Expand **Transform** in Layer Settings for numeric input.
**Original Aspect** restores source proportions inside the current frame; **Reset** resets the transform.

## Pivot, snapping, tiling and filtering

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

## Merge layers or convert them to Drawing

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
