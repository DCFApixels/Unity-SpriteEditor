---
title: "Brush, Pencil and Fill"
parent: "English"
nav_order: 4
lang: "en"
permalink: "/en/painting/"
alternate: "ru/painting.md"
previous_page: "en/transform.md"
next_page: "en/selection.md"
---

# Brush, Pencil and Fill

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

## Brush spacing, Pencil preview and straight-line details

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

## Fill a region

With **Fill** (`G`), click to write the foreground color into the active Drawing layer.
**All Layers** chooses whether boundaries come from that layer or the visible composition.
**Contiguous** chooses a connected region versus all matching colors.

## Fill controls and limits

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
