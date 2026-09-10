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

Use **Rectangle Select** (`M`) or **Polygonal Lasso** (`L`).
The selected area clips painting, erasing and fill. `Ctrl`-click a layer thumbnail or group arrow
to select its alpha; `Ctrl+D` clears the selection.

`Ctrl+C` copies the active layer's selected area; `Ctrl+Shift+C` copies the visible composition.
`Ctrl+V` pastes onto a **new Drawing layer**.

## Lasso gestures, selection operations and clipboard behavior

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
