---
title: "Start here"
parent: "English"
nav_order: 1
lang: "en"
permalink: "/en/getting-started/"
alternate: "ru/getting-started.md"
next_page: "en/layers.md"
---

# Start here

**Unity 6 (`6000.0`) or newer.** In **Window → Package Management → Package Manager**,
choose **Install package from git URL** and paste:

```text
https://github.com/DCFApixels/Unity-SpriteEditor.git
```

Burst, Collections and Newtonsoft Json are declared in [package.json](https://github.com/DCFApixels/Unity-SpriteEditor/blob/main/package.json).

## Install through manifest.json or pin a version

Add this entry to the `dependencies` object in `Packages/manifest.json`:

```json
"com.dcfa_pixels.sprite-editor": "https://github.com/DCFApixels/Unity-SpriteEditor.git"
```

The URL follows the default branch. Append `#<tag>` to pin an existing
[release tag](https://github.com/DCFApixels/Unity-SpriteEditor/tags).
The version badge reflects package.json, not the latest tag.

## Your first document

1. Open **Window → Sprite Editor**, click **New**, and set **W / H** above the Preview.
2. Drag a texture from Project onto the Preview, or click **Page +** in the Layers footer
   to create an empty Drawing layer.
3. Select the layer: use **Transform** (`T`) to arrange it, or **Brush** (`B`) to paint on Drawing.
4. Press `Ctrl+S` to save the editable document and its full-resolution output.
5. Assign the saved asset to a texture field, or expand it in Project and use **Output Sprite**.

Double-click the saved texture, sprite, or nested document to reopen it.
**Assets → Open in Sprite Editor** and the document Inspector button also work.

## Find your way around

The Preview is on the left; **Layer Settings** and **Layers** are on the right.
Select a layer to edit its settings. Drag the dividers to resize the panes; the right pane keeps
its width when the window resizes. The left toolbar selects a tool, whose options appear above the Preview.

Pan with `MMB`-drag and zoom with the mouse wheel, regardless of the selected tool.
**Fit** shows the whole canvas; **100%** uses one UI unit per source pixel.

## Tools and preferences

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
