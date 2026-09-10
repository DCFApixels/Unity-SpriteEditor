---
title: "Layers and groups"
parent: "English"
has_children: true
nav_order: 2
lang: "en"
permalink: "/en/layers/"
alternate: "ru/layers.md"
previous_page: "en/getting-started.md"
next_page: "en/transform.md"
---

# Layers and groups

The list shows the **topmost layer first**. The **last selected layer is active**: painting, transforms
and Layer Settings use that layer. Click a row to select it; use `Ctrl` to toggle rows,
`Shift` for a range, and `Up` / `Down` to navigate visible rows.

Start with a **File**, **Drawing**, **Color Fill**, **Gradient** or **Noise** layer from **+**.
Drag Project textures into the list to place them between rows or inside groups; dropping onto
the Preview adds them at the top. The first texture assigned to an empty File layer receives
**Original Aspect** automatically. Existing source textures are never modified.

Drag a selected row's background or thumbnail to move the selection. Use **Folder** to group it.
Adjust opacity and Blend inline; changing either on a selected row updates all selected rows.

## Footer actions, groups, duplication and naming

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


For a procedural source, see [Noise](noise.md). For transparency and masks, see [blending and clipping](blending.md).
