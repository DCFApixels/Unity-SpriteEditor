---
title: "Preview and navigation"
parent: "English"
nav_order: 10
lang: "en"
permalink: "/en/preview/"
alternate: "ru/preview.md"
previous_page: "en/shader-fx.md"
next_page: "en/color.md"
---

# Preview and navigation

Use the Preview to inspect your image up close, check seams or look at individual channels.
Changing the view does not resize the document.

## Move around

- Hold the mouse wheel and drag to pan.
- Scroll to zoom around the pointer with any tool.
- With **Zoom** (`Z`), click to zoom in, `Alt`-click to zoom out, or drag a rectangle around the area you want to inspect.
- **Fit** shows the whole canvas; **100%** is useful for checking pixel detail.

Turn on **Tiled** to see repeated copies of the image and paint across its edges.
See [seamless painting](symmetry.md).

## Balance detail and responsiveness

Lower **Live Quality** in the footer if painting on a large image feels slow.
Save and export still use full resolution.

Pencil always shows crisp pixels at full quality, so you can place individual pixels accurately.

## See your paint on a model

1. Save the compositor and assign its texture asset to your model's material.
2. Turn on **Live Update** (the circle button) in the preview footer, next to **Post FX**.
3. Paint or adjust layers: objects using that texture update in Scene View.

The material keeps its texture reference when you toggle Live Update or close Sprite Editor.
Turning it off or closing without saving restores the last saved image. Press **Save** or `Ctrl+S`
to keep your edits; **Save As** creates a different asset and does not redirect existing materials.

Live Update shows the composition without EV, channel-display masks, Debug or preview Post FX.
While editing it uses preview quality, then refines the result when you stop.
If you resize the canvas, the live image fits the saved texture size until you save again.
Live Update starts off when you open the window and turns off when you switch documents or reload scripts.

## Check brightness and channels

**EV** changes the viewing exposure, not the image itself. Keep it at **0** for the normal view.
If white looks gray or colors look overexposed, check this field first.

**R / G / B / A** lets you inspect the color channels or transparency separately.
See [color and channels](color.md) for the display modes.

> Channel buttons also affect new paint: disabled color channels receive zero,
> and turning off A prevents Brush, Pencil and Fill from leaving a mark.
> Turn all four channels on for ordinary painting.

## Background and problem pixels

Change the transparency checkerboard's colors and size in
**Window tab ⋮ → User Settings…**.

The bug button highlights pixels with invalid color values. Use it to investigate a broken-looking
effect; its highlight color is also in User Settings. The overlay is not included in saved images.

Zoom, Tiled, EV and [Post FX](post-fx.md) change only the view.
