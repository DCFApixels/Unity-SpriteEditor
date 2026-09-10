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

The Preview footer provides quality, channel, exposure and diagnostic controls.
**Live Quality** changes painting-time resolution (12.5–100%, default 80%), not saved/exported pixels.
**EV** changes preview exposure; the bug button reveals invalid numeric pixels.

> **Important.**
> The **R / G / B / A** buttons are also a **paint mask**.
> Disabled RGB channels write `0` in new strokes/fills; with A disabled, they leave no mark.
> Erasing ignores this mask. Existing pixels and exported channels are not changed by the switches.

## Pan and zoom

Drag with `MMB` to pan; the wheel zooms around the pointer with any tool.
With Zoom (`Z`), click to zoom in, `Alt`-click to zoom out, or drag a rectangle to frame it.
The rectangle can extend beyond the canvas. **Fit** frames the full canvas;
**100%** means one UI unit per source pixel, not necessarily one physical monitor pixel.

**Tiled** displays neighboring copies without enlarging the document.
[Seamless painting](symmetry.md) wraps Brush and Pencil footprints across the edges.

## Quality and exposure

Pencil temporarily fixes Live Quality at 100% and uses Point filtering. Changing tools restores
normal preview settings. Effect quality and preview dimensions are separate: stopping an edit
refines the blur; saving renders at full canvas dimensions.

EV affects display only. It resets to 0 when the window opens; a nonzero value highlights the field.
If white looks gray or the image is overexposed, check EV and channel buttons first.

## Diagnostics

The bug button shows invalid numeric pixels accumulated across stages of the current render.
A location can stay marked even if a later layer covers it. This is not a history of every document error.
Invalid numbers are made safe during rendering; the mask is not exported.

Set its color, checkerboard colors and cell size in **Window tab ⋮ → User Settings…**.
[HDR and diagnostic details](../HDR.md#preview-and-numeric-diagnostics).

## What does not change export

Zoom, pan, Tiled, EV, diagnostics and [Post FX](post-fx.md) are presentation settings.
Channel switches also leave old pixels unchanged, but affect new strokes.
[Color, HDR and channel packing](color.md) explains the distinction.
