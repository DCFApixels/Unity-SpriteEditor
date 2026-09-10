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

Select a **Drawing** layer and paint directly on the canvas.
Choose **Brush** (`B`) for soft strokes or **Pencil** (`P`) for crisp pixels.
If you try to paint on another layer type, the editor offers to convert it first.

## Shape the stroke

| Control | What it changes |
| :--- | :--- |
| Size | Stroke width. You can drag the label or use `[` / `]`. |
| Hard | Edge softness: lower values give a softer brush. |
| Step | Space between brush marks: lower values give a smoother line; higher values separate the marks. |

Pencil has no hardness or spacing controls. Choose **Circle**, **Square** or **Diamond**
for its tip. Zoom in to see its exact pixel outline.

## Color and erasing

The first color swatch is the painting color; `X` swaps the two swatches.
Hold `Alt` and click or drag to pick a color from the image.
With Brush or Pencil, hold the right mouse button to erase, or choose erasing in the tool options.

Brush and fill settings follow you between layers. [Symmetry](symmetry.md) is set separately for each Drawing layer.

## Draw straight lines

Click and begin dragging, then hold `Shift` to draw horizontally or vertically.
To connect points, click the first point, then hold `Shift` and click the next one.
Repeat to draw a chain of straight segments.

## Fill an area

Choose **Fill** (`G`) and click the area you want to color.

| Option | When to use it |
| :--- | :--- |
| All Layers | Follow outlines in the whole visible image while filling only the active Drawing layer. Leave off to use that layer alone. |
| Contiguous | Leave on to fill just the connected area you clicked. Turn off to replace matching colors throughout the layer. |
| Tolerance | Increase it to include more similar colors; reduce it if the fill spreads too far. |
| Antialias | Soften the filled edge. |
| Expand (px) | Extend the fill slightly under an outline to close thin gaps. |

Fill does not repeat through brush symmetry. To fill transformed copies outside the layer's
original area, first use **Convert to Drawing → Apply Transform**.

Use an [area selection](selection.md) to keep painting or filling inside a chosen shape.
