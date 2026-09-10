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

Build an image from separate layers so you can move or adjust each part independently.
The top of the list is the front of the image.

## Add a layer

Use **+** at the bottom of Layers to choose a type:

| Layer | Use it for |
| :--- | :--- |
| File | An existing texture from Project. |
| Drawing Layer | Painting, erasing and filling. |
| Color Fill | A single-color background or shape. |
| Gradient | A smooth color transition. |
| Noise | A generated pattern. See [Noise](noise.md). |

You can also drag a Project texture onto the Preview to add it at the top,
or drop it between rows to choose its position. A newly assigned image keeps its original proportions.

## Select and arrange

Click a row to select it. Hold `Ctrl` to select several layers or `Shift` to select a range.
The **last selected layer is active**: this is the layer you paint on and edit in Layer Settings.

Drag a row's thumbnail or empty space to move the selected layers.
Edit the name directly in the row. **Opacity** controls how much the layer shows;
**Blend** controls how it combines with the image below.
Changing either on a selected row updates all selected layers.

Use the eye to hide a layer. The eye in the column header reveals all layers.

## Keep related parts in a group

Select layers and click **Folder**, or drag layers into an existing group.
Use the group's arrow to expand or collapse it.

Groups start in **Pass Through**, so their layers can blend with layers outside the group.
Choose another blend mode to blend the group as one image. Group opacity fades the whole group.
Transform and Shader FX are available on individual layers, not groups.

## Duplicate, merge or delete

Right-click a row or open **⋮** for actions on the selection.
**Duplicate** makes a copy you can edit separately. File layers still use the same source texture.

| Footer icon | Click | Drop selected layers |
| :--- | :--- | :--- |
| **+** | Choose a layer type. | Duplicate. |
| **Page +** | Add a Drawing layer. | Make a merged Drawing copy. |
| **Folder** | Group the selection. | Group. |
| **Trash** | Delete the selection. | Delete. |

See [merging and conversion](transform.md#merge-layers-or-convert-them-to-drawing)
when you want to paint on the combined result.
