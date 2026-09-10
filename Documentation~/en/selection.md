---
title: "Area selections"
parent: "English"
nav_order: 5
lang: "en"
permalink: "/en/selection/"
alternate: "ru/selection.md"
previous_page: "en/painting.md"
next_page: "en/symmetry.md"
---

# Area selections

Select an area to paint, erase or fill without touching the rest of the image.
Use **Rectangle Select** (`M`) for a rectangle or **Polygonal Lasso** (`L`) for a shape with straight sides.

## Make and adjust a selection

Drag with Rectangle Select. With Lasso, click around the outline and finish with
`Enter`, a double-click or a click on the first point.
`Backspace` or a right-click removes the last point; `Escape` cancels the unfinished outline.

Choose **Replace**, **Add**, **Subtract** or **Intersect** above the canvas.
You can also hold:

- `Shift` to add an area.
- `Alt` to subtract.
- `Shift+Alt` to keep only the overlap.

`Ctrl+A` selects the whole canvas. `Ctrl+Shift+I` selects the opposite area.
`Ctrl+D` removes the selection so you can paint everywhere again.

## Select a layer's shape

Hold `Ctrl` and click a layer thumbnail or the group's arrow.
The selection follows its visible shape, including soft edges, rather than its rectangular bounds.
You can use a hidden layer as the shape.

## Copy and paste

| Shortcut | Action |
| :--- | :--- |
| `Ctrl+C` | Copy the selected area from the active layer. |
| `Ctrl+Shift+C` | Copy what is visible in that area across all layers. |
| `Ctrl+V` | Paste onto a new Drawing layer. |

Without a selection, copy uses the whole canvas. You can paste between Sprite Editor windows.
On a canvas of the same size, the copy keeps its position; on a different-sized canvas, it is centered.

A selection stays active when you change tools or layers. It is not saved with the document.
If painting seems blocked, try `Ctrl+D`.
