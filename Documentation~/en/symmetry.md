---
title: "Symmetry and seamless painting"
parent: "English"
nav_order: 6
lang: "en"
permalink: "/en/symmetry/"
alternate: "ru/symmetry.md"
previous_page: "en/selection.md"
next_page: "en/effects.md"
---

# Symmetry and seamless painting

For mirrored or repeated strokes, open **Symmetry & Repeat** in the Drawing layer's settings.
Each layer remembers its own setup. For edge wrapping across the whole canvas, enable **Tiled**
above the Preview and paint on any copy: footprints crossing an edge continue on the opposite side.

## Symmetry modes, boundaries and the difference from tiling

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
